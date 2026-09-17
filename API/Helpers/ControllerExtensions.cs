using System.Security.Claims;

namespace API.Helpers
{
    public static class ControllerExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(idClaim!); // always [Authorize]
        }
    }
}
