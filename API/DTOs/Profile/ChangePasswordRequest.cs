using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Profile
{
    /// <summary>Cambio de contraseña.</summary>
    public class ChangePasswordRequest
    {
        /// <summary>Contraseña actual, para confirmar identidad.</summary>
        [Required]
        public required string CurrentPassword { get; set; }

        /// <summary>Contraseña nueva, de mínimo 8 caracteres.</summary>
        [Required]
        [MinLength(8)]
        public required string NewPassword { get; set; }
    }
}
