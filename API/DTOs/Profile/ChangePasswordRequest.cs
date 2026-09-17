using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Profile
{
    public class ChangePasswordRequest
    {
        [Required]
        public required string CurrentPassword { get; set; }

        [Required]
        [MinLength(8)]
        public required string NewPassword { get; set; }
    }
}
