using API.Data;
using API.DTOs.Stats;
using API.Exceptions;
using API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.DTOs.Common;

namespace API.Controllers
{
    /// <summary>Estadísticas del jugador autenticado.</summary>
    [Route("api/v1/stats")]
    [ApiController]
    [Authorize]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Devuelve las estadísticas acumuladas.</summary>
        /// <remarks>`winRate` es `wins / matchesPlayed` (entre 0 y 1), o 0 si no hay partidas.</remarks>
        /// <response code="200">Estadísticas del jugador.</response>
        [ProducesResponseType<StatsResponse>(StatusCodes.Status200OK)]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.GetUserId();

            var stats = await _context.PlayerStats
                .FirstOrDefaultAsync(s => s.UserId == userId)
                ?? throw new NotFoundException("Stats not found");

            double winRate = stats.MatchesPlayed == 0
                ? 0
                : (double)stats.Wins / stats.MatchesPlayed;

            return Ok(new StatsResponse
            {
                MatchesPlayed = stats.MatchesPlayed,
                Wins = stats.Wins,
                Losses = stats.Losses,
                Draws = stats.Draws,
                Kills = stats.Kills,
                Deaths = stats.Deaths,
                PowerUpsCollected = stats.PowerUpsCollected,
                WinRate = winRate
            });
        }

    }
}
