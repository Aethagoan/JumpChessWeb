using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.IO;
using JC.GameLib;

public class SQLiteDataHandler
{
    private readonly SqliteConnection _conn;

    public SQLiteDataHandler(IConfiguration config)
    {
        var dbPath = config["DbPath"] ?? Path.Combine(AppContext.BaseDirectory, "data.db");
        var connStr = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        _conn = new SqliteConnection(connStr);
        _conn.Open();

        // ensure tables
        var createGames = _conn.CreateCommand();
        createGames.CommandText = "CREATE TABLE IF NOT EXISTS Games (Id TEXT PRIMARY KEY, Data TEXT NOT NULL)";
        createGames.ExecuteNonQuery();

        var createPlayers = _conn.CreateCommand();
        createPlayers.CommandText = "CREATE TABLE IF NOT EXISTS Players (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL)";
        createPlayers.ExecuteNonQuery();

        var createCreds = _conn.CreateCommand();
        createCreds.CommandText = "CREATE TABLE IF NOT EXISTS Credentials (Username TEXT PRIMARY KEY, Id INTEGER NOT NULL, Password TEXT NOT NULL)";
        createCreds.ExecuteNonQuery();

        // Seed players from configuration (appsettings.json)
        var players = config.GetSection("PlayerNames");
        foreach (var child in players.GetChildren())
        {
            if (!byte.TryParse(child.Key, out var id) || string.IsNullOrEmpty(child.Value)) continue;
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Players (Id, Name) VALUES ($id, $name) ON CONFLICT(Id) DO NOTHING";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$name", child.Value);
            cmd.ExecuteNonQuery();
        }
    }

    public IEnumerable<(Guid Id, Game Game)> LoadGames()
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Data FROM Games";
        using var reader = cmd.ExecuteReader();
        var list = new List<(Guid, Game)>();
        while (reader.Read())
        {
            var idStr = reader.GetString(0);
            var json = reader.GetString(1);
            if (Guid.TryParse(idStr, out var id))
            {
                var game = JsonSerializer.Deserialize<Game>(json);
                if (game != null) list.Add((id, game));
            }
        }
        return list;
    }

    public void PersistGame(Guid id, Game game)
    {
        var json = JsonSerializer.Serialize(game);
        var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Games (Id, Data) VALUES ($id, $data) ON CONFLICT(Id) DO UPDATE SET Data = $data";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$data", json);
        cmd.ExecuteNonQuery();
    }

    public string? GetPlayerName(byte id)
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Name FROM Players WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        var result = cmd.ExecuteScalar();
        return result as string;
    }

    public Dictionary<byte, string> GetAllPlayers()
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Players";
        using var reader = cmd.ExecuteReader();
        var dict = new Dictionary<byte, string>();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            dict[(byte)id] = name;
        }
        return dict;
    }

    public IReadOnlyDictionary<string, (byte Id, string Password)> GetCredentials()
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Username, Id, Password FROM Credentials";
        using var reader = cmd.ExecuteReader();
        var dict = new Dictionary<string, (byte, string)>();
        while (reader.Read())
        {
            var username = reader.GetString(0);
            var id = reader.GetInt32(1);
            var password = reader.GetString(2);
            dict[username] = ((byte)id, password);
        }
        return dict;
    }

    public void UpsertCredential(string username, byte id, string password)
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO Credentials (Username, Id, Password) VALUES ($username, $id, $password)";
        cmd.Parameters.AddWithValue("$username", username);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$password", password);
        cmd.ExecuteNonQuery();
    }
}
