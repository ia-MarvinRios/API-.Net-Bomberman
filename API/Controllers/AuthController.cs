using API.Data;
using API.DTOs.Auth;
using API.Exceptions;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using API.DTOs.Common;

namespace API.Controllers
{
    /// <summary>Autenticación de cuentas.</summary>
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        /// <summary>Crea una cuenta nueva y deja al usuario autenticado.</summary>
        /// <remarks>
        /// Crea también el perfil (displayName = username, skin `default`, xp 0) y las estadísticas en cero.
        /// Devuelve el par de tokens igual que login.
        ///
        /// Reglas: `username` de 3-16 caracteres alfanuméricos y `email` válido, ambos únicos sin distinguir
        /// mayúsculas; `password` de mínimo 8 caracteres (se guarda hasheada con BCrypt).
        /// </remarks>
        /// <param name="request">Datos de la cuenta nueva.</param>
        /// <response code="201">Cuenta creada. Devuelve los tokens y el resumen del usuario.</response>
        /// <response code="400">`VALIDATION_FAILED`: algún campo no cumple el formato.</response>
        /// <response code="409">`AUTH_USER_ALREADY_EXISTS`: el email o el username ya están en uso.</response>
        /// <response code="429">`RATE_LIMITED`: se superaron las 10 peticiones por minuto por IP.</response>
        [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("AuthPolicy")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var emailLower = request.Email.ToLower();
            var usernameLower = request.Username.ToLower();

            bool alreadyExists = await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == emailLower || u.Username.ToLower() == usernameLower);

            if (alreadyExists)
            {
                throw new UserAlreadyExistsException("Email or username already in use");
            }

            var user = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // To get user.Id

            var profile = new Profile
            {
                UserId = user.Id,
                DisplayName = user.Username,
                SelectedSkinId = "default",
                UnlockedSkinIds = new List<string> { "default" },
                Xp = 0
            };

            var stats = new PlayerStats
            {
                UserId = user.Id
            };

            _context.Profiles.Add(profile);
            _context.PlayerStats.Add(stats);
            await _context.SaveChangesAsync();

            var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            var response = BuildAuthResponse(user, accessToken, refreshToken, expiresIn);

            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>Inicia sesión con email o username.</summary>
        /// <remarks>
        /// Revoca cualquier refresh token anterior del usuario (no se acumulan sesiones).
        /// Por seguridad, la respuesta es idéntica si el usuario no existe o la contraseña es incorrecta.
        /// </remarks>
        /// <param name="request">Email o username, más la contraseña.</param>
        /// <response code="200">Sesión iniciada. Devuelve los tokens y el resumen del usuario.</response>
        /// <response code="400">`VALIDATION_FAILED`: faltan campos.</response>
        /// <response code="401">`AUTH_INVALID_CREDENTIALS`: credenciales incorrectas.</response>
        /// <response code="429">`RATE_LIMITED`: se superaron las 10 peticiones por minuto por IP.</response>
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("AuthPolicy")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var identifierLower = request.EmailOrUsername.ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == identifierLower || u.Username.ToLower() == identifierLower);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new InvalidCredentialsException();
            }

            // Prevent duplicated sessions
            await _tokenService.RevokeAllActiveTokensForUserAsync(user.Id);

            var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            var response = BuildAuthResponse(user, accessToken, refreshToken, expiresIn);

            return Ok(response);
        }

        /// <summary>Renueva la sesión con un refresh token.</summary>
        /// <remarks>
        /// El refresh token es de **un solo uso**: al usarlo se revoca y se devuelve uno nuevo.
        /// Reenviar un refresh token ya usado se trata como expirado y revoca las sesiones activas del usuario.
        /// </remarks>
        /// <param name="request">Refresh token vigente.</param>
        /// <response code="200">Sesión renovada. Devuelve un par de tokens nuevo.</response>
        /// <response code="400">`VALIDATION_FAILED`: falta el refresh token.</response>
        /// <response code="401">`AUTH_TOKEN_EXPIRED`: el token no existe, expiró, fue revocado o ya se usó.</response>
        /// <response code="429">`RATE_LIMITED`: se superaron las 10 peticiones por minuto por IP.</response>
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("AuthPolicy")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var (user, tokens) = await _tokenService.RotateRefreshTokenAsync(request.RefreshToken);

            var response = BuildAuthResponse(user, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);

            return Ok(response);
        }

        /// <summary>Cierra la sesión revocando el refresh token.</summary>
        /// <remarks>Es idempotente: no falla si el token no existe o ya estaba revocado.</remarks>
        /// <param name="request">Refresh token a revocar.</param>
        /// <response code="204">Sesión cerrada.</response>
        /// <response code="400">`VALIDATION_FAILED`: falta el refresh token.</response>
        /// <response code="401">`AUTH_UNAUTHORIZED`: falta el access token o es inválido.</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken);

            return NoContent(); // 204
        }

        private static AuthResponse BuildAuthResponse(User user, string accessToken, string refreshToken, int expiresIn)
        {
            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn,
                User = new UserSummaryDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            };
        }

    }
}
