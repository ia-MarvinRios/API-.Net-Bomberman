namespace API.DTOs.Matches
{
    public class MatchHistoryItemDto
    {
        public required Guid MatchId { get; set; }
        public required string MapId { get; set; }
        public required DateTime EndedAt { get; set; }
        public required int MyPlacement { get; set; }
        public required List<MatchPlayerDto> Players { get; set; }
    }
}
