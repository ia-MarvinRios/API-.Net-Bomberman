using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Matches
{
    /// <summary>Reporte de una partida terminada.</summary>
    public class CreateMatchRequest
    {
        /// <summary>UUID de la partida, generado por el cliente. Es la clave de idempotencia.</summary>
        [Required]
        public Guid MatchId { get; set; }

        /// <summary>Id del mapa jugado.</summary>
        /// <example>junkyard01</example>
        [Required]
        public required string MapId { get; set; }

        /// <summary>Inicio de la partida (UTC, ISO 8601).</summary>
        public DateTime StartedAt { get; set; }
        /// <summary>Fin de la partida (UTC, ISO 8601).</summary>
        public DateTime EndedAt { get; set; }

        /// <summary>Motivo de fin: `lastAlive`, `timeout` o `allDisconnected`.</summary>
        /// <example>lastAlive</example>
        [Required]
        public required string EndReason { get; set; }

        /// <summary>Resultado de cada jugador. Debe incluir al usuario que envía el reporte.</summary>
        [Required]
        [MinLength(1)]
        public required List<MatchPlayerRequest> Players { get; set; }
    }
}
