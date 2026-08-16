using System.Collections.Concurrent;
using JC.GameLib;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var games = new ConcurrentDictionary<Guid, Game>();

app.MapPost("/api/games", (GameCreateDto dto) =>
{
    var id = Guid.NewGuid();
    var game = new Game(dto.WhitePlayer, dto.BlackPlayer, dto.EnableUndo, dto.WhiteVetoes, dto.BlackVetoes); // default board, specified players
    games[id] = game;
    return Results.Created($"/api/games/{id}", new { id });
});

app.MapGet("/api/games/{id}", (Guid id) =>
{
    if (games.TryGetValue(id, out var game))
        return Results.Ok(game);
    return Results.NotFound();
});

app.MapPost("/api/games/{id}/move", (Guid id, MoveRequest dto) =>
{
    if (!games.TryGetValue(id, out var game)) return Results.NotFound();
    if (!game.Move((byte)dto.From, (byte)dto.To, (byte)dto.PromotionPiece)) return Results.BadRequest("Invalid move");
    return Results.Ok(game);
});

app.UseSwagger();

// Additional game action endpoints
app.MapPost("/api/games/{id}/undo", (Guid id, byte playerId) =>
{
    if (!games.TryGetValue(id, out var game)) return Results.NotFound();
    bool ok = game.Undo(playerId);
    return ok ? Results.Ok(game) : Results.BadRequest("Undo not allowed");
});

app.MapPost("/api/games/{id}/veto", (Guid id, byte playerId) =>
{
    if (!games.TryGetValue(id, out var game)) return Results.NotFound();
    bool ok = game.Veto(playerId);
    return ok ? Results.Ok(game) : Results.BadRequest("Veto not allowed");
});

app.MapGet("/api/games/{id}/status", (Guid id) =>
{
    if (!games.TryGetValue(id, out var game)) return Results.NotFound();
    return Results.Ok(game.GetGameStatus());
});
app.UseSwaggerUI();

app.Run();

record GameCreateDto(byte WhitePlayer, byte BlackPlayer, bool EnableUndo = false, byte WhiteVetoes = 0, byte BlackVetoes = 0);
record MoveRequest(int From, int To, int PromotionPiece = 0);
