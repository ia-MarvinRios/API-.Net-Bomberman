using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using API.Data;
using API.Middleware;
using API.Configuration;
using API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// -- Database Connection --
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// -- JwtConfig --
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// -- Register Token Service --
builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

// -- Middleware --
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
