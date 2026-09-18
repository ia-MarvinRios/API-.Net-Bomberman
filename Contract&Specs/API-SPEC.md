# API-SPEC v1 - DerbyMetals

Este documento especifica el comportamiento esperado del backend real de DerbyMetals: para que sirve, como se estructura, que datos guarda, que valida cada endpoint, como se protege, como se versiona y como debe probarse. El contrato JSON exacto (peticion/respuesta/errores por endpoint) esta en `docs/api/API-CONTRACT.md`; este documento explica el porque y el como detras de ese contrato. Mientras el backend real no existe, el cliente Unity usa un servidor falso en memoria (`Assets/Scripts/Api/Fake/`) que implementa un subconjunto de estas reglas para desarrollo y tests; las diferencias puntuales entre el fake y esta especificacion se marcan explicitamente mas abajo.

## 1. Proposito

El API expone, sobre HTTPS y JSON, todo lo que el juego necesita fuera de la partida en tiempo real (que se sigue jugando por PUN2/Photon, sin pasar por este API):

- Registro, login y sesion de la cuenta del jugador (tokens JWT).
- Perfil del jugador: nombre visible y skin seleccionada.
- Ajustes del jugador: sonido, calidad de graficos, FPS destino y umbral de gesto tactil, persistidos entre dispositivos.
- Estadisticas acumuladas y el historial de partidas jugadas.
- Reporte del resultado de una partida (lo envia una sola vez el Master Client de la sala Photon al terminar).
- Configuracion remota del juego (mapas habilitados, skins disponibles, balance, version minima de cliente).
- Un endpoint de salud para monitoreo.

No es responsable de la logica de la partida en si (movimiento, bombas, colisiones): eso vive en el cliente y se sincroniza por Photon (plan 02). El API es el "backend de metajuego": cuentas, progreso y configuracion.

## 2. Arquitectura esperada

```
[Base de datos relacional] <-- SQL --> [API HTTP (JSON)] <-- HTTPS/JSON --> [Cliente Unity]
```

- **Base de datos**: almacenamiento relacional persistente (por ejemplo PostgreSQL). Es la unica fuente de verdad de cuentas, perfiles, estadisticas y partidas. El API nunca expone acceso directo a la base de datos al cliente.
- **API**: proceso stateless (sin sesion en memoria de request a request) que valida, autentica, aplica las reglas de negocio de esta especificacion y traduce entre la base de datos y el contrato JSON de `API-CONTRACT.md`. Puede escalar horizontalmente porque toda sesion se resuelve con el JWT que llega en cada request, no con estado local del proceso.
- **Cliente Unity**: consume el API a traves de `Bomberman.Api` (ver seccion 10). Nunca habla directo con la base de datos ni con Photon usa el API para nada de la partida en tiempo real; el API tampoco sabe nada de Photon.
- El API y la base de datos corren en la infraestructura del servidor (fuera de este repositorio de Unity); este repo solo define el contrato y el cliente que lo consume.

## 3. Modelo de datos

Tablas minimas para soportar el contrato. Los tipos son logicos (el motor de base de datos concreto puede variar los nombres exactos).

### `users`
| Columna | Tipo logico | Notas |
|---|---|---|
| id | uuid (PK) | Identificador estable del jugador; es el `userId` del contrato |
| email | string, unico | Validado como email; comparaciones case-insensitive |
| username | string, unico | 3-16 caracteres alfanumericos; unico sin distinguir mayusculas de minusculas |
| password_hash | string | Hash de la contrasena (nunca texto plano); ver seccion 5 |
| created_at | timestamp UTC | Fecha de alta de la cuenta |

### `profiles`
| Columna | Tipo logico | Notas |
|---|---|---|
| user_id | uuid (PK, FK users.id) | Relacion 1 a 1 con `users` |
| display_name | string | 3-16 caracteres; por defecto igual a `username` al registrarse |
| selected_skin_id | string (FK skins.id) | Skin activa; debe estar en `unlocked_skin_ids` |
| unlocked_skin_ids | json/array de string | Skins desbloqueadas por el jugador; incluye `default` desde el alta |
| xp | integer | Experiencia acumulada; nunca negativa |

`level` y `xp_to_next_level` no se guardan: se calculan al vuelo a partir de `xp` (ver seccion 4, regla de nivel).

