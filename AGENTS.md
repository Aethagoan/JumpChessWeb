# Agent Plan

## Goal
Create a new, simplified version of the JumpChess project that replaces the native Android/iOS apps with a single, lightweight web front‑end served by a minimal back‑end. The back‑end will be written in C# (ASP.NET Core) and will expose core game logic and a REST API.

## Scope
- Port or rewrite core game logic from the legacy repository in `../jumpchess`.
- Expose a RESTful API for game state and move validation.
- Replace the mobile UI with a responsive HTML/CSS/JS interface that provides the same features: current games being played, settings page, game setup page, board page with move selection and chat.
- Serve the web files (html) from a separate lightweight web host; the API remains a standalone service.
- Keep the build pipeline simple: `dotnet publish`.
- Avoid introducing heavy dependencies; use standard libraries and minimal third‑party packages.

## Assumptions
- The legacy code in `../jumpchess` contains all the essential game logic and can be reused or lightly refactored.
- Authentication will use an encrypted exchange of credentials and server IP; JWT/session cookies can be added later.
- Deployment will occur on a standard Linux VM or container.
- We aim for a single‑page style application (loads different html files); no server‑rendered templates beyond static assets.
- Legacy code may be tightly coupled to Android/iOS specifics.
- Front‑end may become feature‑heavy if not carefully scoped.
- Deployment simplicity may conflict with scalability.

## Decisions
- Prefer C# for the back‑end to stay consistent with current codebase.
- Serve static assets from a separate lightweight web host; the API remains standalone.
- Use plain JS (no framework) for maximum simplicity.
- All directories needed are created. Do not create any more directories, all the directories needed are already present.

## Architecture Overview
- Back‑end: ASP.NET Core Web API exposing REST endpoints; compiled into a self‑contained DLL.
- Game logic: isolated .NET Standard library, unit‑tested with xUnit.
- Persistence: SQLite.
- Auth: simple symmetric encryption (AES) for credential exchange;
- Front‑end: single HTML page served from an independent static file host (e.g., Nginx or a tiny file server). UI renders an 8×8 grid and uses vanilla JS to send/receive moves.
- Build: `dotnet publish -c Release` creates a publish folder;
- Dotnet version is net10.0 no exceptions.
- The Backend API exists in `./JC.API`. Its corresponding tests exist in `./JC.API.Tests`. All changes to the API or API tests are done through these directories.
- The Game Library exists in `./JC.GameLib`. Its corresponding tests exist in `./JC.GameLib.Tests`. All changes to the Game Library should be done through these directories.
- The Web UI / frontend exists in `./JC.Web`. Corresponding tests should exist in `./JC.Web.Tests`. All changes to the Web Frontend should be done throgh those directories.