namespace API.DTOs.Matches
{
    public class MatchesListResponse
    {
        public required List<MatchHistoryItemDto> Items { get; set; }
        public required int Total { get; set; }
    }
}
