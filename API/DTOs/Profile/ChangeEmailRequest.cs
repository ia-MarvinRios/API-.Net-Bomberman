using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Profile
{
    /// <summary>Cambio de correo.</summary>
    public class ChangeEmailRequest
    {
        /// <summary>Contraseña actual, para confirmar identidad.</summary>
        [Required]
        public required string CurrentPassword { get; set; }

        /// <summary>Correo nuevo, válido y no usado por otra cuenta.</summary>
        /// <example>nuevo@derbymetals.dev</example>
        [Required]
        [EmailAddress]
        public required string NewEmail { get; set; }
    }
}
