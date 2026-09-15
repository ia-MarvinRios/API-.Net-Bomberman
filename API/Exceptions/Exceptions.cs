using System.Net;

namespace API.Exceptions
{
    public class ValidationFailedException : ApiException
    {
        public ValidationFailedException(string message)
            : base(HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed, message) { }
    }

    public class InvalidCredentialsException : ApiException
    {
        public InvalidCredentialsException()
            : base(HttpStatusCode.Unauthorized, ErrorCodes.AuthInvalidCredentials, "Invalid credentials") { }
    }

    public class UserAlreadyExistsException : ApiException
    {
        public UserAlreadyExistsException(string message)
            : base(HttpStatusCode.Conflict, ErrorCodes.AuthUserAlreadyExists, message) { }
    }

    public class TokenExpiredException : ApiException
    {
        public TokenExpiredException()
            : base(HttpStatusCode.Unauthorized, ErrorCodes.AuthTokenExpired, "Token expired or invalid") { }
    }

    public class ForbiddenException : ApiException
    {
        public ForbiddenException(string message)
            : base(HttpStatusCode.Forbidden, ErrorCodes.AuthForbidden, message) { }
    }
}