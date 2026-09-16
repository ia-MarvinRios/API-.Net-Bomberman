using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    public class RefreshRequest
    {
        [Required]
        public required string RefreshToken { get; set; }
    }
}
