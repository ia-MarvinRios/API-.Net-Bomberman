using API.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using API.DTOs.Common;

namespace API.Controllers
{
    /// <summary>Salud del servicio.</summary>
    [Route("api/v1/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        /// <summary>Indica si el servicio está vivo.</summary>
        /// <remarks>Sin autenticación. Uso exclusivo de monitoreo; no debe usarse en lógica de juego.</remarks>
        /// <response code="200">El servicio responde.</response>
        [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new HealthResponse
            {
                Status = "ok",
                Version = "1.0.0",
                ServerTime = DateTime.UtcNow.ToString("o") // format: ISO 8601
            });
        }
    }
}
