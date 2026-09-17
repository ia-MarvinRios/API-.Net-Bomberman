using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Profile
{
    public class ChangeEmailRequest
    {
        [Required]
        public required string CurrentPassword { get; set; }

        [Required]
        [EmailAddress]
        public required string NewEmail { get; set; }
    }
}