### `player_settings`
| Columna | Tipo logico | Notas |
|---|---|---|
| user_id | uuid (PK, FK users.id) | Relacion 1 a 1 con `users` |
| sound_enabled | boolean | Por defecto `true` |
| sfx_volume | float | Acotado en [0, 1]; por defecto `0.8` |
| graphics_quality | string enum (`low`, `high`) | Por defecto `high` |
| target_fps | integer enum (`30`, `60`) | Por defecto `60` |
| swipe_threshold_cm | float | Acotado en [0.4, 1.0]; por defecto `0.6` |

Al crear la cuenta se crea tambien la fila de `player_settings` con estos valores por defecto, con el mismo criterio que `profiles` y `player_stats` en el registro.

### `refresh_tokens`
| Columna | Tipo logico | Notas |
|---|---|---|
| id | uuid (PK) | |
| user_id | uuid (FK users.id) | |
| token_hash | string | Hash del token opaco (el valor en claro solo lo tiene el cliente) |
| created_at | timestamp UTC | |
| expires_at | timestamp UTC | `created_at` + 30 dias |
| revoked_at | timestamp UTC, nullable | Se rellena al hacer logout o al rotar el token |
| replaced_by_token_id | uuid, nullable (FK refresh_tokens.id) | Enlaza con el token que lo sustituyo al rotar |

### `player_stats`
| Columna | Tipo logico | Notas |
|---|---|---|
| user_id | uuid (PK, FK users.id) | Relacion 1 a 1 con `users` |
| matches_played | integer | |
| wins | integer | |
| losses | integer | |
| draws | integer | |
| kills | integer | |
| deaths | integer | |
| power_ups_collected | integer | |

`win_rate` no se guarda: se calcula como `wins / matches_played` (0 si `matches_played` es 0).

### `matches`
| Columna | Tipo logico | Notas |
|---|---|---|
| match_id | uuid (PK) | Lo genera el cliente (Master Client) antes de reportar; el API no lo regenera |
| map_id | string | Identificador del mapa jugado |
| started_at | timestamp UTC | |
| ended_at | timestamp UTC | |
| end_reason | string enum (`lastAlive`, `timeout`, `allDisconnected`) | |
| reported_at | timestamp UTC | Cuando llego el reporte al API (para auditoria, no viaja en el contrato) |

### `match_players`
| Columna | Tipo logico | Notas |
|---|---|---|
| id | uuid o bigint (PK) | |
| match_id | uuid (FK matches.match_id) | |
| user_id | uuid (FK users.id) | |
| placement | integer | 1..4, o 0 para empate |
| kills | integer | |
| deaths | integer | |
| power_ups_collected | integer | |
| disconnected | boolean | |

### `skins`
| Columna | Tipo logico | Notas |
|---|---|---|
| id | string (PK) | Identificador estable (`default`, `skin_01`, ...) |
| enabled_in_selector | boolean | Si aparece como elegible en el selector de skin |
| display_name | string | Nombre mostrado en UI (no viaja en `GET /game-config`, es de administracion) |
| sort_order | integer | Orden de presentacion en el selector |

### `game_config`
Fila unica (singleton) con la configuracion remota vigente.
| Columna | Tipo logico | Notas |
|---|---|---|
| config_version | integer | Se incrementa en cada cambio publicado |
| enabled_map_ids | json/array de string, nullable | `null` significa sin restriccion (todos los mapas habilitados) |
| min_client_version | string (semver) | Version minima de cliente aceptada |
| balance_fuse_seconds | float | |
| balance_base_speed | float | |
| balance_base_range | integer | |
| balance_base_max_bombs | integer | |
| balance_match_duration_seconds | integer | |
| balance_wall_cooldown_seconds | float | |
| balance_wall_duration_seconds | float | |
| balance_bomb_request_timeout_seconds | float | Timeout de la bomba pendiente en el cliente (mayor que el viaje real, menor que la mecha) |
| balance_timed_fuse_seconds | float | Mecha del modo Temporizada |
| balance_remote_max_seconds | float | Tope de seguridad de la bomba Remota |
| balance_throw_distance_cells | integer | Celdas que recorre una bomba lanzada |
| balance_throw_duration_seconds | float | Duracion del vuelo de una bomba lanzada |
| balance_max_range | integer | Tope de rango por Flama |
| balance_max_bombs | integer | Tope de bombas por Bomba extra |
| balance_max_speed_level | integer | Tope de nivel de Velocidad |

