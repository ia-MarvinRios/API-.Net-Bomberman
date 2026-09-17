using API.Data;
using API.DTOs.Profile;
using API.Exceptions;
using API.Helpers;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
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

            return Ok(new { email = user.Email });
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
