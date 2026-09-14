namespace API.Models
{
    public class Profile
    {
        public Guid UserId { get; set; } // PK & FK
        public required string DisplayName { get; set; }
        public string SelectedSkinId { get; set; } = "default";
        public List<string> UnlockedSkinIds { get; set; } = new() { "default" };
        public int Xp { get; set; } = 0;

        public User? User { get; set; }
    }
}
