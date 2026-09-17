using System.Net;

namespace API.Exceptions
{
    public class NotFoundException : ApiException
    {
        public NotFoundException(string message)
            : base(HttpStatusCode.NotFound, ErrorCodes.NotFound, message) { }
    }
}
