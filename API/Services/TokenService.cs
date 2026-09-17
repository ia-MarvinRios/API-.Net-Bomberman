using API.Configuration;
using API.Data;
using API.Models;
using API.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace API.Services
{
    public class TokenService : ITokenService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public TokenService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        public (string token, int expiresIn) GenerateAccessToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expirationMinutes = _jwtSettings.AccessTokenExpirationMinutes;

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expirationMinutes * 60); // to seconds
        }

        public async Task<string> GenerateRefreshTokenAsync(Guid userId)
        {
            var plainToken = GenerateSecureRandomToken();

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(plainToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return plainToken;
        }

        public async Task RevokeAllActiveTokensForUserAsync(Guid userId)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<(User user, TokenPair tokens)> RotateRefreshTokenAsync(string plainRefreshToken)
        {
            var tokenHash = HashToken(plainRefreshToken);

            var existingToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            // Doesn't exists | Invalid
            if (existingToken == null)
            {
                throw new TokenExpiredException();
            }

            // Already revoked | Could be reused
            if (existingToken.RevokedAt != null)
            {
                await RevokeAllActiveTokensForUserAsync(existingToken.UserId);
                throw new TokenExpiredException();
            }

            // Time expired
            if (existingToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new TokenExpiredException();
            }

            var user = existingToken.User
                ?? throw new TokenExpiredException();

            // -- Rotation --
            var newPlainRefreshToken = GenerateSecureRandomToken();

            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = HashToken(newPlainRefreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            };

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync(); // Saves to get the new generated ID

            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReplacedByTokenId = newRefreshToken.Id;
            await _context.SaveChangesAsync();

            var (accessToken, expiresIn) = GenerateAccessToken(user);

            return (user, new TokenPair(accessToken, newPlainRefreshToken, expiresIn));
        }

        public async Task RevokeRefreshTokenAsync(string plainRefreshToken)
        {
            var tokenHash = HashToken(plainRefreshToken);

            var existingToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (existingToken == null || existingToken.RevokedAt != null)
            {
                return;
            }

            existingToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // -- private helpers --
        private static string GenerateSecureRandomToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private static string HashToken(string plainToken)
        {
            var bytes = Encoding.UTF8.GetBytes(plainToken);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
