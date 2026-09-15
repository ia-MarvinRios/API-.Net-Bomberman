using API.Exceptions;
using System.Net;
using System.Text.Json;

namespace API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ApiException ex)
            {
                await WriteErrorResponse(
                    context,
                    ex.StatusCode,
                    ex.ErrorCode,
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error at {Path}", context.Request.Path);

                await WriteErrorResponse(
                    context,
                    HttpStatusCode.InternalServerError,
                    ErrorCodes.ServerError,
                    "An unexpected error ocurred"
                );
            }
        }

        private static async Task WriteErrorResponse(
            HttpContext context, HttpStatusCode statusCode, string errorCode, string message
        )
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new 
            {
                error = new
                {
                    code = errorCode,
                    message = message
                }
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            await context.Response.WriteAsync(json);
        }

    }
}
