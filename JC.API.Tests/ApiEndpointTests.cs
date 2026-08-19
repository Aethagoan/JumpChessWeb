using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

public class ApiEndpointTests : IClassFixture<TestHost>
{
    private readonly HttpClient _client;
    public ApiEndpointTests(TestHost factory)
    {
        _client = factory.CreateClient();
    }

    private static async Task<HttpClient> AuthedClientAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { Username = "alice", Password = "alicepwd" });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
        return client;
    }

    [Fact]
    public async Task Login_ReturnsToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { Username = "alice", Password = "alicepwd" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task CreateGame_ReturnsCreated()
    {
        var client = await AuthedClientAsync(HttpClientFactory.Create());
        var response = await client.PostAsJsonAsync("/api/games", new { WhitePlayer = (byte)1, BlackPlayer = (byte)2 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task CreateGame_Unauthorized_WhenNoToken()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { WhitePlayer = (byte)1, BlackPlayer = (byte)2 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_NotFound()
    {
        var client = await AuthedClientAsync(HttpClientFactory.Create());
        var response = await client.GetAsync("/api/games/00000000-0000-0000-0000-000000000000");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_ReturnsCreatedGame()
    {
        var client = await AuthedClientAsync(HttpClientFactory.Create());
        var createResp = await client.PostAsJsonAsync("/api/games", new { WhitePlayer = (byte)1, BlackPlayer = (byte)2 });
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var response = await client.GetAsync($"/api/games/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(64, json.GetProperty("board").GetArrayLength());
    }

    [Fact]
    public async Task GetPlayerName_ReturnsName()
    {
        var response = await _client.GetAsync("/api/players/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("id").GetByte());
        Assert.Equal("Alice", json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetPlayerName_NotFound()
    {
        var response = await _client.GetAsync("/api/players/99");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class HttpClientFactory
{
    private static readonly Lazy<TestHost> _factory = new(() => new TestHost());
    public static HttpClient Create() => _factory.Value.CreateClient();
}
