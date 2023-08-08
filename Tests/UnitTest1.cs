using ChessChallenge.API;
using ChessChallenge.Example;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Engine = ChessChallenge.Chess;
using CTimer = ChessChallenge.API.Timer;

namespace Tests
{
    public class MyBotTests
    {
        public static ChessChallenge.API.Timer EasyTimer(int millis = 1_000_000)
        {
            return new ChessChallenge.API.Timer(millis);
        }

        public static bool BoardIsStalemate(Board board) {
            return board.GetLegalMoves().Length == 0;
        }

        public static IEnumerable<object[]> MakeConfiguration(string[] fenPositions, uint minDepth, uint maxDepth, bool flipBoard=true, bool loopSeeds=true)
        {
            //List<int> seeds = new ();
            //if (loopSeeds)
            //{
            //    seeds.AddRange(new int[] { 0, 1, 42, 420, 1337, 0xCAFE, 0xAFFE, 0xBEEF });
            //} else
            //{
            //    seeds.Add(0);
            //}

            foreach (var position in fenPositions)
            {
                List<string> fenViews = new List<string>();
                fenViews.Add(position);
                if (flipBoard)
                {
                    fenViews.Add(Engine.FenUtility.FlipFen(position));
                }

                for (uint depth = minDepth; depth <= maxDepth; depth++)
                {
                    foreach (var fen in fenViews)
                    {
                        //foreach (var seed in seeds)
                        //{
                        yield return new object[] { fen, depth }; //, seed };
                        //}   
                    }
                }
            }
        }

        public (Board, CTimer, MyBot) ArrangeBoardBotTimer(String fenString, uint? searchDepth = null, int? seed = null)
        {
            var board = Board.CreateBoardFromFEN(fenString);
            var timer = new ChessChallenge.API.Timer(999_999);

            MyBot bot;

            if (searchDepth.HasValue)
            {
                if (seed.HasValue)
                {
                    bot = new MyBot(searchDepth: searchDepth.Value, seed: seed.Value);
                } else
                {
                    bot = new MyBot(searchDepth: searchDepth.Value);
                }
            } else
            {
                if (seed.HasValue)
                {
                    bot = new MyBot(seed: seed.Value);
                }
                else
                {
                    bot = new MyBot();
                }
            }

            return (board, timer, bot);
        }
        

        [Theory]
        [InlineData("r1b1k1nr/ppppqppp/2n5/2b1P3/8/2N2N2/PPP1PPPP/R1BQKB1R w KQkq - 0 1")]
        [InlineData("2kr3r/p1ppqp2/bpn2p1p/8/1P4P1/2N2N2/P1PBPK1P/1R1Q1B1R b Hk - 0 1")]
        [InlineData("7k/3q4/1P2n3/4Rb2/8/2N3p1/8/K2R1R2 w - - 0 1")]
        public void EvaluationsAreSymmetric(String fenPosition)
        {
            MyBot bot = new();

            string flippedPosition = Engine.FenUtility.FlipFen(fenPosition);

            int fenEval = bot.EvaluateForWhite(Board.CreateBoardFromFEN(fenPosition), EasyTimer(), new Stack<ulong>());
            int flipEval = bot.EvaluateForWhite(Board.CreateBoardFromFEN(flippedPosition), EasyTimer(), new Stack<ulong>());

            Assert.Equal(fenEval, -flipEval);
        }

        [Theory]
        [InlineData("7Q/8/8/8/4k3/8/8/K7 w - - 0 1", "7R/8/8/8/4k3/8/8/K7 w - - 0 1")]
        public void EvaluationForQueenIsBetterThanRook(String fenPositionQ, String fenPositionR)
        {
            MyBot bot = new();

            int evalQ = bot.EvaluateForWhite(Board.CreateBoardFromFEN(fenPositionQ), EasyTimer(), new Stack<ulong>());
            int evalR = bot.EvaluateForWhite(Board.CreateBoardFromFEN(fenPositionR), EasyTimer(), new Stack<ulong>());

            Assert.True(evalQ > evalR);
        }


