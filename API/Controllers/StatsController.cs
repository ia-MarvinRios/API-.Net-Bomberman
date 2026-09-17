using API.Data;
using API.DTOs.Stats;
using API.Exceptions;
using API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
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
