namespace API.DTOs.GameConfig
{
    public class GameConfigResponse
    {
        public List<string>? EnabledMapIds { get; set; }
        public required List<SkinDto> Skins { get; set; }
        public required BalanceDto Balance { get; set; }
        public required string MinClientVersion { get; set; }
        public required int ConfigVersion { get; set; }
    }
}
