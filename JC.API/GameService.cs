using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.Sqlite;
using JC.GameLib;
using System.IO;

public class GameService
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();
    private readonly SqliteConnection _conn;

    public GameService()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "data.db");
        var connStr = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        _conn = new SqliteConnection(connStr);
        _conn.Open();

        var createCmd = _conn.CreateCommand();
        createCmd.CommandText = "CREATE TABLE IF NOT EXISTS Games (Id TEXT PRIMARY KEY, Data TEXT NOT NULL)";
        createCmd.ExecuteNonQuery();

        var selectCmd = _conn.CreateCommand();
        selectCmd.CommandText = "SELECT Id, Data FROM Games";
        using var reader = selectCmd.ExecuteReader();
        while (reader.Read())
        {
            var idStr = reader.GetString(0);
            var json = reader.GetString(1);
            if (Guid.TryParse(idStr, out var id))
            {
                var game = System.Text.Json.JsonSerializer.Deserialize<Game>(json);
                if (game != null) _games[id] = game;
            }
        }
    }

    private void Persist(Guid id, Game game)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(game);
        var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Games (Id, Data) VALUES ($id, $data) ON CONFLICT(Id) DO UPDATE SET Data = $data";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$data", json);
        cmd.ExecuteNonQuery();
    }

    public Guid CreateGame(byte whitePlayer, byte blackPlayer, bool enableUndo, byte whiteVetoes, byte blackVetoes)
    {
        var id = Guid.NewGuid();
        var game = new Game(whitePlayer, blackPlayer, enableUndo, whiteVetoes, blackVetoes);
        _games[id] = game;
        Persist(id, game);
        return id;
    }

    public Game? GetGame(Guid id) => _games.TryGetValue(id, out var g) ? g : null;

    public void Save(Guid id, Game game) => Persist(id, game);

    public IEnumerable<Game> GetGames(byte? playerId)
    {
        if (playerId == null) return _games.Values;
        return _games.Values.Where(g => g.whitePlayerID == playerId.Value || g.blackPlayerID == playerId.Value);
    }
}
