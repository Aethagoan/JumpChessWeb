# Retrospective – Core Logic Extraction

## What was done
- Created a dedicated **JC.GameLib** class library to host the core chess logic.
- Copied the entire `Game` implementation, along with supporting types (`MoveDto`, `MoveSnapshot`, `GameStatus`).
- Updated the API service to reference the new library.
- Removed the duplicate logic from `JC.Shared`.
- Updated the plan document to reflect the new architecture.

## Lessons learned
- Copy‑paste large logic blocks demands careful verification of all dependencies and field names.
- Keeping the game logic in a single, isolated library simplifies future maintenance and testing.
- Reflection usage for state reconstruction can be fragile if the underlying fields change.

## Risks
- Renaming any field in `Game` or `MoveSnapshot` will break the Redis deserialization logic.
- Adding new features may require additional migrations.

## Next Steps
- Add comprehensive unit tests for the library (xUnit).
- Update the target framework to **.NET 10.0**.
- Re‑build and redeploy the API and services.
