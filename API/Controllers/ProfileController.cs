using API.Data;
using API.DTOs.Profile;
using API.Exceptions;
using API.Helpers;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.DTOs.Common;

namespace API.Controllers
{
    /// <summary>Perfil, ajustes y datos de cuenta del jugador autenticado.</summary>
    [Route("api/v1/profile")]
    [ApiController]
    [Authorize] // All endpoints in this controller need authentication
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;

        public ProfileController(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        /// <summary>Devuelve el perfil del jugador autenticado.</summary>
        /// <remarks>El nivel se calcula a partir de la experiencia: `level = 1 + floor(xp / 500)`.</remarks>
        /// <response code="200">Perfil del jugador.</response>
        /// <response code="401">`AUTH_UNAUTHORIZED`: falta el access token o es inválido.</response>
        [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.GetUserId();

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Profile not found");

            return Ok(BuildProfileResponse(profile));
        }

        /// <summary>Actualiza parcialmente el perfil.</summary>
        /// <remarks>Ambos campos son opcionales; solo se actualiza lo que se envía.</remarks>
        /// <param name="request">`displayName` (3-16 caracteres) y/o `selectedSkinId` (debe estar desbloqueada).</param>
        /// <response code="200">Perfil actualizado.</response>
        /// <response code="400">`VALIDATION_FAILED`: nombre fuera de rango o skin no desbloqueada.</response>
        [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMe([FromBody] PatchProfileRequest request)
        {
            var userId = User.GetUserId();

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Profile not found");

            if (request.DisplayName != null)
            {
                if (request.DisplayName.Length is < 3 or > 16)
                {
                    throw new ValidationFailedException("displayName must be 3-16 characters");
                }
                profile.DisplayName = request.DisplayName;
            }

            if (request.SelectedSkinId != null)
            {
                if (!profile.UnlockedSkinIds.Contains(request.SelectedSkinId))
                {
                    throw new ValidationFailedException("Skin is not unlocked");
                }
                profile.SelectedSkinId = request.SelectedSkinId;
            }

            await _context.SaveChangesAsync();

            return Ok(BuildProfileResponse(profile));
        }

        /// <summary>Devuelve los ajustes del jugador.</summary>
        /// <remarks>Si la cuenta aún no tiene ajustes, se crean con los valores por defecto la primera vez.</remarks>
        /// <response code="200">Ajustes del jugador.</response>
        [ProducesResponseType<PlayerSettingsDto>(StatusCodes.Status200OK)]
        [HttpGet("me/settings")]
        public async Task<IActionResult> GetSettings()
        {
            var userId = User.GetUserId();

            var settings = await _context.PlayerSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            // Accounts created before this endpoint management. Set to default
            if (settings == null)
            {
                settings = new PlayerSettings { UserId = userId };
                _context.PlayerSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return Ok(BuildSettingsDto(settings));
        }

        /// <summary>Reemplaza por completo los ajustes del jugador.</summary>
        /// <remarks>
        /// No es una actualización parcial: los cinco campos son obligatorios.
        /// Validación de todo o nada: si algo falla, no se guarda nada.
        /// </remarks>
        /// <param name="request">Ajustes completos.</param>
        /// <response code="200">Ajustes guardados.</response>
        /// <response code="400">`VALIDATION_FAILED`: faltan campos o hay valores fuera de rango.</response>
        [ProducesResponseType<PlayerSettingsDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [HttpPut("me/settings")]
        public async Task<IActionResult> ReplaceSettings([FromBody] PlayerSettingsDto request)
        {
            var userId = User.GetUserId();

            // Validation. Check before accesing db
            if (request.GraphicsQuality is not ("low" or "high"))
            {
                throw new ValidationFailedException("graphicsQuality must be 'low' or 'high'");
            }

            if (request.TargetFps is not (30 or 60))
            {
                throw new ValidationFailedException("targetFps must be 30 or 60");
            }

            var settings = await _context.PlayerSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new PlayerSettings { UserId = userId };
                _context.PlayerSettings.Add(settings);
            }

            settings.SoundEnabled = request.SoundEnabled!.Value;
            settings.SfxVolume = request.SfxVolume!.Value;
            settings.GraphicsQuality = request.GraphicsQuality!;
            settings.TargetFps = request.TargetFps!.Value;
            settings.SwipeThresholdCm = request.SwipeThresholdCm!.Value;

            await _context.SaveChangesAsync();

            return Ok(BuildSettingsDto(settings));
        }

        /// <summary>Cambia la contraseña.</summary>
        /// <remarks>
        /// Revoca el refresh token activo del usuario. El access token usado en esta petición sigue siendo
        /// válido hasta que expire por su cuenta; lo que deja de funcionar es renovar la sesión con el refresh token anterior.
        /// </remarks>
        /// <param name="request">Contraseña actual y contraseña nueva (mínimo 8 caracteres).</param>
        /// <response code="204">Contraseña cambiada.</response>
        /// <response code="400">`VALIDATION_FAILED`: faltan campos o la nueva contraseña es muy corta.</response>
        /// <response code="401">`AUTH_INVALID_CREDENTIALS` si la contraseña actual es incorrecta, o `AUTH_UNAUTHORIZED` si falta el access token.</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.GetUserId();

            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException("User not found");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidCredentialsException();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            // Revokes the active refresh tokens: current session keeps alive due to its access token,
            // but can't be renewed using the previous refresh token
            await _tokenService.RevokeAllActiveTokensForUserAsync(userId);

            return NoContent();
        }

        /// <summary>Cambia el correo electrónico.</summary>
        /// <param name="request">Contraseña actual (para confirmar identidad) y correo nuevo.</param>
        /// <response code="200">Correo actualizado.</response>
        /// <response code="400">`VALIDATION_FAILED`: faltan campos o el formato del correo no es válido.</response>
        /// <response code="401">`AUTH_INVALID_CREDENTIALS` si la contraseña actual es incorrecta, o `AUTH_UNAUTHORIZED` si falta el access token.</response>
        /// <response code="409">`AUTH_USER_ALREADY_EXISTS`: el correo ya lo usa otra cuenta.</response>
        [ProducesResponseType<ChangeEmailResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
        [HttpPut("me/email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            var userId = User.GetUserId();

            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException("User not found");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidCredentialsException();
            }

            var newEmailLower = request.NewEmail.ToLower();

            bool emailTakenByAnotherUser = await _context.Users
                .AnyAsync(u => u.Id != userId && u.Email.ToLower() == newEmailLower);

            if (emailTakenByAnotherUser)
            {
                throw new UserAlreadyExistsException("Email already in use");
            }

            user.Email = request.NewEmail;
            await _context.SaveChangesAsync();

            return Ok(new ChangeEmailResponse{ Email = user.Email });
        }

        // --- Private helpers ---

        private static ProfileResponse BuildProfileResponse(Profile profile)
        {
            var (level, xpToNextLevel) = LevelCalculator.Calculate(profile.Xp);

            return new ProfileResponse
            {
                UserId = profile.UserId,
                Username = profile.User!.Username,
                DisplayName = profile.DisplayName,
                SelectedSkinId = profile.SelectedSkinId,
                UnlockedSkinIds = profile.UnlockedSkinIds,
                Level = level,
                Xp = profile.Xp,
                XpToNextLevel = xpToNextLevel
            };
        }

        private static PlayerSettingsDto BuildSettingsDto(PlayerSettings settings)
        {
            return new PlayerSettingsDto
            {
                SoundEnabled = settings.SoundEnabled,
                SfxVolume = settings.SfxVolume,
                GraphicsQuality = settings.GraphicsQuality,
                TargetFps = settings.TargetFps,
                SwipeThresholdCm = settings.SwipeThresholdCm
            };
        }

    }
}
