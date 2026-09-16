using API.Models;

namespace API.Services
{
    public record TokenPair(string AccessToken, string RefreshToken, int ExpiresIn);

    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT access token for an authenticated user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        (string token, int expiresIn) GenerateAccessToken(User user);

        /// <summary>
        /// Generates a refresh token, hashes and saves it in the DB. Returns plain text value once.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<string> GenerateRefreshTokenAsync(Guid userId);

        /// <summary>
        /// Revokes all the active refresh tokens for a given user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task RevokeAllActiveTokensForUserAsync(Guid userId);

        /// <summary>
        /// Validates a received refresh token, then rotates it (revokes old, creates a new one) and returns the owner user.
        /// Throws TokenExpiredException if it's invalid, expired, already revoked or reused.
        /// </summary>
        /// <param name="plainRefreshToken"></param>
        /// <returns></returns>
        Task<(User user, TokenPair tokens)> RotateRefreshTokenAsync(string plainRefreshToken);

        /// <summary>
        /// Revokes an specific refresh token.
        /// </summary>
        /// <param name="plainRefreshToken"></param>
        /// <returns></returns>
        Task RevokeRefreshTokenAsync(string plainRefreshToken);
    }
}
