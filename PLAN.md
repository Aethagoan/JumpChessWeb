# Agent Plan

## Goal
Create a simplified JumpChess web service: port core game logic, expose a REST API, and provide a lightweight static front-end.

## Scope
* Re‑use core game logic from legacy JC.Core/JC.Shared.
* Implement API endpoints: create, read, move, undo, veto, status.
* Persist games in SQLite.
* Add minimal authentication.
* Serve static files from a separate lightweight host.

## Assumptions
* Legacy logic is functional; only API glue is missing.
* Front‑end is a single static site.

## Backlog
* Port core game logic to JC.GameLib.
* Wire up GameService with ported logic.
* Implement all API endpoints.
* Add persistence layer.
* Add authentication.
* Write unit tests.
* Prepare Dockerfile for deployment.

## Current Sprint
See SPRINT.md.

## Completed
None.

## In Progress
None.

## Blocked
None.

## Risks
* API contract may change if legacy logic differs.
* SQLite integration may be tricky.

## Decisions
* Use ASP.NET Core minimal APIs.

## Next Steps
Complete backlog items.

## Retrospective
None yet.
