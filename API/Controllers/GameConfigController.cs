using API.Data;
using API.DTOs.GameConfig;
using API.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.DTOs.Common;

namespace API.Controllers
{
    /// <summary>Configuración remota del juego.</summary>
    [Route("api/v1/game-config")]
    [ApiController]
    // No auth. Client can request without a session
    public class GameConfigController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GameConfigController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Devuelve la configuración vigente del juego.</summary>
        /// <remarks>
        /// No requiere autenticación, así el cliente puede pedirla antes de iniciar sesión.
        /// `enabledMapIds = null` significa que todos los mapas están habilitados.
        /// El cliente debe comparar su versión con `minClientVersion` y bloquear el juego si es menor.
        /// `configVersion` cambia cada vez que se publica una edición y sirve para invalidar caché local.
        /// </remarks>
        /// <response code="200">Configuración vigente.</response>
        [ProducesResponseType<GameConfigResponse>(StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var config = await _context.GameConfig.FirstOrDefaultAsync(g => g.Id == 1)
                ?? throw new NotFoundException("Game config not found");

            var skins = await _context.Skins
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            return Ok(new GameConfigResponse
            {
                EnabledMapIds = config.EnabledMapIds,
                Skins = skins.Select(s => new SkinDto
                {
                    Id = s.Id,
                    EnabledInSelector = s.EnabledInSelector
                }).ToList(),
                Balance = new BalanceDto
                {
                    FuseSeconds = config.BalanceFuseSeconds,
                    BaseSpeed = config.BalanceBaseSpeed,
                    BaseRange = config.BalanceBaseRange,
                    BaseMaxBombs = config.BalanceBaseMaxBombs,
                    MatchDurationSeconds = config.BalanceMatchDurationSeconds,
                    WallCooldownSeconds = config.BalanceWallCooldownSeconds,
                    WallDurationSeconds = config.BalanceWallDurationSeconds,
                    BombRequestTimeoutSeconds = config.BalanceBombRequestTimeoutSeconds,
                    TimedFuseSeconds = config.BalanceTimedFuseSeconds,
                    RemoteMaxSeconds = config.BalanceRemoteMaxSeconds,
                    ThrowDistanceCells = config.BalanceThrowDistanceCells,
                    ThrowDurationSeconds = config.BalanceThrowDurationSeconds,
                    MaxRange = config.BalanceMaxRange,
                    MaxBombs = config.BalanceMaxBombs,
                    MaxSpeedLevel = config.BalanceMaxSpeedLevel
                },
                MinClientVersion = config.MinClientVersion,
                ConfigVersion = config.ConfigVersion
            });
        }
    }
}
