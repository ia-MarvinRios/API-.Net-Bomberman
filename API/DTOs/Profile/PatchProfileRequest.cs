namespace API.DTOs.Profile
{
    public class PatchProfileRequest
    {
        // Both optional: only validates/updates incoming stuff
        public string? DisplayName { get; set; }
        public string? SelectedSkinId { get; set; }
    }
}
