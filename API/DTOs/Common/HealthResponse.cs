namespace API.DTOs.Common
{
    public class HealthResponse
    {
        public required string Status { get; set; }
        public required string Version { get; set; }
        public required string ServerTime { get; set; }
    }
}
