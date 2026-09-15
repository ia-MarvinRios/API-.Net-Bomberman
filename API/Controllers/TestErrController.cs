using API.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestErrController : ControllerBase
    {
        [HttpGet("test-error")]
        public IActionResult TestError()
        {
            throw new InvalidCredentialsException();
        }
    }
}
