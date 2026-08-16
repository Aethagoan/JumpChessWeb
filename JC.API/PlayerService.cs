using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public interface IPlayerService
{
    string? GetPlayerName(byte id);
}

public class PlayerService : IPlayerService
{
    private readonly Dictionary<byte, string> _players;

    public PlayerService(SQLiteDataHandler db)
    {
        _players = db.GetAllPlayers();
    }

    public string? GetPlayerName(byte id) => _players.TryGetValue(id, out var name) ? name : null;
}
