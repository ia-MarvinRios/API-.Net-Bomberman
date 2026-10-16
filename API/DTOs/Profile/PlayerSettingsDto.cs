using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Profile
{
    /// <summary>Ajustes del jugador. En PUT, los cinco campos son obligatorios.</summary>
    public class PlayerSettingsDto
    {
        /// <summary>Sonido activado.</summary>
        /// <example>true</example>
        [Required]
        public bool? SoundEnabled { get; set; }

        /// <summary>Volumen de efectos, entre 0 y 1.</summary>
        /// <example>0.8</example>
        [Required]
        [Range(0.0, 1.0)]
        public float? SfxVolume { get; set; }

        /// <summary>Calidad gráfica: `low` o `high`.</summary>
        /// <example>high</example>
        [Required]
        public string? GraphicsQuality { get; set; }

        /// <summary>Fotogramas por segundo objetivo: 30 o 60.</summary>
        /// <example>60</example>
        [Required]
        public int? TargetFps { get; set; }

        /// <summary>Umbral de deslizamiento en centímetros, entre 0.4 y 1.0.</summary>
        /// <example>0.6</example>
        [Required]
        [Range(0.4, 1.0)]
        public float? SwipeThresholdCm { get; set; }
    }
}
