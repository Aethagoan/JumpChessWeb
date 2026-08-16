using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using JC.GameLib;
using System.IO;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ISettingsService, SettingsService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var games = new ConcurrentDictionary<Guid, Game>();

// SQLite persistence
var dbPath = Path.Combine(AppContext.BaseDirectory, "data.db");
var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
var conn = new SqliteConnection(connectionString);
conn.Open();
// Ensure table exists
var createCmd = conn.CreateCommand();
createCmd.CommandText = "CREATE TABLE IF NOT EXISTS Games (Id TEXT PRIMARY KEY, Data TEXT NOT NULL)";
createCmd.ExecuteNonQuery();
// Load persisted games
var selectCmd = conn.CreateCommand();
selectCmd.CommandText = "SELECT Id, Data FROM Games";
using var reader = selectCmd.ExecuteReader();
while (reader.Read())
{
    var idStr = reader.GetString(0);
    var dataJson = reader.GetString(1);
    if (Guid.TryParse(idStr, out var id))
    {
        var game = System.Text.Json.JsonSerializer.Deserialize<Game>(dataJson);
        if (game != null)
            games[id] = game;
    }
}

void Persist(Guid id, Game game)
{
    var json = System.Text.Json.JsonSerializer.Serialize(game);
    var upsertCmd = conn.CreateCommand();
    upsertCmd.CommandText = "INSERT INTO Games (Id, Data) VALUES ($id, $data) ON CONFLICT(Id) DO UPDATE SET Data = $data";
    upsertCmd.Parameters.AddWithValue("$id", id.ToString());
    upsertCmd.Parameters.AddWithValue("$data", json);
    upsertCmd.ExecuteNonQuery();
}

app.MapPost("/api/auth/login", (LoginDto dto) =>
{
    // Very simple auth: accept any username/password and return an encrypted token
    var token = AESAuth.EncryptToken(dto.Username, dto.Password);
    return Results.Ok(new { token });
});

var settings = app.Services.GetRequiredService<ISettingsService>();

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

app.MapPost("/api/games", (HttpContext ctx, GameCreateDto dto) =>
{
    var playerId = GetPlayerId(ctx);
    if (playerId == null) return Results.Unauthorized();
    var id = Guid.NewGuid();
    var game = new Game(dto.WhitePlayer, dto.BlackPlayer, dto.EnableUndo, dto.WhiteVetoes, dto.BlackVetoes);
    games[id] = game;
    Persist(id, game);
    return Results.Created($"/api/games/{id}", new { id });
});

app.MapGet("/api/games/{id}", (HttpContext ctx, Guid id) =>
{
    var playerId = GetPlayerId(ctx);
    if (playerId == null) return Results.Unauthorized();
    return games.TryGetValue(id, out var game) ? Results.Ok(game) : Results.NotFound();
});

app.MapPost("/api/games/{id}/move", (HttpContext ctx, Guid id, MoveRequest dto) =>
{
    var playerId = GetPlayerId(ctx);
    if (playerId == null) return Results.Unauthorized();
    if (!games.TryGetValue(id, out var game))
        return Results.NotFound();
    if (!game.Move((byte)dto.From, (byte)dto.To, (byte)dto.PromotionPiece))
        return Results.BadRequest("Invalid move");
    Persist(id, game);
    return Results.Ok(game);
});

app.MapPost("/api/games/{id}/undo", (HttpContext ctx, Guid id, byte playerId) =>
{
    var authId = GetPlayerId(ctx);
    if (authId == null) return Results.Unauthorized();
    if (!games.TryGetValue(id, out var game))
        return Results.NotFound();
    if (game.Undo(playerId))
    {
        Persist(id, game);
        return Results.Ok(game);
    }
    return Results.BadRequest("Undo not allowed");
});

app.MapPost("/api/games/{id}/veto", (HttpContext ctx, Guid id, byte playerId) =>
{
    var authId = GetPlayerId(ctx);
    if (authId == null) return Results.Unauthorized();
    if (!games.TryGetValue(id, out var game))
        return Results.NotFound();
    if (game.Veto(playerId))
    {
        Persist(id, game);
        return Results.Ok(game);
    }
    return Results.BadRequest("Veto not allowed");
});

app.MapGet("/api/games/{id}/status", (HttpContext ctx, Guid id) =>
{
    var authId = GetPlayerId(ctx);
    if (authId == null) return Results.Unauthorized();
    return games.TryGetValue(id, out var game) ? Results.Ok(game.GetGameStatus()) : Results.NotFound();
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

record LoginDto(string Username, string Password);
record GameCreateDto(byte WhitePlayer, byte BlackPlayer, bool EnableUndo = false, byte WhiteVetoes = 0, byte BlackVetoes = 0);
record MoveRequest(int From, int To, int PromotionPiece = 0);