Los campos `balance_*` conforman el objeto `balance` de `GET /game-config`; se modelan como columnas (o un unico JSON) porque cambian juntos y rara vez de forma individual.

## 4. Reglas de negocio por endpoint

### POST /auth/register
- `email`: formato de email valido, unico en `users.email` (comparacion case-insensitive).
- `username`: 3-16 caracteres alfanumericos, unico en `users.username` (comparacion case-insensitive: `Alvaro` y `alvaro` son el mismo username).
- `password`: minimo 8 caracteres; se guarda como hash (bcrypt o argon2), nunca en texto plano.
- Si `email` o `username` ya existen: `409 AUTH_USER_ALREADY_EXISTS`.
- Si algun campo no cumple el formato: `400 VALIDATION_FAILED`.
- Al crear la cuenta se crea tambien la fila de `profiles` (`display_name` = `username`, `selected_skin_id` = `default`, `unlocked_skin_ids` = `["default"]`, `xp` = 0) y la de `player_stats` en cero.
- Devuelve el par de tokens igual que login (el registro deja al usuario autenticado).

### POST /auth/login
- Acepta `emailOrUsername` contra `users.email` o `users.username` (case-insensitive).
- Compara `password` contra el hash guardado.
- Si el usuario no existe o la contrasena no coincide, la respuesta es identica en ambos casos: `401 AUTH_INVALID_CREDENTIALS`. El API nunca debe indicar cual de las dos cosas fallo (ver seccion 5).
- Emite un nuevo `accessToken` (JWT, 1 hora) y un nuevo `refreshToken` (opaco, 30 dias), y revoca cualquier `refreshToken` previo del usuario que hubiera quedado activo por una sesion anterior no cerrada explicitamente (login no acumula sesiones infinitas).

### POST /auth/refresh
- Busca `refresh_tokens` por el hash del token recibido, no expirado y no revocado.
- Si no lo encuentra o esta expirado/revocado: `401 AUTH_TOKEN_EXPIRED`.
- Si es valido: rota el token. Emite un `accessToken` y un `refreshToken` nuevos, marca el token recibido como `revoked_at` = ahora y `replaced_by_token_id` apuntando al nuevo. El token anterior deja de servir aunque no haya expirado (rotacion, no reuso).
- Reuso de un refresh token ya rotado (alguien reenvia un token viejo) se trata igual que expirado: `401 AUTH_TOKEN_EXPIRED`. Es una senal de posible robo de token; el backend puede optar por revocar toda la cadena de tokens del usuario en ese caso.

### POST /auth/logout
- Revoca (`revoked_at` = ahora) el `refreshToken` recibido si existe y esta vivo.
- Idempotente: si el token ya no existe o ya estaba revocado, igual responde `204` sin error. Llamarlo dos veces con el mismo token no falla la segunda vez.
- Requiere `Authorization: Bearer` valido (el access token identifica quien cierra sesion), aunque el `refreshToken` del body es lo que efectivamente se revoca.

### GET /profile/me
- Devuelve `profiles` del usuario autenticado (por `userId` del JWT) combinado con `level`, `xp` y `xpToNextLevel` calculados (ver formula de nivel mas abajo).
- Requiere autenticacion; sin token o token invalido: `401 AUTH_UNAUTHORIZED`.

### PATCH /profile/me
- Ambos campos son opcionales; solo se actualiza lo que llega en el body (los campos ausentes no se tocan).
- `displayName`: si llega, debe tener 3-16 caracteres, si no `400 VALIDATION_FAILED`.
- `selectedSkinId`: si llega, debe estar presente en `unlocked_skin_ids` del usuario; si no lo esta, `400 VALIDATION_FAILED` ("skin no desbloqueada"). El API no desbloquea skins en este endpoint, solo permite seleccionar entre las ya desbloqueadas.
- Devuelve el perfil actualizado con la misma forma que `GET /profile/me`.

### GET /profile/me/settings
- Devuelve `player_settings` del usuario autenticado. Si la cuenta no tiene fila todavia (cuentas creadas antes de que existiera este endpoint), se crea con los valores por defecto de la seccion 3 la primera vez que se pide y esa fila queda persistida.
- Requiere autenticacion; sin token o token invalido: `401 AUTH_UNAUTHORIZED`.

