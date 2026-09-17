namespace API.DTOs.Profile
{
    public class ProfileResponse
    {
        public required Guid UserId { get; set; }
        public required string Username { get; set; }
        public required string DisplayName { get; set; }
        public required string SelectedSkinId { get; set; }
        public required List<string> UnlockedSkinIds { get; set; }
        public required int Level { get; set; }
        public required int Xp { get; set; }
        public required int XpToNextLevel { get; set; }
    }
}
