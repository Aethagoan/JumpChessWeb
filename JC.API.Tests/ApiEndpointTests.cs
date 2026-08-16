using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public ApiEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGame_ReturnsCreated()
    {
        var dto = new { WhitePlayer = (byte)1, BlackPlayer = (byte)2 };
        var response = await _client.PostAsJsonAsync("/api/games", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task GetGame_NotFound()
    {
        var response = await _client.GetAsync("/api/games/00000000-0000-0000-0000-000000000000");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
