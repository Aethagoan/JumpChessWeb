using System;
using Xunit;
using JC.GameLib;

namespace JC.GameLib.Tests
{
    public class GameTests
    {
        [Fact]
        public void NewGame_InitialBoard_SetUpCorrectly()
        {
            var game = new Game(1,2);
            // Count pieces on board
            int pieceCount = 0;
            foreach (var piece in game.board)
                if (piece != Game.NONE) pieceCount++;
            // Standard chess start has 32 pieces
            Assert.Equal(32, pieceCount);
        }

        [Fact]
        public void Move_WhitePawnForwardOne_Valid()
        {
            var game = new Game(1,2);
            // e2 to e3 is from 12 to 4? Actually board index: row*8+col, row0 top
            // White pawns start at row 6 (index 48-55). e2 is row6 col4 => 48+4=52
            int from = 52; // e2
            int to = 44;   // e3 (row5 col4)
            bool result = game.Move((byte)from, (byte)to);
            Assert.True(result);
            Assert.Equal(Game.WHITE_PAWN, game.board[to]);
            Assert.Equal(Game.NONE, game.board[from]);
        }

        [Fact]
        public void Move_EmptyFrom_Rejects()
        {
            var game = new Game(1,2);
            bool result = game.Move((byte)0, (byte)1); // empty square to empty square
            Assert.False(result);
        }
    }
}
