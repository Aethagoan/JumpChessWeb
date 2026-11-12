using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using JC.GameLib;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SQLiteDataHandler>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<IPlayerService, PlayerService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.IncludeFields = true;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors("WebClient");

var settings = app.Services.GetRequiredService<ISettingsService>();

app.MapPost("/api/auth/login", (LoginDto dto) =>
{
    var username = string.Empty;
    if (dto.PlayerId.HasValue)
    {
        var match = settings.Credentials.FirstOrDefault(entry =>
            entry.Value.Id == dto.PlayerId.Value && entry.Value.Password == dto.Password);
        if (string.IsNullOrEmpty(match.Key)) return Results.Unauthorized();
        username = match.Key;
    }
    else if (!string.IsNullOrEmpty(dto.Username) &&
             settings.Credentials.TryGetValue(dto.Username, out var credential) &&
             credential.Password == dto.Password)
    {
        username = dto.Username;
    }
    else
    {
        return Results.Unauthorized();
    }

    var token = AESAuth.EncryptToken(username, dto.Password);
    return Results.Ok(new LoginResponse(token));
});

byte? GetPlayerId(HttpContext ctx)
{
    if (!ctx.Request.Headers.TryGetValue("Authorization", out var authHeaders)) return null;
    var header = authHeaders.FirstOrDefault();
    if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ")) return null;
    var token = header[7..];
    var creds = AESAuth.DecryptToken(token);
    if (creds == null) return null;
    if (!settings.Credentials.TryGetValue(creds.Value.username, out var entry)) return null;
    if (entry.Password != creds.Value.password) return null;
    return entry.Id;
}

app.MapPost("/api/games", (HttpContext ctx, GameCreateDto dto, GameService gameService) =>
{
    var playerId = GetPlayerId(ctx);
    if (playerId == null) return Results.Unauthorized();
    var id = gameService.CreateGame(dto.WhitePlayer, dto.BlackPlayer, dto.EnableUndo, dto.WhiteVetoes, dto.BlackVetoes);
    return Results.Created($"/api/games/{id}", new GameCreatedResponse(id));
});

app.MapGet("/api/games", (HttpContext ctx, GameService gameService) =>
{
    var playerId = GetPlayerId(ctx);
    if (playerId == null) return Results.Unauthorized();
    var games = gameService.GetGames(playerId).Select(entry =>
        new GameSummaryResponse(
            entry.Id,
            entry.Game.whitePlayerID,
            entry.Game.blackPlayerID,
            entry.Game.whiteToMove,
            entry.Game.gameStatus,
            entry.Game.allowUndo,
            entry.Game.whiteVetoesPerTurn,
            entry.Game.blackVetoesPerTurn));
    return Results.Ok(games);
});

app.MapGet("/api/games/{id}", (HttpContext ctx, Guid id, GameService gameService) =>
{
    var playerId = GetPlayerId(ctx);
    if (playerId == null) return Results.Unauthorized();
    var game = gameService.GetGame(id);
    return game != null ? Results.Ok(game) : Results.NotFound();
});

app.MapPost("/api/games/{id}/move", (HttpContext ctx, Guid id, MoveRequest dto, GameService gameService) =>
{
    var playerId = GetPlayerId(ctx);
    if (playerId == null) return Results.Unauthorized();
    var game = gameService.GetGame(id);
    if (game == null) return Results.NotFound();
    if (!game.Move((byte)dto.From, (byte)dto.To, (byte)dto.PromotionPiece))
        return Results.BadRequest(new MoveResult(false, "Invalid move"));
    gameService.Save(id, game);
    return Results.Ok(game);
});

app.MapPost("/api/games/{id}/undo", (HttpContext ctx, Guid id, byte playerId, GameService gameService) =>
{
    var authId = GetPlayerId(ctx);
    if (authId == null) return Results.Unauthorized();
    var game = gameService.GetGame(id);
    if (game == null) return Results.NotFound();
    if (game.Undo(playerId))
    {
        gameService.Save(id, game);
        return Results.Ok(game);
    }
    return Results.BadRequest(new MoveResult(false, "Undo not allowed"));
});

app.MapPost("/api/games/{id}/veto", (HttpContext ctx, Guid id, byte playerId, GameService gameService) =>
{
    var authId = GetPlayerId(ctx);
    if (authId == null) return Results.Unauthorized();
    var game = gameService.GetGame(id);
    if (game == null) return Results.NotFound();
    if (game.Veto(playerId))
    {
        gameService.Save(id, game);
        return Results.Ok(game);
    }
    return Results.BadRequest(new MoveResult(false, "Veto not allowed"));
});

app.MapGet("/api/games/{id}/status", (HttpContext ctx, Guid id, GameService gameService) =>
{
    var authId = GetPlayerId(ctx);
    if (authId == null) return Results.Unauthorized();
    var game = gameService.GetGame(id);
    return game != null ? Results.Ok(game.GetGameStatus()) : Results.NotFound();
});

app.MapGet("/api/players/{id}", (byte id, IPlayerService ps) =>
{
    var name = ps.GetPlayerName(id);
    return name != null ? Results.Ok(new PlayerResponse(id, name)) : Results.NotFound();
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

record LoginDto(string? Username, string Password, byte? PlayerId = null);
record GameCreateDto(byte WhitePlayer, byte BlackPlayer, bool EnableUndo = false, byte WhiteVetoes = 0, byte BlackVetoes = 0);
record MoveRequest(int From, int To, int PromotionPiece = 0);
