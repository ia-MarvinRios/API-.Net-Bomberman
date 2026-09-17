namespace API.DTOs.Matches
{
    public class MatchPlayerDto
    {
        public required Guid UserId { get; set; }
        public required int Placement { get; set; }
        public required int Kills { get; set; }
        public required int Deaths { get; set; }
        public required int PowerUpsCollected { get; set; }
        public required bool Disconnected { get; set; }
    }
}
