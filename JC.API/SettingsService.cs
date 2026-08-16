using System;
using System.Collections.Generic;
using System.Linq;

public record Credential(string Username, string Password, byte Id);

public interface ISettingsService
{
    IReadOnlyDictionary<string, (byte Id, string Password)> Credentials { get; }
}

public class SettingsService : ISettingsService
{
    private readonly Dictionary<string, (byte Id, string Password)> _credentials;

    public SettingsService(SQLiteDataHandler db)
    {
        var creds = db.GetCredentials();
        _credentials = creds ?? new();
    }

    public IReadOnlyDictionary<string, (byte Id, string Password)> Credentials => _credentials;
}
