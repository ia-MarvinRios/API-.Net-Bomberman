using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Matches
{
    public class MatchPlayerRequest
    {
        [Required]
        public Guid UserId { get; set; }
        public int Placement { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int PowerUpsCollected { get; set; }
        public bool Disconnected { get; set; }
    }
}
