# API-CONTRACT v1 - DerbyMetals

Base URL: `https://<host>/api/v1`. Todo en JSON UTF-8. Fechas en ISO 8601 UTC (`2026-08-15T12:34:56Z`). Autenticacion: `Authorization: Bearer <accessToken>` (JWT).

Formato de error uniforme (cualquier codigo 4xx/5xx):
{ "error": { "code": "AUTH_INVALID_CREDENTIALS", "message": "Invalid credentials" } }

## Auth
### POST /auth/register (sin auth)
Request: { "email": "a@b.com", "username": "alvaro", "password": "secret12" }
201: { "accessToken": "jwt", "refreshToken": "opaque", "expiresIn": 3600, "user": { "userId": "uuid", "username": "alvaro", "email": "a@b.com", "createdAt": "iso" } }
Errores: 400 VALIDATION_FAILED, 409 AUTH_USER_ALREADY_EXISTS, 429 RATE_LIMITED

### POST /auth/login (sin auth)
Request: { "emailOrUsername": "alvaro", "password": "secret12" }
200: igual que register.
Errores: 400 VALIDATION_FAILED, 401 AUTH_INVALID_CREDENTIALS, 429 RATE_LIMITED

### POST /auth/refresh (sin auth)
Request: { "refreshToken": "opaque" }
200: igual que login (refresh token rotado; el anterior queda invalido).
Errores: 401 AUTH_TOKEN_EXPIRED

### POST /auth/logout (auth)
Request: { "refreshToken": "opaque" }
204 sin cuerpo. Idempotente.

## Profile
### GET /profile/me (auth)
200: { "userId": "uuid", "username": "alvaro", "displayName": "Alvaro", "selectedSkinId": "default", "unlockedSkinIds": ["default","skin_01"], "level": 3, "xp": 1250, "xpToNextLevel": 250 }

### PATCH /profile/me (auth)
Request (campos opcionales): { "displayName": "Nuevo", "selectedSkinId": "skin_01" }
200: perfil actualizado (misma forma que GET).
Errores: 400 VALIDATION_FAILED (nombre 3-16 chars; skin no desbloqueada)

### GET /profile/me/settings (auth)
200: { "soundEnabled": true, "sfxVolume": 0.8, "graphicsQuality": "high", "targetFps": 60, "swipeThresholdCm": 0.6 }

### PUT /profile/me/settings (auth)
Request (reemplazo completo, todos los campos obligatorios): { "soundEnabled": true, "sfxVolume": 0.8, "graphicsQuality": "high", "targetFps": 60, "swipeThresholdCm": 0.6 }
200: ajustes actualizados (misma forma que GET).
Errores: 400 VALIDATION_FAILED (sfxVolume fuera de [0,1]; swipeThresholdCm fuera de [0.4,1.0]; targetFps distinto de 30/60; graphicsQuality distinto de "low"/"high")

### PUT /profile/me/password (auth)
Request: { "currentPassword": "secret12", "newPassword": "newsecret1" }
204 sin cuerpo. Revoca el refresh token guardado del usuario; el access token con el que se hizo esta peticion sigue valido hasta que expire por su cuenta.
Errores: 400 VALIDATION_FAILED (currentPassword o newPassword faltantes; newPassword de menos de 8 caracteres), 401 AUTH_INVALID_CREDENTIALS (currentPassword incorrecta)

### PUT /profile/me/email (auth)
Request: { "currentPassword": "secret12", "newEmail": "nuevo@b.com" }
200: { "email": "nuevo@b.com" }
Errores: 400 VALIDATION_FAILED (currentPassword o newEmail faltantes; newEmail sin formato valido), 401 AUTH_INVALID_CREDENTIALS (currentPassword incorrecta), 409 AUTH_USER_ALREADY_EXISTS (email ya registrado por otro usuario)

## Stats y partidas
### GET /stats/me (auth)
200: { "matchesPlayed": 10, "wins": 4, "losses": 5, "draws": 1, "kills": 12, "deaths": 8, "powerUpsCollected": 40, "winRate": 0.4 }

### POST /matches (auth; solo lo envia el Master Client)
Request: { "matchId": "uuid", "mapId": "junkyard01", "startedAt": "iso", "endedAt": "iso", "endReason": "lastAlive|timeout|allDisconnected", "players": [ { "userId": "uuid", "placement": 1, "kills": 2, "deaths": 0, "powerUpsCollected": 5, "disconnected": false } ] }
201 primera vez / 200 si `matchId` ya existia (idempotente): {}
Errores: 400 VALIDATION_FAILED, 403 AUTH_FORBIDDEN (el remitente no esta entre players)

### GET /matches/me?limit=20 (auth)
200: { "items": [ { "matchId": "uuid", "mapId": "junkyard01", "endedAt": "iso", "myPlacement": 1, "players": [ ... ] } ], "total": 1 }

## Config y salud
### GET /game-config (sin auth)
200: { "enabledMapIds": null | ["junkyard01"], "skins": [ { "id": "default", "enabledInSelector": true } ], "balance": { "fuseSeconds": 2.5, "baseSpeed": 4, "baseRange": 1, "baseMaxBombs": 1, "matchDurationSeconds": 180, "wallCooldownSeconds": 8, "wallDurationSeconds": 4, "bombRequestTimeoutSeconds": 2, "timedFuseSeconds": 5, "remoteMaxSeconds": 15, "throwDistanceCells": 3, "throwDurationSeconds": 0.5, "maxRange": 8, "maxBombs": 6, "maxSpeedLevel": 4 }, "minClientVersion": "0.1.0", "configVersion": 1 }

### GET /health (sin auth)
200: { "status": "ok", "version": "1.0.0", "serverTime": "iso" }

## Catalogo de error.code
AUTH_INVALID_CREDENTIALS, AUTH_USER_ALREADY_EXISTS, AUTH_TOKEN_EXPIRED, AUTH_UNAUTHORIZED, AUTH_FORBIDDEN, VALIDATION_FAILED, NOT_FOUND, CONFLICT, RATE_LIMITED, SERVER_ERROR