        [Theory]
        [InlineData("8/7P/8/8/4k3/8/8/K7 w - - 0 1", 1)]
        [InlineData("8/7P/8/8/4k3/8/8/K7 w - - 0 1", 2)]
        [InlineData("8/7P/8/8/4k3/8/8/K7 w - - 0 1", 3)]
        public void PromotionFavorsQueens(String fenPosition, uint depth)
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

        public static IEnumerable<object[]> checkMateInOneData = 
            MakeConfiguration(new string[] { "k7/7R/1K6/8/8/8/8/8 w - - 0 1" }, 1, 5);
        [Theory]
        [MemberData(nameof(checkMateInOneData))]
        public void CheckMatesInOneAreTaken(String fenPosition, uint depth)
        {
            MyBot bot = new MyBot(depth);
            Board b = Board.CreateBoardFromFEN(fenPosition);

            Move move = bot.Think(b, EasyTimer());
            b.MakeMove(move);

            Assert.Equal(PieceType.Rook, move.MovePieceType);
            Assert.True(b.IsInCheckmate());
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
        public void CheckMatesInOneAreEscaped(String fenPosition, uint depth, int seed=0)
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
        public void DoesNotBlunderAQueen(String fenPosition, uint depth)
        {
            string[] positions = { fenPosition, Engine.FenUtility.FlipFen(fenPosition) };
            MyBot bot = new MyBot(depth);
            EvilBot enemy = new EvilBot();

            foreach (String fen in positions)
            {
                Board board = Board.CreateBoardFromFEN(fenPosition);
                ChessChallenge.API.Timer t = EasyTimer();

                while (board.FiftyMoveCounter < 50)
                {
                    Move move = bot.Think(board, t);
                    board.MakeMove(move);
                    if (BoardIsStalemate(board))
                    {
                        break;
                    }
                    Move move2 = enemy.Think(board, t);
                    board.MakeMove(move2);
                    if (BoardIsStalemate(board))
                    {
                        break;
                    }

                    // No Queen will be captured
                    Assert.Equal(PieceType.None, move2.CapturePieceType);
                }
            }
        }

        [Theory]
        [InlineData("8/8/8/8/3k4/8/8/K6Q w - - 0 1", 3)]
        public void DoesNotBlunderAStalemate(String fenPosition, uint depth)
        {
            string[] positions = { fenPosition, Engine.FenUtility.FlipFen(fenPosition) };
            MyBot bot = new MyBot(depth);
            EvilBot enemy = new EvilBot();

            foreach (String fen in positions)
            {
                Board board = Board.CreateBoardFromFEN(fenPosition);
                ChessChallenge.API.Timer t = EasyTimer();

                while (board.FiftyMoveCounter < 50)
                {
                    Move move = bot.Think(board, t);
                    board.MakeMove(move);
                    Assert.False(BoardIsStalemate(board));
                    Move move2 = enemy.Think(board, t);
                    board.MakeMove(move2);
                    Assert.False(BoardIsStalemate(board));
                }
            }
        }

        [Theory]
        [InlineData("k3r3/8/8/1N6/3p4/8/8/K7 w - - 0 1", 4)]
        public void FindsFork(String fenPosition, uint depth)
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

        //[Theory]
        //// [InlineData("6k1 / 7p / b6P / p7 / 8 / 8 / 4r1p1 / 2K5 w - -0 3", 1)]
        //public void DoesNotPerformIllegalMove(string fenPosition, uint depth)
        //{
        //    (var board, var timer, var bot) = ArrangeBoardBotTimer(fenPosition, depth, null);
        //    var move = bot.Think(board, timer);

        //    Assert.False(board.IsInCheckmate());
        //    Assert.False(board.IsInsufficientMaterial());
        //    Assert.False(board.IsDraw());

        //    // Should not raise
        //    board.MakeMove(move);
        //}
    }
}