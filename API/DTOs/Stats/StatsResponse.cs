namespace API.DTOs.Stats
{
    public class StatsResponse
    {
        public required int MatchesPlayed { get; set; }
        public required int Wins { get; set; }
        public required int Losses { get; set; }
        public required int Draws { get; set; }
        public required int Kills { get; set; }
        public required int Deaths { get; set; }
        public required int PowerUpsCollected { get; set; }
        public required double WinRate { get; set; }
    }
}
