using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Matches
{
    /// <summary>Resultado de un jugador dentro de la partida.</summary>
    public class MatchPlayerRequest
    {
        /// <summary>UUID del jugador. Si no corresponde a una cuenta, se ignora para estadísticas y XP.</summary>
        [Required]
        public Guid UserId { get; set; }
        /// <summary>Puesto final: 1 es el ganador y 0 indica empate.</summary>
        /// <example>1</example>
        public int Placement { get; set; }
        /// <summary>Eliminaciones logradas.</summary>
        /// <example>3</example>
        public int Kills { get; set; }
        /// <summary>Veces que murió.</summary>
        /// <example>1</example>
        public int Deaths { get; set; }
        /// <summary>Power-ups recogidos.</summary>
        /// <example>5</example>
        public int PowerUpsCollected { get; set; }
        /// <summary>Si el jugador se desconectó antes de terminar.</summary>
        public bool Disconnected { get; set; }
    }
}
