using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9]{3,16}$", ErrorMessage = "Username must be 3-16 alphanumeric characters")]
        public required string Username { get; set; }

        [Required]
        [MinLength(8)]
        public required string Password { get; set; }
    }
}
