using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public interface IPlayerService
{
    string? GetPlayerName(byte id);
}

public class PlayerService : IPlayerService
{
    private readonly Dictionary<byte, string> _players;

    public PlayerService(IConfiguration config)
    {
        var section = config.GetSection("PlayerNames");
        var dict = new Dictionary<byte, string>();
        foreach (var child in section.GetChildren())
        {
            if (byte.TryParse(child.Key, out var id))
                dict[id] = child.Value;
        }
        _players = dict;
    }

    public string? GetPlayerName(byte id) => _players.TryGetValue(id, out var name) ? name : null;
}
