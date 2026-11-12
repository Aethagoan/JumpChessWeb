using System;

public record LoginResponse(string Token);
public record GameCreatedResponse(Guid Id);
public record PlayerResponse(byte Id, string Name);
public record MoveResult(bool Success, string? Message = null);
public record GameSummaryResponse(
    Guid Id,
    byte WhitePlayerID,
    byte BlackPlayerID,
    bool WhiteToMove,
    JC.GameLib.GameStatus GameStatus,
    bool AllowUndo,
    byte WhiteVetoesPerTurn,
    byte BlackVetoesPerTurn);
