using System;

public record LoginResponse(string Token);
public record GameCreatedResponse(Guid Id);
public record PlayerResponse(byte Id, string Name);
public record MoveResult(bool Success, string? Message = null);
