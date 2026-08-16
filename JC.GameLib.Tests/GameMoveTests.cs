using System;
using JC.GameLib;
using Xunit;

namespace JC.GameLib.Tests;

/// <summary>
/// Tests for basic chess piece movements and game rules
/// </summary>
public class GameMoveTests
{
    #region Helper Methods

    /// <summary>
    /// Helper to convert chess notation (e.g., "e2") to board position (0-63)
    /// Board layout: a1=0, b1=1, ..., h1=7, a2=8, ..., h8=63
    /// </summary>
    private static byte PositionFromNotation(string notation)
    {
        if (notation.Length != 2)
            throw new ArgumentException("Invalid notation");
        
        char file = notation[0]; // a-h
        char rank = notation[1]; // 1-8
        
        int col = file - 'a'; // 0-7
        int row = rank - '1'; // 0-7
        
        return (byte)(row * 8 + col);
    }

    /// <summary>
    /// Helper to create a string representation of the board for debugging
    /// </summary>
    private static string BoardToString(byte[] board)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("  a b c d e f g h");
        for (int row = 7; row >= 0; row--)
        {
            sb.Append($"{row + 1} ");
            for (int col = 0; col < 8; col++)
            {
                byte piece = board[row * 8 + col];
                char symbol = piece switch
                {
                    Game.WHITE_KING => 'K',
                    Game.WHITE_QUEEN => 'Q',
                    Game.WHITE_ROOK => 'R',
                    Game.WHITE_BISHOP => 'B',
                    Game.WHITE_KNIGHT => 'N',
                    Game.WHITE_PAWN => 'P',
                    Game.BLACK_KING => 'k',
                    Game.BLACK_QUEEN => 'q',
                    Game.BLACK_ROOK => 'r',
                    Game.BLACK_BISHOP => 'b',
                    Game.BLACK_KNIGHT => 'n',
                    Game.BLACK_PAWN => 'p',
                    _ => '.'
                };
                sb.Append($"{symbol} ");
            }
            sb.AppendLine($"{row + 1}");
        }
        sb.AppendLine("  a b c d e f g h");
        return sb.ToString();
    }

    #endregion

    #region Basic Pawn Movement Tests

    [Fact]
    public void PawnCanMoveForwardOneSquare()
    {
        var game = new Game(1, 2);
        bool result = game.Move(PositionFromNotation("e2"), PositionFromNotation("e3"));
        Assert.True(result, "Pawn should be able to move forward one square");
        Assert.Equal(Game.NONE, game.board[PositionFromNotation("e2")]);
        Assert.Equal(Game.WHITE_PAWN, game.board[PositionFromNotation("e3")]);
    }

    [Fact]
    public void PawnCanMoveForwardTwoSquaresFromStartingPosition()
    {
        var game = new Game(1, 2);
        bool result = game.Move(PositionFromNotation("e2"), PositionFromNotation("e4"));
        Assert.True(result, "Pawn should be able to move forward two squares from starting position");
        Assert.Equal(Game.NONE, game.board[PositionFromNotation("e2")]);
        Assert.Equal(Game.WHITE_PAWN, game.board[PositionFromNotation("e4")]);
    }

    [Fact]
    public void PawnCannotMoveBackward()
    {
        var game = new Game(1, 2);
        game.Move(PositionFromNotation("e2"), PositionFromNotation("e3"));
        game.Move(PositionFromNotation("e7"), PositionFromNotation("e6")); // Black moves
        bool result = game.Move(PositionFromNotation("e3"), PositionFromNotation("e2"));
        Assert.False(result, "Pawn should not be able to move backward");
    }

    [Fact]
    public void PawnCanJumpTwoSpaces()
    {
        var game = new Game(1, 2);
        game.Move(PositionFromNotation("e2"), PositionFromNotation("e3"));
        game.Move(PositionFromNotation("h7"), PositionFromNotation("h6"));
        bool result = game.Move(PositionFromNotation("e3"), PositionFromNotation("e5"));
        Assert.True(result, "Pawn moves forward two at any time");
    }

    [Fact]
    public void PawnCanJumpFriendlyPiece()
    {
        var game = new Game(1, 2);
        game.Move(PositionFromNotation("d1"),PositionFromNotation("d3"));
        game.Move(PositionFromNotation("h7"), PositionFromNotation("h6"));
        bool result = game.Move(PositionFromNotation("e2"), PositionFromNotation("e4"));
        Assert.True(result, "Pawn Jump Friendly Piece");
    }

    [Fact]
    public void PawnCanCaptureDiagonally()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e4")] = Game.WHITE_PAWN;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("d5")] = Game.BLACK_PAWN;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("e4"), PositionFromNotation("d5"));
        Assert.True(result, "Pawn should be able to capture diagonally");
        Assert.Equal(Game.WHITE_PAWN, game.board[PositionFromNotation("d5")]);
    }

    #endregion

    #region Knight Movement Tests

    [Fact]
    public void KnightCanMoveInLShape()
    {
        var game = new Game(1, 2);
        bool result = game.Move(PositionFromNotation("b1"), PositionFromNotation("c3"));
        Assert.True(result, "Knight should be able to move in L-shape");
        Assert.Equal(Game.NONE, game.board[PositionFromNotation("b1")]);
        Assert.Equal(Game.WHITE_KNIGHT, game.board[PositionFromNotation("c3")]);
    }

    [Fact]
    public void KnightCanJumpOverPieces()
    {
        var game = new Game(1, 2);
        bool result = game.Move(PositionFromNotation("b1"), PositionFromNotation("a3"));
        Assert.True(result, "Knight should be able to jump over pieces");
        Assert.Equal(Game.WHITE_KNIGHT, game.board[PositionFromNotation("a3")]);
    }

    [Fact]
    public void KnightCanMove3SquaresInJumpChess()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("e4")] = Game.WHITE_KNIGHT;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("e4"), PositionFromNotation("h4"));
        Assert.True(result, "Knight should be able to move 3 squares in any direction (Jump Chess rule)");
        Assert.Equal(Game.WHITE_KNIGHT, game.board[PositionFromNotation("h4")]);
    }

    #endregion

    #region Rook Movement Tests

    [Fact]
    public void RookCanMoveHorizontally()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("a1")] = Game.WHITE_ROOK;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("a1"), PositionFromNotation("h1"));
        Assert.True(result, "Rook should be able to move horizontally");
        Assert.Equal(Game.WHITE_ROOK, game.board[PositionFromNotation("h1")]);
    }

    [Fact]
    public void RookCanMoveVertically()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("a1")] = Game.WHITE_ROOK;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("a1"), PositionFromNotation("a8"));
        Assert.True(result, "Rook should be able to move vertically");
        Assert.Equal(Game.WHITE_ROOK, game.board[PositionFromNotation("a8")]);
    }

    [Fact]
    public void RookCanJumpOverOneFriendlyPiece()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e2")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("a1")] = Game.WHITE_ROOK;
        board[PositionFromNotation("d1")] = Game.WHITE_PAWN;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("a1"), PositionFromNotation("h1"));
        Assert.True(result, "Rook should be able to jump over one friendly piece (Jump Chess rule)");
        Assert.Equal(Game.WHITE_ROOK, game.board[PositionFromNotation("h1")]);
        Assert.Equal(Game.WHITE_PAWN, game.board[PositionFromNotation("d1")]);
    }

    #endregion

    #region Bishop Movement Tests

    [Fact]
    public void BishopCanMoveDiagonally()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("a1")] = Game.WHITE_BISHOP;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("a1"), PositionFromNotation("h8"));
        Assert.True(result, "Bishop should be able to move diagonally");
        Assert.Equal(Game.WHITE_BISHOP, game.board[PositionFromNotation("h8")]);
    }

    #endregion

    #region Queen Movement Tests

    [Fact]
    public void QueenCanMoveHorizontally()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("d1")] = Game.WHITE_QUEEN;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("d1"), PositionFromNotation("h1"));
        Assert.True(result, "Queen should be able to move horizontally");
        Assert.Equal(Game.WHITE_QUEEN, game.board[PositionFromNotation("h1")]);
    }

    [Fact]
    public void QueenCanMoveDiagonally()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("d1")] = Game.WHITE_QUEEN;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("d1"), PositionFromNotation("h5"));
        Assert.True(result, "Queen should be able to move diagonally");
        Assert.Equal(Game.WHITE_QUEEN, game.board[PositionFromNotation("h5")]);
    }

    #endregion

    #region King Movement Tests

    [Fact]
    public void KingCanMoveOneSquareInAnyDirection()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e4")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("e4"), PositionFromNotation("e5"));
        Assert.True(result, "King should be able to move one square");
        Assert.Equal(Game.WHITE_KING, game.board[PositionFromNotation("e5")]);
    }

    [Fact]
    public void KingCannotMoveTwoSquares()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e4")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("e4"), PositionFromNotation("e6"));
        Assert.False(result, "King should not be able to move two squares (except castling)");
    }

    #endregion

    #region Turn-Based Tests

    [Fact]
    public void WhiteMovesFirst()
    {
        var game = new Game(1, 2);
        Assert.True(game.whiteToMove, "White should move first");
    }

    [Fact]
    public void BlackCannotMoveFirst()
    {
        var game = new Game(1, 2);
        bool result = game.Move(PositionFromNotation("e7"), PositionFromNotation("e6"));
        Assert.False(result, "Black should not be able to move first");
    }

    [Fact]
    public void TurnsAlternate()
    {
        var game = new Game(1, 2);
        game.Move(PositionFromNotation("e2"), PositionFromNotation("e4"));
        Assert.False(game.whiteToMove, "After white moves, it should be black's turn");
        game.Move(PositionFromNotation("e7"), PositionFromNotation("e5"));
        Assert.True(game.whiteToMove, "After black moves, it should be white's turn again");
    }

    #endregion

    #region Capture Tests

    [Fact]
    public void PieceCanCaptureEnemyPiece()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("e4")] = Game.WHITE_QUEEN;
        board[PositionFromNotation("e6")] = Game.BLACK_PAWN;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("e4"), PositionFromNotation("e6"));
        Assert.True(result, "Piece should be able to capture enemy piece");
        Assert.Equal(Game.WHITE_QUEEN, game.board[PositionFromNotation("e6")]);
    }

    [Fact]
    public void PieceCanCaptureFriendlyPiece_JumpChessRule()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("e4")] = Game.WHITE_QUEEN;
        board[PositionFromNotation("e5")] = Game.WHITE_PAWN;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("e4"), PositionFromNotation("e5"));
        Assert.True(result, "Piece should be able to capture friendly piece (Jump Chess rule)");
        Assert.Equal(Game.WHITE_QUEEN, game.board[PositionFromNotation("e5")]);
    }

    [Fact]
    public void CannotCaptureOwnKing()
    {
        var board = new byte[64];
        for (int i = 0; i < 64; i++) board[i] = Game.NONE;
        board[PositionFromNotation("e1")] = Game.WHITE_KING;
        board[PositionFromNotation("e8")] = Game.BLACK_KING;
        board[PositionFromNotation("e2")] = Game.WHITE_QUEEN;
        var game = new Game(board, 1, 2);
        bool result = game.Move(PositionFromNotation("e2"), PositionFromNotation("e1"));
        Assert.False(result, "Cannot capture own king");
    }

    #endregion

    #region Full Game Scenario Test

    [Fact]
    public void CanPlayMultipleMoves()
    {
        var game = new Game(1, 2);
        Assert.True(game.Move(PositionFromNotation("e2"), PositionFromNotation("e4")), "Move 1: e2-e4 should succeed");
        Assert.True(game.Move(PositionFromNotation("e7"), PositionFromNotation("e5")), "Move 2: e7-e5 should succeed");
        Assert.True(game.Move(PositionFromNotation("g1"), PositionFromNotation("f3")), "..." );
        Assert.True(game.Move(PositionFromNotation("b8"), PositionFromNotation("c6")), "..." );
        Assert.True(game.Move(PositionFromNotation("f1"), PositionFromNotation("c4")), "..." );
        Assert.Equal(GameStatus.InProgress, game.gameStatus);
        Assert.Equal(5, game.moves.Count);
    }

    #endregion

    #region Board State Tests

    [Fact]
    public void NewGameHasCorrectInitialSetup()
    {
        var game = new Game(1, 2);
        Assert.Equal(Game.WHITE_ROOK, game.board[PositionFromNotation("a1")]);
        Assert.Equal(Game.WHITE_KNIGHT, game.board[PositionFromNotation("b1")]);
        Assert.Equal(Game.WHITE_BISHOP, game.board[PositionFromNotation("c1")]);
        Assert.Equal(Game.WHITE_QUEEN, game.board[PositionFromNotation("d1")]);
        Assert.Equal(Game.WHITE_KING, game.board[PositionFromNotation("e1")]);
        Assert.Equal(Game.WHITE_PAWN, game.board[PositionFromNotation("e2")]);
        Assert.Equal(Game.BLACK_ROOK, game.board[PositionFromNotation("a8")]);
        Assert.Equal(Game.BLACK_KING, game.board[PositionFromNotation("e8")]);
        Assert.Equal(Game.BLACK_PAWN, game.board[PositionFromNotation("e7")]);
        Assert.Equal(Game.NONE, game.board[PositionFromNotation("e4")]);
    }

    #endregion

    #region Invalid Move Tests

    [Fact]
    public void CannotMoveToSameSquare()
    {
        var game = new Game(1, 2);
        bool result = game.Move(PositionFromNotation("e2"), PositionFromNotation("e2"));
        Assert.False(result, "Cannot move to the same square");
    }

    [Fact]
    public void CannotMoveFromEmptySquare()
    {
        var game = new Game(1, 2);
        bool result = game.Move(PositionFromNotation("e4"), PositionFromNotation("e5"));
        Assert.False(result, "Cannot move from empty square" );
    }

    #endregion

}