### PUT /profile/me/settings
- Reemplazo completo: a diferencia de `PATCH /profile/me`, el body debe traer los cinco campos; no es una actualizacion parcial.
- Validacion de rangos, todo o nada (si algo falla no se guarda nada): `sfxVolume` en [0,1], `swipeThresholdCm` en [0.4,1.0], `targetFps` en {30,60}, `graphicsQuality` en {"low","high"}. Si el body es nulo o algun campo esta fuera de rango: `400 VALIDATION_FAILED`.
- Si pasa la validacion, reemplaza `player_settings` por completo y devuelve el mismo objeto guardado (misma forma que `GET /profile/me/settings`).
- Requiere autenticacion; sin token o token invalido: `401 AUTH_UNAUTHORIZED`.

### PUT /profile/me/password
- Requiere `currentPassword` (contrasena actual) y `newPassword`; si falta alguno de los dos: `400 VALIDATION_FAILED`.
- Verifica `currentPassword` contra el hash guardado en `users.password_hash`; si no coincide, `401 AUTH_INVALID_CREDENTIALS`.
- `newPassword` debe tener minimo 8 caracteres, igual que en el registro; si no, `400 VALIDATION_FAILED`.
- Si pasa la validacion, guarda el hash de `newPassword` y **revoca el refresh token activo del usuario** (marca `revoked_at` = ahora en `refresh_tokens`, equivalente a cerrar cualquier otra sesion que dependiera de ese refresh token); no emite tokens nuevos como parte de esta respuesta.
- **El access token con el que se hizo esta peticion sigue siendo valido** hasta que expire por su propio `exp` (1 hora): cambiar la contrasena no invalida la sesion en curso, solo impide renovarla con el refresh token anterior. Cualquier intento de `POST /auth/refresh` con ese refresh token responde `401 AUTH_TOKEN_EXPIRED` a partir de este momento.
- Responde `204` sin cuerpo.
- Requiere autenticacion; sin token o token invalido: `401 AUTH_UNAUTHORIZED`.

### PUT /profile/me/email
- Requiere `currentPassword` (para confirmar identidad, ya que cambiar el email es sensible) y `newEmail`; si falta alguno de los dos: `400 VALIDATION_FAILED`.
- Verifica `currentPassword` contra el hash guardado; si no coincide, `401 AUTH_INVALID_CREDENTIALS`.
- `newEmail` debe tener formato de email valido; si no, `400 VALIDATION_FAILED`.
- Si `newEmail` ya pertenece a otro usuario (`users.email`, comparacion case-insensitive), `409 AUTH_USER_ALREADY_EXISTS`. Si pertenece al mismo usuario que hace la peticion, no es un conflicto (permite reenviar el mismo valor).
- Si pasa la validacion, actualiza `users.email` y responde `200` con `{ "email": "..." }`. No afecta a los tokens vigentes (a diferencia de `PUT /profile/me/password`, cambiar el email no revoca nada).
- Requiere autenticacion; sin token o token invalido: `401 AUTH_UNAUTHORIZED`.

### GET /stats/me
- Devuelve `player_stats` del usuario autenticado con `winRate` calculado (`wins / matchesPlayed`, `0` si `matchesPlayed` es `0`).

### POST /matches
- Solo lo envia el Master Client de la sala Photon una vez, al terminar la partida (no cada jugador).
- **Idempotencia por `matchId`**: si `matches.match_id` ya existe, el API no vuelve a aplicar el resultado (no duplica XP ni estadisticas) y responde `200` con cuerpo vacio `{}`. Si es la primera vez, aplica el resultado y responde `201` con cuerpo vacio `{}`. Esto protege contra reintentos de red del cliente.
- **Autorizacion por pertenencia**: el `userId` del JWT que envia la peticion debe estar presente en la lista `players` del body; si no lo esta, `403 AUTH_FORBIDDEN`. Evita que un cliente reporte partidas en las que no participo.
- Validacion: `matchId` y al menos un elemento en `players` son obligatorios; si faltan, `400 VALIDATION_FAILED`.
- `placement`: `1` a `4` (posicion final) o `0` para indicar empate. `1` cuenta como victoria solo si hay mas de un jugador en la partida (no hay "victoria" en partida de un solo jugador contra nadie).
- Por cada jugador de `players` que exista en `users`, se actualiza `player_stats`: `matches_played += 1`, `kills += kills`, `deaths += deaths`, `power_ups_collected += powerUpsCollected`, y `wins`/`losses`/`draws` segun `placement` (`1` gana, `0` empata, cualquier otro valor pierde).
- **Formula de XP** aplicada a `profiles.xp` de cada jugador reportado: `xp += 50 + (kills * 20) + (100 si placement == 1, si no 0)`. Es decir, 50 XP fijos por jugar, 20 XP por cada kill, y 100 XP extra por ganar.
- **Formula de nivel** (derivada, no almacenada, usada tambien en `GET /profile/me`): `level = 1 + floor(xp / 500)`, y `xpToNextLevel = level * 500 - xp`. Ejemplo: `xp = 1250` da `level = 3` y `xpToNextLevel = 250`.
- Jugadores listados en `players` que no correspondan a ningun `userId` conocido se ignoran para estadisticas/XP pero no invalidan el reporte completo (partida contra invitados o datos parciales no rompe el guardado del resto).

