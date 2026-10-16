using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    /// <summary>Datos para crear una cuenta nueva.</summary>
    public class RegisterRequest
    {
        /// <summary>Correo válido y único (sin distinguir mayúsculas).</summary>
        /// <example>jugador@derbymetals.dev</example>
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        /// <summary>Nombre de usuario: 3-16 caracteres alfanuméricos, único (sin distinguir mayúsculas).</summary>
        /// <example>alvaro123</example>
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9]{3,16}$", ErrorMessage = "Username must be 3-16 alphanumeric characters")]
        public required string Username { get; set; }

        /// <summary>Contraseña de mínimo 8 caracteres.</summary>
        /// <example>secret123</example>
        [Required]
        [MinLength(8)]
        public required string Password { get; set; }
    }
}
