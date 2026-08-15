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
    var game = new Game(0, 0); // default board, no players yet
    games[id] = game;
    return Results.Created($"/api/games/{id}", new { id });
});

app.MapGet("/api/games/{id}", (Guid id) =>
{
    if (games.TryGetValue(id, out var game))
        return Results.Ok(game);
    return Results.NotFound();
});

app.MapPost("/api/games/{id}/move", (Guid id, MoveDto dto) =>
{
    if (!games.TryGetValue(id, out var game)) return Results.NotFound();
    if (!game.Move((byte)dto.From, (byte)dto.To)) return Results.BadRequest("Invalid move");
    return Results.Ok(game);
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

record GameCreateDto(string[] Players);
record MoveDto(int From, int To);
