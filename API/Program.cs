using API.Configuration;
using API.Data;
using API.Exceptions;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Validation failed";

            var response = new
            {
                error = new
                {
                    code = API.Exceptions.ErrorCodes.ValidationFailed,
                    message = firstError
                }
            };

            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddOpenApi();

// -- Database Connection --
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// -- JwtConfig --
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("=== JWT VALIDATION FAILED ===");
                Console.WriteLine(context.Exception.GetType().Name);
                Console.WriteLine(context.Exception.Message);
                return Task.CompletedTask;
            },

            // Triggers when token is missing or invalid
            OnChallenge = async context =>
            {
                context.HandleResponse(); // prevents .Net from using a Default response
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = new
                    {
                        code = ErrorCodes.AuthUnauthorized,
                        message = "Missing or invalid access token"
                    }
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            },

            // Triggers when token is valid but user has no permission (ex: [Authorize(Roles = "Admin")])
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = new
                    {
                        code = ErrorCodes.AuthForbidden,
                        message = "You do not have permission to perform this action"
                    }
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        };
    });

// -- Rate Limit --
builder.Services.AddRateLimiter(options =>
{
    // Uniform response on limit reached
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                code = ErrorCodes.RateLimited,
                message = "Too many requests. Please try again later."
            }
        };

        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response), cancellationToken);
    };

    // 10 requests per minute per IP
    options.AddPolicy("AuthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0, // no queue. Reject on limit reached
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

builder.Services.AddAuthorization();

// -- Register Token Service --
builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

// -- APPLY MIGRATIONS AND EXIT --
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    Console.WriteLine("Applying migrations...");
    db.Database.Migrate();
    Console.WriteLine("Done!");

    return; // Exit
}

// -- Middleware --
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
