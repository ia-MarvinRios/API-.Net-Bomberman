namespace API.Exceptions
{
    public static class ErrorCodes
    {
        public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
        public const string AuthUserAlreadyExists = "AUTH_USER_ALREADY_EXISTS";
        public const string AuthTokenExpired = "AUTH_TOKEN_EXPIRED";
        public const string AuthUnauthorized = "AUTH_UNAUTHORIZED";
        public const string AuthForbidden = "AUTH_FORBIDDEN";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string RateLimited = "RATE_LIMITED";
        public const string ServerError = "SERVER_ERROR";
    }
}
