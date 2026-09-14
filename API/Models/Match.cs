namespace API.Models
{
    public class Match
    {
        public Guid MatchId { get; set; } // Client
        public required string MapId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public required string EndReason { get; set; }
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MatchPlayer> Players { get; set; } = new List<MatchPlayer>();
    }
}
