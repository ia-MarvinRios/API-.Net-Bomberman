namespace API.DTOs.Auth
{
    public class UserSummaryDto
    {
        public required Guid UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
