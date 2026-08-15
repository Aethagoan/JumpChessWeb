# Retrospectives

- Created a new **JC.GameLib** class library to hold the core chess logic.
- Copied the full implementation of the `Game` class, along with helper types such as `MoveDto` and `MoveSnapshot`.
- Updated the API service (`JC.API/Services/GameService.cs`) to reference the new library (`using JC.GameLib;`).
- Updated the project files to include the new library and removed the old shared game logic from `JC.Shared`.
- Updated the reference document (`AGENTS.md`) to reflect the new plan and to record the decisions made.
- Added unit tests for the new library.
- Moved shared types to library where needed.
- Updated target framework to **.NET 10.0**.
