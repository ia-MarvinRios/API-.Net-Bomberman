using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    public class LogoutRequest
    {
        [Required]
        public required string RefreshToken { get; set; }
    }
}
