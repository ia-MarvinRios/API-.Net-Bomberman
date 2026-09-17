using API.Data;
using API.DTOs.Auth;
using API.Exceptions;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
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

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var (user, tokens) = await _tokenService.RotateRefreshTokenAsync(request.RefreshToken);

            var response = BuildAuthResponse(user, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);

            return Ok(response);
        }

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
