# JumpChess Web API

## Overview

This project exposes a minimal REST API for managing JumpChess games.
It is implemented in ASP.NET Core 10.0.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| **POST** | `/api/auth/login` | Authenticate a user. Accepts `LoginDto` with `Username` and `Password`. Returns an encrypted token.
| **POST** | `/api/games` | Create a new game. Accepts `GameCreateDto` (`WhitePlayer`, `BlackPlayer`, optional `EnableUndo`, `WhiteVetoes`, `BlackVetoes`). Returns `{ id: Guid }`.
| **GET** | `/api/games/{id}` | Retrieve a full game state.
| **POST** | `/api/games/{id}/move` | Submit a move. Accepts `MoveRequest` (`From`, `To`, optional `PromotionPiece`). Returns the updated game.
| **POST** | `/api/games/{id}/undo` | Undo the last move. Requires query parameter `playerId`.
| **POST** | `/api/games/{id}/veto` | Veto the last move. Requires query parameter `playerId`.
| **GET** | `/api/games/{id}/status` | Get the current game status enum.

All responses are JSON.

## Data Model

- **Game** – contains the board state and game metadata. The board is a 64‑element array of bytes.
- **GameCreateDto** – input for creating a game.
- **MoveRequest** – input for a move.
- **LoginDto** – credentials for login.

## Running

```bash
dotnet run --project JC.API
```

The API runs on `http://localhost:5000` by default.

## Front‑end

A very small static front‑end lives in the `frontend/` directory.

---

See the `docs/` folder for more details.