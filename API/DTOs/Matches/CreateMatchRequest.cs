using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Matches
{
    public class CreateMatchRequest
    {
        [Required]
        public Guid MatchId { get; set; }

        [Required]
        public required string MapId { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }

        [Required]
        public required string EndReason { get; set; }

        [Required]
        [MinLength(1)]
        public required List<MatchPlayerRequest> Players { get; set; }
    }
}
