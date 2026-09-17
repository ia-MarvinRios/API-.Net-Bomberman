using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Profile
{
    public class PlayerSettingsDto
    {
        [Required]
        public bool? SoundEnabled { get; set; }

        [Required]
        [Range(0.0, 1.0)]
        public float? SfxVolume { get; set; }

        [Required]
        public string? GraphicsQuality { get; set; }

        [Required]
        public int? TargetFps { get; set; }

        [Required]
        [Range(0.4, 1.0)]
        public float? SwipeThresholdCm { get; set; }
    }
}
