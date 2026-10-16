<div align="center">

# 🎮 Bomberman API — DerbyMetals

### Backend de metajuego para un Bomberman multijugador: cuentas, perfiles, estadísticas, partidas y configuración remota.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-black?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-9.0-512BD4?style=for-the-badge)
![OpenAPI](https://img.shields.io/badge/OpenAPI-3.1-6BA539?style=for-the-badge&logo=openapiinitiative&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger%20UI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![Postman](https://img.shields.io/badge/Postman-FF6C37?style=for-the-badge&logo=postman&logoColor=white)

</div>

---

## 📖 Descripción

API REST que actúa como **backend de metajuego** para un videojuego estilo Bomberman multijugador. No maneja la lógica de la partida en tiempo real (eso vive en el cliente y se sincroniza por Photon) — se encarga de todo lo que persiste entre sesiones: cuentas, progreso del jugador, estadísticas, historial de partidas y configuración remota del balance del juego.

Implementada siguiendo estrictamente las especificaciones `API-CONTRACT.md` y `API-SPEC.md` del proyecto.

## ⚙️ Stack Tecnológico

- **.NET 10** — Framework principal
- **ASP.NET Core Web API** (Controllers) — Arquitectura REST
- **Entity Framework Core 9.0** + **Pomelo.EntityFrameworkCore.MySql** — ORM y proveedor de MySQL
- **MySQL** — Base de datos relacional (XAMPP en desarrollo local)
- **JWT Bearer** — Autenticación con access tokens (1h) + refresh tokens opacos rotativos (30 días)
- **BCrypt.Net-Next** — Hashing de contraseñas
- **Microsoft.AspNetCore.OpenApi** — Generación nativa del documento OpenAPI
- **Scalar.AspNetCore** y **Swagger UI** — Dos visores interactivos para la misma documentación
- **Rate Limiting nativo de .NET** — Protección contra fuerza bruta en endpoints de autenticación

## ✨ Características

- 🔐 Autenticación completa con rotación de refresh tokens y detección de reuso
- 👤 Gestión de perfil, ajustes de jugador, cambio de contraseña y correo
- 📊 Estadísticas acumuladas y sistema de experiencia/nivel
- 🎮 Reporte de partidas idempotente, con validación de pertenencia
- 🛠️ Configuración remota del balance del juego, versionada
- 📚 Documentación OpenAPI completa: descripciones, códigos de respuesta, esquemas de error y autenticación Bearer
- 🚦 Formato de error uniforme en toda la API: `{ "error": { "code", "message" } }`
- 🛡️ Rate limiting, contraseñas hasheadas, y JWT con eventos personalizados

## 📁 Estructura del proyecto

```
API/
├── Configuration/       # JwtSettings
├── Controllers/         # Auth, Profile, Stats, Matches, GameConfig, Health
├── Data/                # AppDbContext
├── Documentation/       # Transformers de OpenAPI (info general y seguridad Bearer)
├── DTOs/                # Contratos de entrada/salida por dominio
├── Exceptions/          # ApiException, ErrorCodes, excepciones especificas
├── Helpers/             # ControllerExtensions, LevelCalculator
├── Middleware/          # ExceptionHandlingMiddleware
├── Migrations/          # Historial de migraciones de EF Core
├── Models/              # Entidades: User, Profile, PlayerSettings, RefreshToken,
│                         # PlayerStats, Match, MatchPlayer, Skin, GameConfig
├── Services/            # ITokenService / TokenService
└── Program.cs
```

## 🚀 Cómo levantar el proyecto

### Prerrequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL corriendo localmente (por ejemplo, vía XAMPP) o accesible remotamente

### 1. Clonar y restaurar

```bash
git clone https://github.com/ia-MarvinRios/API-.Net-Bomberman.git
cd API-.Net-Bomberman/API
dotnet restore
```

### 2. Configurar `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=derbymetals;User=root;Password=;Port=3306"
  },
  "Jwt": {
    "Key": "TU_CLAVE_SECRETA_DE_AL_MENOS_32_CARACTERES",
    "Issuer": "ApiBomberman",
    "Audience": "ApiBombermanUsers",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

> ⚠️ `Jwt:Key` debe medir **al menos 32 caracteres** (256 bits) para el algoritmo HS256. Genera una clave aleatoria segura con:
> ```powershell
> [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
> ```

### 3. Aplicar las migraciones

```bash
dotnet ef database update
```

Esto crea la base de datos `derbymetals` con las 9 tablas necesarias, incluyendo el seed inicial de `GameConfig` y `Skins`.

### 4. Correr la API

```bash
dotnet run
```

La API queda disponible en las URLs indicadas en consola. La documentación interactiva está descrita en la siguiente sección.

## 📚 Documentación de la API

La API genera su propio documento **OpenAPI** y lo expone en tres formatos. Reemplaza `<puerto>` por el HTTPS que indique la consola al arrancar.

| Visor | URL | Uso |
|---|---|---|
| 🟣 **Scalar** | `https://localhost:<puerto>/scalar/v1` | Documentación interactiva moderna |
| 🟢 **Swagger UI** | `https://localhost:<puerto>/swagger` | Interfaz clásica de Swagger |
| 📄 **OpenAPI JSON** | `https://localhost:<puerto>/openapi/v1.json` | Documento crudo, importable en Postman o [editor.swagger.io](https://editor.swagger.io) |

Cada endpoint incluye su descripción, reglas de negocio, códigos de respuesta posibles (con el esquema de error uniforme) y ejemplos de los campos de entrada.

### 🔑 Probar endpoints protegidos desde la documentación

1. Ejecuta `POST /api/v1/auth/login` (o `register`) y copia el `accessToken` de la respuesta.
2. Pulsa **Authorize** (Swagger UI) o abre la sección de autenticación **Bearer** (Scalar).
3. Pega el token. Los endpoints con candado 🔒 ya lo enviarán automáticamente.

> 💡 Solo los endpoints que requieren autenticación muestran candado. `register`, `login`, `refresh`, `game-config` y `health` son públicos.

### ⚠️ Disponibilidad por entorno

La documentación **solo se expone con `ASPNETCORE_ENVIRONMENT=Development`**, que es lo que usan `dotnet run` y Visual Studio. El `.exe` publicado arranca en `Production` por defecto, así que no la incluye. Para verla desde el ejecutable publicado:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
.\API.exe
```

### ✍️ Documentar un endpoint nuevo

La documentación se alimenta de los comentarios XML (`///`) y de los atributos `[ProducesResponseType]` de cada acción. Al agregar un endpoint, sigue el mismo patrón que los existentes:

```csharp
/// <summary>Descripción corta de lo que hace.</summary>
/// <remarks>Reglas de negocio y detalles relevantes.</remarks>
/// <response code="200">Qué significa este resultado.</response>
/// <response code="400">`VALIDATION_FAILED`: cuándo ocurre.</response>
[ProducesResponseType<MiRespuesta>(StatusCodes.Status200OK)]
[ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
```

Los comentarios se procesan al **compilar**, así que recompila para ver los cambios.

## 📡 Endpoints

Todas las rutas cuelgan del prefijo `/api/v1`.

| Método | Ruta | Auth | Descripción |
|---|---|:---:|---|
| POST | `/auth/register` | ❌ | Crear cuenta |
| POST | `/auth/login` | ❌ | Iniciar sesión |
| POST | `/auth/refresh` | ❌ | Rotar refresh token |
| POST | `/auth/logout` | ✅ | Cerrar sesión |
| GET | `/profile/me` | ✅ | Ver perfil propio |
| PATCH | `/profile/me` | ✅ | Editar perfil |
| GET | `/profile/me/settings` | ✅ | Ver ajustes |
| PUT | `/profile/me/settings` | ✅ | Reemplazar ajustes |
| PUT | `/profile/me/password` | ✅ | Cambiar contraseña |
| PUT | `/profile/me/email` | ✅ | Cambiar correo |
| GET | `/stats/me` | ✅ | Ver estadísticas |
| POST | `/matches` | ✅ | Reportar resultado de partida |
| GET | `/matches/me` | ✅ | Ver historial de partidas |
| GET | `/game-config` | ❌ | Configuración remota del juego |
| GET | `/health` | ❌ | Estado del servicio |

## 📦 Publicación

### Generar el ejecutable

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

### Aplicar migraciones desde el ejecutable publicado

El mismo `.exe` incluye un modo especial para aplicar migraciones sin levantar el servidor web — útil para preparar la base de datos en un entorno nuevo antes del primer arranque:

```powershell
.\API.exe --migrate
```

### Correr la API publicada

```powershell
.\API.exe
```

> 💡 Si necesitas HTTPS al correr el `.exe` directamente (fuera de Visual Studio), define los endpoints de Kestrel explícitamente en `appsettings.json`:
> ```json
> {
>   "Kestrel": {
>     "Endpoints": {
>       "Http": { "Url": "http://localhost:5000" },
>       "Https": { "Url": "https://localhost:5001" }
>     }
>   }
> }
> ```

## 🧪 Pruebas

El proyecto se probó exhaustivamente en Postman, cubriendo:

- Flujo completo de autenticación (register → login → refresh → logout)
- Rotación de refresh tokens y detección de reuso
- Reglas de negocio de partidas (idempotencia por `matchId`, autorización por pertenencia, cálculo de XP/nivel)
- Validaciones de perfil y ajustes (rangos, formatos, unicidad de email/username)
- Rate limiting en endpoints de autenticación (10 peticiones/minuto por IP)

## 🛡️ Seguridad

- Contraseñas hasheadas con **BCrypt**, nunca en texto plano
- Refresh tokens hasheados con **SHA-256** antes de persistirse
- `POST /auth/login` nunca revela si falló el usuario o la contraseña
- Rate limiting en `register`, `login` y `refresh`
- Formato de error uniforme en toda la API, sin filtrar detalles internos en errores `500`
- HTTPS habilitado mediante `UseHttpsRedirection`
- Documentación de la API expuesta únicamente en el entorno `Development`