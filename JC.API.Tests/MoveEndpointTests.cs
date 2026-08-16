using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class MoveEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public MoveEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync()
    {
        var loginDto = new { Username = "alice", Password = "pwd" };
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        loginResp.EnsureSuccessStatusCode();
        var tokenObj = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        return tokenObj.GetProperty("token").GetString();
    }

    [Fact]
    public async Task MoveGame_ValidMove_ReturnsOk()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createDto = new { WhitePlayer = (byte)1, BlackPlayer = (byte)2 };
        var createResp = await _client.PostAsJsonAsync("/api/games", createDto);
        createResp.EnsureSuccessStatusCode();
        var createJson = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = createJson.GetProperty("id").GetGuid();

        var moveDto = new { From = 52, To = 36, PromotionPiece = 0 };
        var moveResp = await _client.PostAsJsonAsync($"/api/games/{id}/move", moveDto);
        Assert.Equal(HttpStatusCode.OK, moveResp.StatusCode);
        var gameJson = await moveResp.Content.ReadFromJsonAsync<JsonElement>();

        var boardArray = gameJson.GetProperty("board");
        var board = boardArray.EnumerateArray().Select((x, i) => x.GetByte()).ToArray();
        Assert.Equal((byte)2, board[36]); // white pawn moved to e4
        Assert.Equal((byte)0, board[52]); // source empty
        Assert.Equal((byte)0, gameJson.GetProperty("gameStatus").GetByte()); // InProgress
    }
}
