# Next Sprint

- [ ] Create JC.GameLib with core game logic from JC.Core and JC.Shared.
- [ ] Implement GameService to use JC.GameLib.
- [ ] Add SQLite persistence (data context, migrations).
- [ ] Create API endpoints:
  * POST `/api/games/create` – create game.
  * GET `/api/games/{id}` – retrieve state.
  * GET `/api/games/{id}/status` – game status.
- [ ] Implement minimal authentication with AESAuth.
- [ ] Add unit tests for GameLib and API.
- [ ] Add Dockerfile and CI pipeline.
