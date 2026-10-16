namespace API.DTOs.Profile
{
    /// <summary>Actualización parcial del perfil. Ambos campos son opcionales.</summary>
    public class PatchProfileRequest
    {
        /// <summary>Nombre visible, de 3 a 16 caracteres.</summary>
        /// <example>Marvin</example>
        // Both optional: only validates/updates incoming stuff
        public string? DisplayName { get; set; }
        /// <summary>Id de una skin ya desbloqueada por el jugador.</summary>
        /// <example>skin_01</example>
        public string? SelectedSkinId { get; set; }
    }
}