> Nota de implementacion: el servidor falso (`FakeApiRoutes.Register`, ruta `POST /matches`) implementa la idempotencia por `matchId`, la formula de XP y la formula de nivel exactamente como se describe arriba, pero **no** valida todavia que el remitente este entre `players` (no emite `403 AUTH_FORBIDDEN`); es una simplificacion aceptada para desarrollo local y debe implementarse en el backend real segun esta especificacion y segun `API-CONTRACT.md`.

### GET /matches/me
- `limit` es un query param opcional, entre 1 y 100 (por defecto 20); valores fuera de rango se ajustan al limite mas cercano, no son un error.
- Devuelve las partidas del usuario autenticado ordenadas de mas reciente a mas antigua, con `myPlacement` resuelto para ese usuario dentro de `players`.
- `total` es la cantidad de items devueltos en esta pagina (no el total historico global) mientras no se agregue paginacion real; los clientes deben tratarlo como el tamano de `items`.

### GET /game-config
- Sin autenticacion; el cliente lo puede pedir antes de tener sesion (por ejemplo para saber la version minima antes de dejar loguearse).
- `enabledMapIds`: `null` significa "sin restriccion", es decir, todos los mapas del cliente estan habilitados. Una lista con ids significa que solo esos mapas se deben ofrecer.
- `minClientVersion`: el cliente debe comparar su propia version contra este valor; si es menor, debe bloquear el juego y pedir actualizar (la logica de bloqueo vive en el cliente, el API solo informa el valor).
- `configVersion` cambia cada vez que se publica una edicion de la config; el cliente puede usarlo para invalidar cache local.

### GET /health
- Sin autenticacion. Uso exclusivo de monitoreo/orquestacion (liveness/readiness probes), nunca de logica de juego.
- Debe responder rapido y sin tocar la base de datos si es posible (o con una comprobacion minima, ej. `SELECT 1`), para no dar falsos negativos bajo carga de la base de datos.

### Divergencias conocidas del servidor falso

El servidor falso del cliente Unity (`Assets/Scripts/Api/Fake/`) es una simplificacion para desarrollo local y para tests. Estas son **todas** las divergencias respecto a esta especificacion; el backend real debe implementar lo que dice la spec, no lo que hace el fake:

1. **Skins desbloqueadas al registrar**: el fake crea el usuario con `unlockedSkinIds = ["default", "skin_01", "skin_02", "skin_03"]` (`FakeUserRecord.UnlockedSkinIds`) para poder probar el selector de skins del plan 02 sin economia ni desbloqueos. El backend real debe crear el perfil con `unlocked_skin_ids = ["default"]` como dice `POST /auth/register`.
2. **Validacion de email**: el fake solo comprueba que el email contenga `@` (`FakeApiRoutes.ValidateRegister`). El backend real debe validar formato de email completo.
3. **Validacion de username**: el fake solo comprueba la longitud 3-16. El backend real debe exigir ademas que sea alfanumerico.
4. **Autorizacion por pertenencia en `POST /matches`**: el fake no valida que el `userId` del token este en la lista `players` y por tanto nunca emite `403 AUTH_FORBIDDEN` (ver nota de implementacion mas arriba). El backend real debe validarlo.
5. **Validacion de email en `PUT /profile/me/email`**: igual que en el registro (punto 2), el fake solo comprueba que `newEmail` contenga `@` (`FakeApiRoutes.Register`, handler de `ProfileMeEmail`). El backend real debe validar formato de email completo.

