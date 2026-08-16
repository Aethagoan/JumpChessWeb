using System;
using JC.GameLib;
using Xunit;
using Xunit.Abstractions;

namespace JC.GameLib.Tests;


public class DebugTest
{
    private readonly ITestOutputHelper output;

    public DebugTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static byte PositionFromNotation(string notation)
    {
        if (notation.Length != 2)
            throw new ArgumentException("Invalid notation");
        
        char file = notation[0]; // a-h
        char rank = notation[1]; // 1-8
        
        int col = file - 'a';
        int row = rank - '1';
        
        return (byte)(row * 8 + col);
    }

    [Fact]
    public void DebugRookJump()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        
        board[PositionFromNotation("e2")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("a1")] = Game.WHITE_ROOK;
        board[PositionFromNotation("d1")] = Game.WHITE_PAWN;
        
        output.WriteLine("Board setup:");
        output.WriteLine($"e2 position: {PositionFromNotation("e2")}, piece: {board[PositionFromNotation("e2")]}");
        output.WriteLine($"e8 position: {PositionFromNotation("e8")}, piece: {board[PositionFromNotation("e8")]}");
        output.WriteLine($"a1 position: {PositionFromNotation("a1")}, piece: {board[PositionFromNotation("a1")]}");
        output.WriteLine($"d1 position: {PositionFromNotation("d1")}, piece: {board[PositionFromNotation("d1")]}");
        output.WriteLine($"h1 position: {PositionFromNotation("h1")}, piece: {board[PositionFromNotation("h1")]}");
        
        var game = new Game(board, 1, 2);
        
        bool result = game.Move(PositionFromNotation("a1"), PositionFromNotation("h1"));
        
        output.WriteLine($"Move result: {result}");
        output.WriteLine($"Rook at a1 after: {game.board[PositionFromNotation("a1")]}");
        output.WriteLine($"Rook at h1 after: {game.board[PositionFromNotation("h1")]}");
        output.WriteLine($"Pawn at d1 after: {game.board[PositionFromNotation("d1")]}");
        
        Assert.True(result);
    }

}
