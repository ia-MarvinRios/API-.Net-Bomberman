namespace API.Models
{
    public class MatchPlayer
    {
        public long Id { get; set; } // PK
        public Guid MatchId { get; set; }
        public Guid UserId { get; set; }
        public int Placement { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int PowerUpsCollected { get; set; }
        public bool Disconnected { get; set; }

        public Match? Match { get; set; }
    }
}