## 5. Seguridad

- Todo el trafico va sobre HTTPS; el API no debe aceptar HTTP en produccion.
- Contrasenas: hash con bcrypt o argon2 (nunca MD5/SHA plano, nunca texto plano). El campo `password` nunca aparece en logs.
- Tokens: `accessToken` es un JWT firmado con expiracion de 1 hora; `refreshToken` es un valor opaco (no JWT) con expiracion de 30 dias, rotativo (cada uso emite uno nuevo e invalida el anterior) y revocable (logout, o deteccion de reuso).
- Rate limiting en los endpoints de `Auth` (`register`, `login`, `refresh`): 10 peticiones por minuto por IP. Al superarlo, `429 RATE_LIMITED`. Protege contra fuerza bruta de credenciales y contra abuso de creacion de cuentas.
- `POST /auth/login` no debe revelar si el fallo fue por email/username inexistente o por contrasena incorrecta: siempre `401 AUTH_INVALID_CREDENTIALS` con el mismo mensaje generico, para no permitir enumerar cuentas validas.
- Autorizacion: cualquier endpoint marcado "auth" en `API-CONTRACT.md` exige `Authorization: Bearer <accessToken>` valido y no expirado; si falta o es invalido, `401 AUTH_UNAUTHORIZED`. Acciones que ademas dependen de pertenencia (como `POST /matches`) devuelven `403 AUTH_FORBIDDEN` cuando el usuario esta autenticado pero no tiene permiso sobre ese recurso especifico.
- Los `userId` en las respuestas son UUIDs opacos; no se exponen IDs incrementales que permitan estimar el numero total de cuentas.

## 6. Versionado

- El API se versiona por path: `/api/v1`. Todas las rutas de `API-CONTRACT.md` cuelgan de ese prefijo.
- Cambios compatibles hacia atras (agregar un campo opcional a una respuesta, agregar un endpoint nuevo, agregar un `error.code` nuevo) se publican dentro de `v1` sin romper clientes existentes; el cliente Unity debe ignorar campos JSON desconocidos.
- Cambios incompatibles (quitar o renombrar un campo, cambiar el tipo de un campo, cambiar el significado de un codigo de error existente, quitar un endpoint) requieren una nueva version `/api/v2`; `v1` se mantiene disponible durante un periodo de transicion acordado con el equipo de cliente.
- `minClientVersion` (de `GET /game-config`) es el mecanismo para forzar actualizaciones de cliente cuando un cambio de version de API lo requiere.

## 7. CORS

Irrelevante para v1: el unico consumidor es el cliente nativo de Unity (`UnityWebRequest`), que no esta sujeto a la politica de mismo origen del navegador. El API no necesita configurar cabeceras CORS. Si en el futuro se agrega un panel de administracion web u otro cliente basado en navegador, ese consumo debera configurarse con una lista explicita de origenes permitidos en ese momento; no se habilita CORS abierto (`*`) preventivamente.

## 8. Logging y observabilidad

Minimo esperado para v1:
- Por cada peticion: metodo, ruta, codigo de estado HTTP, latencia en ms, y `userId` si la peticion estaba autenticada. Sin body, sin headers de autenticacion, sin contrasenas ni tokens en texto plano.
- Los errores `5xx` se loguean con detalle suficiente para depurar (stack trace del lado servidor), pero la respuesta al cliente nunca incluye ese detalle (solo `{ "error": { "code": "SERVER_ERROR", "message": "..." } }` generico).
- Metricas minimas agregadas por endpoint: conteo de peticiones, tasa de error (proporcion de respuestas 4xx/5xx) y latencia (p50/p95). Sirven para detectar regresiones antes de que las reporten los jugadores.
- Los intentos fallidos de login y los `429 RATE_LIMITED` se loguean de forma distinguible (permiten detectar ataques de fuerza bruta).

Del lado del cliente Unity, `Bomberman.Api` ya sigue una convencion equivalente que el backend deberia espejar: `HttpApiClient` (`Assets/Scripts/Api/Client/HttpApiClient.cs`) loguea cada peticion y su resultado con `GameLog.Info`/`GameLog.Warn` bajo el canal `LogChannel.Api`, sin loguear el cuerpo de la peticion (por lo tanto nunca la contrasena ni los tokens), y solo en Editor/builds de desarrollo (`GameLog` es condicional, ver `Assets/Scripts/Core/Logging/GameLog.cs`).

