namespace API.DTOs.Auth
{
    /// <summary>Respuesta de register, login y refresh.</summary>
    public class AuthResponse
    {
        /// <summary>JWT de acceso. Se envía como `Authorization: Bearer ...`.</summary>
        public required string AccessToken { get; set; }
        /// <summary>Token opaco de renovación (dura 30 días, de un solo uso).</summary>
        public required string RefreshToken { get; set; }
        /// <summary>Segundos de vida del access token.</summary>
        /// <example>3600</example>
        public required int ExpiresIn { get; set; } // seconds
        /// <summary>Resumen de la cuenta autenticada.</summary>
        public required UserSummaryDto User { get; set; }
    }
}
