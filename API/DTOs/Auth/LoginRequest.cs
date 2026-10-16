using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    /// <summary>Credenciales de inicio de sesión.</summary>
    public class LoginRequest
    {
        /// <summary>Correo o nombre de usuario (sin distinguir mayúsculas).</summary>
        /// <example>alvaro123</example>
        [Required]
        public required string EmailOrUsername { get; set; }

        /// <summary>Contraseña de la cuenta.</summary>
        /// <example>secret123</example>
        [Required]
        public required string Password { get; set; }
    }
}
