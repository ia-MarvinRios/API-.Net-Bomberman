namespace API.Models
{
    public class PlayerStats
    {
        public Guid UserId { get; set; } // PK & FK
        public int MatchesPlayed { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Draws { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int PowerUpsCollected { get; set; } = 0;

        public User? User { get; set; }
    }
}
