# Agent Plan

## Goal
Create a new, simplified version of the JumpChess project that replaces the native Android/iOS apps with a single, lightweight web front‑end served by a minimal back‑end. The back‑end will be written in C# (ASP.NET Core) and will expose core game logic and a REST API.

## Scope
- Port or rewrite core game logic from the legacy repository in `../jumpchess`.
- Expose a RESTful API for game state, move validation, and matchmaking.
- Replace the mobile UI with a responsive HTML/CSS/JS interface that provides the same features: board view, move selection, chat, leaderboard, and settings.
- Serve the web assets from a separate lightweight web host; the API remains a standalone service.
- Keep the build pipeline simple: `dotnet publish` for C# or `python -m uvicorn` for Python.
- Avoid introducing heavy dependencies; use standard libraries and minimal third‑party packages.

## Assumptions

## API Endpoints
- POST `/api/games` – Create a new game. Payload: `GameCreateDto` (player IDs, undo flag, veto counts). Returns game ID.
- GET `/api/games/{id}` – Retrieve full game state.
- POST `/api/games/{id}/move` – Submit a move. Payload: `MoveRequest` (from, to, optional promotion piece). Returns updated game.
- POST `/api/games/{id}/undo` – Undo the last move. Requires query `playerId`. Returns updated game or BadRequest.
- POST `/api/games/{id}/veto` – Veto the last move. Requires query `playerId`. Returns updated game or BadRequest.
- GET `/api/games/{id}/status` – Get current `GameStatus` enum value.

- The legacy code in `../jumpchess` contains all the essential game logic and can be reused or lightly refactored.
- Users will access the new version via modern browsers (Chrome, Firefox, Safari).
- Authentication will use an encrypted exchange of credentials and server IP; JWT/session cookies can be added later.
- Deployment will occur on a standard Linux VM or container.
- We aim for a single‑page application; no server‑rendered templates beyond static assets.
- Legacy code may be tightly coupled to Android/iOS specifics.
- Front‑end may become feature‑heavy if not carefully scoped.
- Deployment simplicity may conflict with scalability.

## Decisions
- Prefer C# for the back‑end to stay consistent with current codebase.
- Serve static assets from a separate lightweight web host; the API remains standalone.
- Use plain JS (no framework) for maximum simplicity.

## Architecture Overview
- Back‑end: ASP.NET Core Web API exposing REST endpoints; compiled into a self‑contained DLL.
- Game logic: isolated .NET Standard library, unit‑tested with xUnit.
- Persistence: 
- Auth: simple symmetric encryption (AES) for credential exchange; JWT added later.
- Front‑end: single HTML page served from an independent static file host (e.g., Nginx or a tiny file server). UI renders an 8×8 grid and uses vanilla JS to send/receive moves.
- Build: `dotnet publish -c Release` creates a publish folder; Dockerfile can wrap it later.
- Dotnet version is net10.0 no exceptions.