using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

public class TestHost : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Private, throwaway DB per test run: keeps tests isolated from dev data.
        var dbPath = Path.Combine(Path.GetTempPath(), $"jc-api-tests-{Guid.NewGuid():N}.db");
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // appsettings.json is copied to the test bin (API csproj is Microsoft.NET.Sdk.Web,
            // which includes appsettings.json as Content with CopyToOutputDirectory), but only when
            // this project is the entry assembly. Referenced-project appsettings does not get copied,
            // so point at it explicitly.
            config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DbPath"] = dbPath,
            });
        });
    }
}
