namespace API.Models
{
    public class GameConfig
    {
        public int Id { get; set; } = 1; // Always 1
        public int ConfigVersion { get; set; } = 1;
        public List<string>? EnabledMapIds { get; set; } = null;

        public string MinClientVersion { get; set; } = "0.1.0";

        public float BalanceFuseSeconds { get; set; }
        public float BalanceBaseSpeed { get; set; }
        public int BalanceBaseRange { get; set; }
        public int BalanceBaseMaxBombs { get; set; }
        public int BalanceMatchDurationSeconds { get; set; }
        public float BalanceWallCooldownSeconds { get; set; }
        public float BalanceWallDurationSeconds { get; set; }
        public float BalanceBombRequestTimeoutSeconds { get; set; }
        public float BalanceTimedFuseSeconds { get; set; }
        public float BalanceRemoteMaxSeconds { get; set; }
        public int BalanceThrowDistanceCells { get; set; }
        public float BalanceThrowDurationSeconds { get; set; }
        public int BalanceMaxRange { get; set; }
        public int BalanceMaxBombs { get; set; }
        public int BalanceMaxSpeedLevel { get; set; }
    }
}
