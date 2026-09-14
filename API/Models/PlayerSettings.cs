namespace API.Models
{
    public class PlayerSettings
    {
        public Guid UserId { get; set; }
        public bool SoundEnabled { get; set; } = true;
        public float SfxVolume { get; set; } = 0.8f;
        public string GraphicsQuality { get; set; } = "high";
        public int TargetFps { get; set; } = 60;
        public float SwipeThresholdCm { get; set; } = 0.6f;

        public User? User { get; set; }
    }
}
