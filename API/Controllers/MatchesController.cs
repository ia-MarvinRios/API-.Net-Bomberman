using API.Data;
using API.DTOs.Matches;
using API.Exceptions;
using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.DTOs.Common;

namespace API.Controllers
{
    /// <summary>Reporte e historial de partidas.</summary>
    [Route("api/v1/matches")]
    [ApiController]
    [Authorize]
    public class MatchesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MatchesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Reporta el resultado de una partida terminada.</summary>
        /// <remarks>
        /// Lo envía **una sola vez el Master Client** de la sala. Reglas:
        ///
        /// - **Autorización:** el usuario del token debe estar entre los `players`.
        /// - **Idempotencia:** si el `matchId` ya existe, responde 200 con `{}` sin reaplicar nada.
        /// - **XP por jugador:** `50 + kills × 20 + 100 si ganó`.
        /// - **Victoria:** `placement = 1` cuenta como victoria solo si hubo más de un jugador. `placement = 0` es empate.
        /// - Los jugadores cuyo `userId` no exista se ignoran para estadísticas y XP, sin invalidar el reporte.
        /// </remarks>
        /// <param name="request">Datos completos de la partida.</param>
        /// <response code="201">Partida registrada y resultados aplicados.</response>
        /// <response code="200">La partida ya estaba registrada (idempotente). No se aplicó nada nuevo.</response>
        /// <response code="400">`VALIDATION_FAILED`: el cuerpo no cumple el formato.</response>
        /// <response code="403">`AUTH_FORBIDDEN`: el remitente no está entre los jugadores de la partida.</response>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMatchRequest request)
        {
            var senderId = User.GetUserId();

            // Sender must be a player in the match
            bool senderIsInMatch = request.Players.Any(p => p.UserId == senderId);
            if (!senderIsInMatch)
            {
                throw new ForbiddenException("You are not a participant of this match");
            }

            // Don't re-apply anything on an existing match
            bool alreadyExists = await _context.Matches.AnyAsync(m => m.MatchId == request.MatchId);
            if (alreadyExists)
            {
                return Ok(new { });
            }

            var match = new Match
            {
                MatchId = request.MatchId,
                MapId = request.MapId,
                StartedAt = request.StartedAt,
                EndedAt = request.EndedAt,
                EndReason = request.EndReason
            };

            _context.Matches.Add(match);

            var matchPlayerRows = request.Players.Select(p => new MatchPlayer
            {
                MatchId = request.MatchId,
                UserId = p.UserId,
                Placement = p.Placement,
                Kills = p.Kills,
                Deaths = p.Deaths,
                PowerUpsCollected = p.PowerUpsCollected,
                Disconnected = p.Disconnected
            }).ToList();

            _context.MatchPlayers.AddRange(matchPlayerRows);

            bool isMultiplayer = request.Players.Count > 1;

            foreach (var p in request.Players)
            {
                var user = await _context.Users.FindAsync(p.UserId);
                if (user == null)
                {
                    continue; // unknown player
                }

                var stats = await _context.PlayerStats.FirstOrDefaultAsync(s => s.UserId == p.UserId);
                var profile = await _context.Profiles.FirstOrDefaultAsync(pr => pr.UserId == p.UserId);

                if (stats != null)
                {
                    stats.MatchesPlayed += 1;
                    stats.Kills += p.Kills;
                    stats.Deaths += p.Deaths;
                    stats.PowerUpsCollected += p.PowerUpsCollected;

                    bool countsAsWin = isMultiplayer && p.Placement == 1;

                    if (countsAsWin) stats.Wins += 1;
                    else if (p.Placement == 0) stats.Draws += 1;
                    else stats.Losses += 1;
                }

                if (profile != null)
                {
                    int winBonus = (isMultiplayer && p.Placement == 1) ? 100 : 0;
                    profile.Xp += 50 + (p.Kills * 20) + winBonus;
                }
            }

            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new { });
        }

        /// <summary>Devuelve el historial de partidas del jugador, de la más reciente a la más antigua.</summary>
        /// <param name="limit">Cantidad máxima de partidas (1-100, por defecto 20). Los valores fuera de rango se ajustan, no son error.</param>
        /// <response code="200">Lista de partidas.</response>
        [ProducesResponseType<MatchesListResponse>(StatusCodes.Status200OK)]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyMatches([FromQuery] int limit = 20)
        {
            var userId = User.GetUserId();

            // out of range values adjustments
            limit = Math.Clamp(limit, 1, 100);

            var myMatchIds = await _context.MatchPlayers
                .Where(mp => mp.UserId == userId)
                .Join(_context.Matches, mp => mp.MatchId, m => m.MatchId, (mp, m) => m)
                .OrderByDescending(m => m.EndedAt)
                .Take(limit)
                .Select(m => m.MatchId)
                .ToListAsync();

            var matches = await _context.Matches
                .Where(m => myMatchIds.Contains(m.MatchId))
                .ToListAsync();

            var allPlayers = await _context.MatchPlayers
                .Where(mp => myMatchIds.Contains(mp.MatchId))
                .ToListAsync();

            var items = myMatchIds.Select(matchId =>
            {
                var match = matches.First(m => m.MatchId == matchId);
                var playersInMatch = allPlayers.Where(p => p.MatchId == matchId).ToList();
                var myEntry = playersInMatch.First(p => p.UserId == userId);

                return new MatchHistoryItemDto
                {
                    MatchId = match.MatchId,
                    MapId = match.MapId,
                    EndedAt = match.EndedAt,
                    MyPlacement = myEntry.Placement,
                    Players = playersInMatch.Select(p => new MatchPlayerDto
                    {
                        UserId = p.UserId,
                        Placement = p.Placement,
                        Kills = p.Kills,
                        Deaths = p.Deaths,
                        PowerUpsCollected = p.PowerUpsCollected,
                        Disconnected = p.Disconnected
                    }).ToList()
                };
            }).ToList();

            return Ok(new MatchesListResponse
            {
                Items = items,
                Total = items.Count
            });
        }

    }
}
