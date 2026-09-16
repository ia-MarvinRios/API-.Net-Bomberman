using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        public required string EmailOrUsername { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
