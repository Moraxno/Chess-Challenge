using ChessChallenge.API;
using ChessChallenge.Example;
using System.Collections;
using System.Collections.Generic;
using Engine = ChessChallenge.Chess;

namespace Tests
{
    public class MyBotTests
    {
        public static ChessChallenge.API.Timer EasyTimer(int millis = 1_000_000)
        {
            return new ChessChallenge.API.Timer(millis);
        }

        [Theory]
        [InlineData("r1b1k1nr/ppppqppp/2n5/2b1P3/8/2N2N2/PPP1PPPP/R1BQKB1R w KQkq - 0 1")]
        [InlineData("2kr3r/p1ppqp2/bpn2p1p/8/1P4P1/2N2N2/P1PBPK1P/1R1Q1B1R b Hk - 0 1")]
        [InlineData("7k/3q4/1P2n3/4Rb2/8/2N3p1/8/K2R1R2 w - - 0 1")]
        public void EvaluationsAreSymmetric(String fenPosition)
        {
            MyBot bot = new();

            string flippedPosition = Engine.FenUtility.FlipFen(fenPosition);

            int fenEval = bot.EvaluateForWhite(Board.CreateBoardFromFEN(fenPosition), EasyTimer());
            int flipEval = bot.EvaluateForWhite(Board.CreateBoardFromFEN(flippedPosition), EasyTimer());

            Assert.Equal(fenEval, -flipEval);
        }


        [Theory]
        [InlineData("k7/7P/8/8/8/8/8/K7 w - - 0 1", 0)]
        [InlineData("k7/7P/8/8/8/8/8/K7 w - - 0 1", 1)]
        [InlineData("k7/7P/8/8/8/8/8/K7 w - - 0 1", 2)]
        [InlineData("k7/7P/8/8/8/8/8/K7 w - - 0 1", 3)]
        public void PromotionFavorsQueens(String fenPosition, int depth)
        {
            string[] positions = { fenPosition, Engine.FenUtility.FlipFen(fenPosition) };
            MyBot bot = new MyBot(depth);

            foreach (String fen in positions)
            {
                Board b = Board.CreateBoardFromFEN(fenPosition);
                ChessChallenge.API.Timer t = new ChessChallenge.API.Timer(5000000);
                Move move = bot.Think(b, EasyTimer());
                Assert.True(move.IsPromotion);
                Assert.Equal(PieceType.Queen, move.PromotionPieceType);
            }
        }

        [Theory]
        [InlineData("k7/7R/1K6/8/8/8/8/8 w - - 0 1", 0)]
        [InlineData("k7/7R/1K6/8/8/8/8/8 w - - 0 1", 1)]
        [InlineData("k7/7R/1K6/8/8/8/8/8 w - - 0 1", 2)]
        [InlineData("k7/7R/1K6/8/8/8/8/8 w - - 0 1", 3)]
        public void CheckMatesInOneAreTaken(String fenPosition, int depth)
        {
            string[] positions = { fenPosition, Engine.FenUtility.FlipFen(fenPosition) };
            MyBot bot = new MyBot(depth);

            foreach (String fen in positions)
            {
                Board b = Board.CreateBoardFromFEN(fenPosition);

                Move move = bot.Think(b, EasyTimer());
                b.MakeMove(move);

                Assert.Equal(PieceType.Rook, move.MovePieceType);
                Assert.True(b.IsInCheckmate());
            }
        }

        [Theory]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 2)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 3)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 2, 42)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 3, 42)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 2, 1337)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 3, 1337)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 2, 420)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 3, 420)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 2, -174)]
        [InlineData("3k4/R7/2K5/8/8/8/8/8 b - - 0 1", 3, -174)]
        public void CheckMatesInOneAreEscaped(String fenPosition, int depth, int seed=0)
        {
            string[] positions = { fenPosition, Engine.FenUtility.FlipFen(fenPosition) };

            foreach (String fen in positions)
            {
                MyBot bot = new MyBot(depth, seed);
                Board b = Board.CreateBoardFromFEN(fenPosition);

                Move move = bot.Think(b, EasyTimer());
                b.MakeMove(move);

                // King has to make escaping move away from other King
                Assert.NotEqual(2, move.TargetSquare.File);
                Assert.NotEqual(5, move.TargetSquare.File);
                Assert.False(b.IsInCheckmate());
            }
        }

        [Theory]
        [InlineData("8/8/8/8/3k4/8/8/K6Q w - - 0 1", 3)]
        public void DoesNotBlunderAQueen(String fenPosition, int depth)
        {
            string[] positions = { fenPosition, Engine.FenUtility.FlipFen(fenPosition) };
            MyBot bot = new MyBot(depth);
            EvilBot enemy = new EvilBot();

            foreach (String fen in positions)
            {
                Board b = Board.CreateBoardFromFEN(fenPosition);
                ChessChallenge.API.Timer t = EasyTimer();

                while (b.FiftyMoveCounter < 50)
                {
                    Move move = bot.Think(b, t);
                    b.MakeMove(move);
                    Move move2 = enemy.Think(b, t);
                    b.MakeMove(move2);

                    // No Queen will be captured
                    Assert.Equal(PieceType.None, move2.CapturePieceType);
                }
            }
        }

        [Theory]
        [InlineData("k3r3/8/8/1N6/3p4/8/8/K7 w - - 0 1", 4)]
        public void FindsFork(String fenPosition, int depth)
        {
            string[] positions = { fenPosition, Engine.FenUtility.FlipFen(fenPosition) };
            MyBot bot = new MyBot(depth);
            EvilBot enemy = new EvilBot();

            foreach (String fen in positions)
            {
                Board b = Board.CreateBoardFromFEN(fenPosition);
                ChessChallenge.API.Timer t = new ChessChallenge.API.Timer(5000000);

                Move move = bot.Think(b, t);
                b.MakeMove(move);

                // Bot knight checks instead of instantly grabbing the pawn
                Assert.Equal(PieceType.Knight, move.MovePieceType);
                Assert.True(b.IsInCheck());
                
            
                // Enemy responds
                move = enemy.Think(b, t);
                b.MakeMove(move);

                // Bot responds
                move = bot.Think(b, t);
                b.MakeMove(move);

                // Bot actually grabs the rook
                Assert.Equal(PieceType.Knight, move.MovePieceType);
                Assert.Equal(PieceType.Rook, move.CapturePieceType);
            }
        }
    }
}