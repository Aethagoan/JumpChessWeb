using System.Collections.Concurrent;
using System.Linq;
using JC.GameLib;

public class GameService
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();
    private readonly SQLiteDataHandler _db;

    public GameService(SQLiteDataHandler db)
    {
        _db = db;
        foreach (var (id, game) in _db.LoadGames())
        {
            _games[id] = game;
        }
    }

    private void Persist(Guid id, Game game)
    {
        _db.PersistGame(id, game);
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

    public IEnumerable<(Guid Id, Game Game)> GetGames(byte? playerId)
    {
        var games = _games.Select(entry => (entry.Key, entry.Value));
        if (playerId == null) return games;
        return games.Where(entry => entry.Value.whitePlayerID == playerId.Value || entry.Value.blackPlayerID == playerId.Value);
    }
}
