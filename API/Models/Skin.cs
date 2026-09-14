namespace API.Models
{
    public class Skin
    {
        public required string Id { get; set; }
        public bool EnabledInSelector { get; set; } = true;
        public required string DisplayName { get; set; }
        public int SortOrder { get; set; }
    }
}
