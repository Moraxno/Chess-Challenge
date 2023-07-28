using ChessChallenge.API;
using System;
using System.Collections.Generic;

namespace Tests
{
    public class MyBotTests
    {
        [Fact]
        public void PromotionFavorsQueens()
        {
            Board b = Board.CreateBoardFromFEN("k7/7P/8/8/8/8/8/K7 w - - 0 1");
            MyBot bot = new();
            Move move = bot.Think(b, new ChessChallenge.API.Timer(50000));
            Assert.True(move.IsPromotion);
            Assert.Equal(PieceType.Queen, move.PromotionPieceType);
        }

        [Fact]
        public void CheckMatesInOneAreTaken()
        {
            Board b = Board.CreateBoardFromFEN("k7/7R/1K6/8/8/8/8/8 w - - 0 1");

            foreach (bool isWhite in [true, false])
            {
                String playerKing = isWhite ? "K" : "k";
                String enemyKing = isWhite ? "k" : "K";
                String fen = "k7/7R/1K6/8/8/8/8/8 w - - 0 1";
                for (int depth = 0; depth < 4; depth++)
                {
                    Board b = Board.CreateBoardFromFEN();
                    MyBot bot = new MyBot(0);
                    ChessChallenge.API.Timer t = new ChessChallenge.API.Timer(5000000);
                    Move move = bot.Think(b, t);
                    Assert.Equal(PieceType.Rook, move.MovePieceType);

                    b.MakeMove(move);
                    Assert.True(b.IsInCheckmate());
                }
            }
            
        }
    }
}