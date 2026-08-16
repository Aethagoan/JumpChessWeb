# Retrospective

- Refactored persistence from file‑based JSON to SQLite for better durability and scalability.
- Updated Program.cs to manage a single SQLite connection and upsert records.
- Added Microsoft.Data.Sqlite package.
- Implemented AES‑based authentication endpoint.
- Added static front‑end with board rendering.
- No regressions observed in existing API behavior.
