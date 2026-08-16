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
- The legacy code in `../jumpchess` contains all the essential game logic and can be reused or lightly refactored.
- Users will access the new version via modern browsers (Chrome, Firefox, Safari).
- Authentication will use an encrypted exchange of credentials and server IP; JWT/session cookies can be added later.
- Deployment will occur on a standard Linux VM or container.
- We aim for a single‑page application; no server‑rendered templates beyond static assets.

## Backlog
- [ ] Extract core game logic and package as a reusable library.
- [ ] Define API contract (routes, payloads).
- [ ] Implement back‑end in C# (ASP.NET Core Web API).
- [ ] Build a simple front‑end: HTML page, CSS for board layout, JS for interaction.
- [ ] Authentication integration.
- [ ] Data persistence using NoSQL (e.g., MongoDB, LiteDB) and migration scripts.
- [ ] CI/CD pipeline with minimal steps.
- [ ] User testing and bug triage.

## Current Sprint
- Port game logic to a C# class library.
- Draft API routes and data contracts.
- Prototype minimal HTML/CSS layout.

## Completed
- [x] Implement back‑end in C# (ASP.NET Core Web API).
- [x] Add undo, veto, status API endpoints

## In Progress
- Game logic extraction.
- API design sketch.

## Blocked
- Decision on authentication method.
- Decision on database choice.

## Risks
- Legacy code may be tightly coupled to Android/iOS specifics.
- Front‑end may become feature‑heavy if not carefully scoped.
- Deployment simplicity may conflict with scalability.

## Decisions
- Prefer C# for the back‑end to stay consistent with current codebase.
- Serve static assets from a separate lightweight web host; the API remains standalone.
- Use plain JS (no framework) for maximum simplicity.

## Next Steps
- Add unit tests for the new library (xUnit).  
- Migrate any other shared types to the library if they become needed.
- Update the target framework to **.NET 10.0** across all projects (already done for API and GameLib).
- Implement authentication (AES encryption, later JWT).
- Set up persistence layer with LiteDB or MongoDB.
- Build and deploy a minimal static front‑end.

## Retrospective
- Created a new **JC.GameLib** class library to hold the core chess logic.
- Copied the full implementation of the `Game` class, along with helper types such as `MoveDto` and `MoveSnapshot`.
- Updated the API service (`JC.API/Services/GameService.cs`) to reference the new library (`using JC.GameLib;`).
- Updated the project files to include the new library and removed the old shared game logic from `JC.Shared`.
- Updated the reference document (`AGENTS.md`) to reflect the new plan and to record the decisions made.
- Added unit tests for the new library.
- Moved shared types to library where needed.
- Updated target framework to **.NET 10.0**.
