namespace API.DTOs.Auth
{
    public class AuthResponse
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public required int ExpiresIn { get; set; } // seconds
        public required UserSummaryDto User { get; set; }
    }
}
