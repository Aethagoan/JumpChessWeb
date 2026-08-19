using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

public record Credential(string Username, string Password, byte Id);

public interface ISettingsService
{
    IReadOnlyDictionary<string, (byte Id, string Password)> Credentials { get; }
}

public class SettingsService : ISettingsService
{
    private readonly Dictionary<string, (byte Id, string Password)> _credentials;

    public SettingsService(SQLiteDataHandler db, IConfiguration? config = null)
    {
        var creds = db.GetCredentials();
        _credentials = creds != null ? new(creds) : new();

        // Seed test credentials if configured and none exist
        if (_credentials.Count == 0 && config != null)
        {
            var section = config.GetSection("TestCredentials");
            var user = section["Username"];
            var pass = section["Password"];
            var idStr = section["Id"];
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass) && byte.TryParse(idStr, out var id))
            {
                _credentials[user] = (id, pass);
                db.UpsertCredential(user, id, pass);
            }
        }
    }

    public IReadOnlyDictionary<string, (byte Id, string Password)> Credentials => _credentials;
}
