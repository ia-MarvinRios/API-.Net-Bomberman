using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace API.Documentation
{
    internal sealed class ApiDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info = new OpenApiInfo
            {
                Title = "Bomberman API — DerbyMetals",
                Version = "v1",
                Description =
                    "Backend de metajuego para un Bomberman multijugador: cuentas, perfiles, " +
                    "estadísticas, historial de partidas y configuración remota del juego.\n\n" +
                    "**Autenticación:** inicia sesión en `POST /api/v1/auth/login`, copia el `accessToken` " +
                    "y pégalo en el esquema *Bearer* de esta página para probar los endpoints protegidos.\n\n" +
                    "**Errores:** todos los errores 4xx/5xx usan el formato `{ \"error\": { \"code\", \"message\" } }`.\n\n" +
                    "**Fechas:** ISO 8601 en UTC. **Identificadores:** UUID.",
                Contact = new OpenApiContact { Name = "Raydell Ríos" }
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Access token JWT (dura 1 hora). Se obtiene en login, register o refresh."
                }
            };

            document.Tags = new HashSet<OpenApiTag>
            {
                new() { Name = "Auth", Description = "Registro, inicio de sesión, renovación y cierre de sesión. Register, login y refresh tienen un límite de 10 peticiones por minuto por IP." },
                new() { Name = "Profile", Description = "Perfil del jugador, ajustes, cambio de contraseña y de correo." },
                new() { Name = "Stats", Description = "Estadísticas acumuladas del jugador." },
                new() { Name = "Matches", Description = "Reporte de partidas (lo envía el Master Client) e historial del jugador." },
                new() { Name = "GameConfig", Description = "Configuración remota del juego: mapas, skins, balance y versión mínima del cliente." },
                new() { Name = "Health", Description = "Estado del servicio para monitoreo." }
            };

            return Task.CompletedTask;
        }
    }
}
