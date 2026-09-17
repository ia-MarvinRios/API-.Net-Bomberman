namespace API.DTOs.GameConfig
{
    public class BalanceDto
    {
        public required float FuseSeconds { get; set; }
        public required float BaseSpeed { get; set; }
        public required int BaseRange { get; set; }
        public required int BaseMaxBombs { get; set; }
        public required int MatchDurationSeconds { get; set; }
        public required float WallCooldownSeconds { get; set; }
        public required float WallDurationSeconds { get; set; }
        public required float BombRequestTimeoutSeconds { get; set; }
        public required float TimedFuseSeconds { get; set; }
        public required float RemoteMaxSeconds { get; set; }
        public required int ThrowDistanceCells { get; set; }
        public required float ThrowDurationSeconds { get; set; }
        public required int MaxRange { get; set; }
        public required int MaxBombs { get; set; }
        public required int MaxSpeedLevel { get; set; }
    }
}