## 9. Entorno de pruebas

El entorno de pruebas del backend real (staging/QA) debe traer un seed fijo de 4 cuentas para pruebas manuales y automatizadas de extremo a extremo, sin depender de que alguien las cree a mano:

| username | email | password |
|---|---|---|
| test1 | test1@derbymetals.dev | test1234 |
| test2 | test2@derbymetals.dev | test1234 |
| test3 | test3@derbymetals.dev | test1234 |
| test4 | test4@derbymetals.dev | test1234 |

Estas 4 cuentas alcanzan exactamente para llenar una sala de Bomberman de 4 jugadores (ver spec de diseno) y probar `POST /matches` con una partida completa real. El seed debe re-aplicarse (o ser idempotente) cada vez que se reinicia el entorno de pruebas, y nunca debe existir en produccion.

Mientras el backend real no existe, el equivalente local es el servidor falso: `ApiSettings.asset` (`Assets/ScriptableObjects/Api/ApiSettings.asset`) trae `mode = Fake` por defecto, y el fake no trae usuarios precargados (los tests y el juego los crean via `POST /auth/register` segun necesiten, y con `persistFakeData = true` los datos se guardan entre sesiones en `Application.persistentDataPath/fake-api-db.json`). Cuando el backend real este disponible, el seed de `test1..test4` se documenta aqui para que cualquiera (persona o test automatizado) pueda loguearse contra staging sin coordinacion adicional.

## 10. Como se conecta el cliente Unity

El cliente nunca habla HTTP directo: pasa siempre por `Bomberman.Api` (`Assets/Scripts/Api/`), que arma una cadena de responsabilidad segun `ApiSettings`:

1. **`ApiSettings`** (`Assets/Scripts/Api/Config/ApiSettings.cs`), un `ScriptableObject` editable desde el Inspector (asset en `Assets/ScriptableObjects/Api/ApiSettings.asset`), define:
   - `mode`: `Fake` (servidor en memoria, usado en Editor y tests) o `Http` (backend real).
   - `baseUrl`: URL base incluyendo `/api/v1` (por ejemplo `https://localhost:5001/api/v1` en el asset actual).
   - `timeoutSeconds`: timeout por intento de request (10s por defecto).
   - `retryCount`: reintentos con backoff exponencial (300ms, 600ms, ...) ante error de red o `5xx` (2 por defecto).
2. **`ApiClientFactory.Create`** (`Assets/Scripts/Api/Client/ApiClientFactory.cs`) arma la cadena real: si `mode == Http`, crea `HttpApiClient` (usa `UnityWebRequest` + Newtonsoft.Json, ver `Assets/Scripts/Api/Client/HttpApiClient.cs`); si `mode == Fake`, crea `FakeApiClient` sobre `FakeDatabase` con las rutas de `FakeApiRoutes.Register`.
3. Ese transporte se envuelve siempre en **`AuthenticatingApiClient`** (`Assets/Scripts/Api/Client/AuthenticatingApiClient.cs`), que agrega el header `Authorization: Bearer <accessToken>` en los requests que lo requieren y, ante un `401`, intenta refrescar el token una vez (`POST /auth/refresh`) y reintenta la peticion original; si el refresh falla, limpia la sesion guardada (`ITokenStore`) para que la app vuelva a pantalla de login.
4. Los servicios de dominio (`Api/Services/AuthService.cs`, `ProfileService.cs`, `StatsService.cs`, `GameConfigService.cs`, `HealthService.cs`) son los que el resto del juego consume; envuelven al cliente y devuelven `ApiResult<T>` (nunca lanzan excepcion por errores HTTP, ver `Assets/Scripts/Api/Client/ApiResult.cs`).

Expectativa de tiempo de respuesta: el backend real debe responder en menos de 500 ms en condiciones normales (p95). El `timeoutSeconds` de 10s del cliente es un limite de tolerancia ante degradacion puntual, no el tiempo esperado; una latencia sostenida cercana al timeout se considera una incidencia del backend, no un comportamiento normal. El servidor falso simula esto localmente con latencia aleatoria configurable (`fakeMinLatencyMs`/`fakeMaxLatencyMs`, 80-250 ms por defecto en el asset actual) para que la UI del cliente nunca asuma respuesta instantanea.
