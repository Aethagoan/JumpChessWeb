using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using JC.GameLib;
using Xunit;

public class MoveEndpointTests : IClassFixture<TestHost>
{
    private readonly HttpClient _client;
    public MoveEndpointTests(TestHost factory)
    {
        _client = factory.CreateClient();
        Auth(_client).GetAwaiter().GetResult();
    }

    internal static async Task Auth(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { Username = "alice", Password = "alicepwd" });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
    }

    private async Task<string> CreateGameAsync(byte whiteVetoes = 0, byte blackVetoes = 0, bool enableUndo = false)
    {
        var dto = new { WhitePlayer = (byte)1, BlackPlayer = (byte)2, EnableUndo = enableUndo, WhiteVetoes = whiteVetoes, BlackVetoes = blackVetoes };
        var resp = await _client.PostAsJsonAsync("/api/games", dto);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString();
    }

    private static byte[] GetBoard(JsonElement gameJson) =>
        gameJson.GetProperty("board").EnumerateArray().Select(x => x.GetByte()).ToArray();

    [Fact]
    public async Task MoveGame_ValidMove_ReturnsOk()
    {
        var id = await CreateGameAsync();
        var moveResp = await _client.PostAsJsonAsync($"/api/games/{id}/move", new { From = 52, To = 36, PromotionPiece = 0 });
        Assert.Equal(HttpStatusCode.OK, moveResp.StatusCode);
        var gameJson = await moveResp.Content.ReadFromJsonAsync<JsonElement>();
        var board = GetBoard(gameJson);
        Assert.Equal((byte)2, board[36]); // white pawn moved to e4
        Assert.Equal((byte)0, board[52]); // source empty
        Assert.Equal((byte)0, gameJson.GetProperty("gameStatus").GetByte()); // InProgress
    }

    [Fact]
    public async Task MoveGame_InvalidMove_ReturnsBadRequest()
    {
        var id = await CreateGameAsync();
        // e2 already has a pawn, no move possible from there
        var moveResp = await _client.PostAsJsonAsync($"/api/games/{id}/move", new { From = 52, To = 53, PromotionPiece = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, moveResp.StatusCode);
    }

    [Fact]
    public async Task MoveGame_Unauthorized_WhenNoToken()
    {
        var client = HttpClientFactory.Create();
        var createResp = await _client.PostAsJsonAsync("/api/games", new { WhitePlayer = (byte)1, BlackPlayer = (byte)2 });
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var moveResp = await client.PostAsJsonAsync($"/api/games/{id}/move", new { From = 52, To = 36, PromotionPiece = 0 });
        Assert.Equal(HttpStatusCode.Unauthorized, moveResp.StatusCode);
    }

    [Fact]
    public async Task MoveGame_UnknownGame_ReturnsNotFound()
    {
        var moveResp = await _client.PostAsJsonAsync("/api/games/00000000-0000-0000-0000-000000000000/move", new { From = 52, To = 36, PromotionPiece = 0 });
        Assert.Equal(HttpStatusCode.NotFound, moveResp.StatusCode);
    }
}

public class UndoVetoStatusTests : IClassFixture<TestHost>
{
    private readonly HttpClient _client;
    public UndoVetoStatusTests(TestHost factory)
    {
        _client = factory.CreateClient();
        MoveEndpointTests.Auth(_client).GetAwaiter().GetResult();
    }

    private async Task<string> CreateGameAsync(bool enableUndo = false, byte whiteVetoes = 0, byte blackVetoes = 0)
    {
        var dto = new { WhitePlayer = (byte)1, BlackPlayer = (byte)2, EnableUndo = enableUndo, WhiteVetoes = whiteVetoes, BlackVetoes = blackVetoes };
        var resp = await _client.PostAsJsonAsync("/api/games", dto);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString();
    }

    private async Task<byte[]> MakeMoveAsync(string id, int from, int to)
    {
        var resp = await _client.PostAsJsonAsync($"/api/games/{id}/move", new { From = from, To = to, PromotionPiece = 0 });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("board").EnumerateArray().Select(x => x.GetByte()).ToArray();
    }

    [Fact]
    public async Task Undo_WhenAllowed_ReturnsOk()
    {
        var id = await CreateGameAsync(enableUndo: true);
        var moved = await MakeMoveAsync(id, 52, 36);
        Assert.Equal((byte)2, moved[36]);

        var undoResp = await _client.PostAsJsonAsync($"/api/games/{id}/undo", 1);
        Assert.Equal(HttpStatusCode.OK, undoResp.StatusCode);
        var json = await undoResp.Content.ReadFromJsonAsync<JsonElement>();
        var board = json.GetProperty("board").EnumerateArray().Select(x => x.GetByte()).ToArray();
        Assert.Equal((byte)2, board[52]);
        Assert.Equal((byte)0, board[36]);
    }

    [Fact]
    public async Task Undo_WhenDisabled_ReturnsBadRequest()
    {
        var id = await CreateGameAsync();
        await MakeMoveAsync(id, 52, 36);
        var undoResp = await _client.PostAsJsonAsync($"/api/games/{id}/undo", 1);
        Assert.Equal(HttpStatusCode.BadRequest, undoResp.StatusCode);
    }

    [Fact]
    public async Task Veto_ConsumesVetoPower()
    {
        var id = await CreateGameAsync(whiteVetoes: 1, blackVetoes: 1);
        var moved = await MakeMoveAsync(id, 52, 36);
        Assert.Equal((byte)2, moved[36]);

        // black vetoes white's move
        var vetoResp = await _client.PostAsJsonAsync($"/api/games/{id}/veto", 2);
        Assert.Equal(HttpStatusCode.OK, vetoResp.StatusCode);
        var json = await vetoResp.Content.ReadFromJsonAsync<JsonElement>();
        var board = json.GetProperty("board").EnumerateArray().Select(x => x.GetByte()).ToArray();
        Assert.Equal((byte)2, board[52]);
        Assert.Equal((byte)0, board[36]);
        Assert.Equal(1, json.GetProperty("whiteVetoesRemaining").GetByte());
        Assert.Equal(0, json.GetProperty("blackVetoesRemaining").GetByte());
    }

    [Fact]
    public async Task Veto_WithoutVetoPower_ReturnsBadRequest()
    {
        var id = await CreateGameAsync();
        await MakeMoveAsync(id, 52, 36);
        var vetoResp = await _client.PostAsJsonAsync($"/api/games/{id}/veto", 2);
        Assert.Equal(HttpStatusCode.BadRequest, vetoResp.StatusCode);
    }

    [Fact]
    public async Task Status_ReturnsInProgress()
    {
        var id = await CreateGameAsync();
        var resp = await _client.GetAsync($"/api/games/{id}/status");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal((byte)GameStatus.InProgress, (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetByte());
    }

    [Fact]
    public async Task Status_Unauthorized_WhenNoToken()
    {
        var id = await CreateGameAsync();
        var client = HttpClientFactory.Create();
        var resp = await client.GetAsync($"/api/games/{id}/status");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
