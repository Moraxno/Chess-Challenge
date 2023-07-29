using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

public class Evaluation
{
    public int score;
    public List<Move> line;

    public Evaluation(int score, List<Move> line)
    {
        this.score = score;
        this.line = line;
    }
}
public class MyBot : IChessBot
{
    uint depth = 0;
    int seed = 0;

    Evaluation lastEval;

    // Piece values:      null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = {    0,  100,    300,    300,  500,   900, 1000 };
    int[] maxActivity = {    0,    0,      8,     13,   14,    27,    8 };

    int softInfinity = 1_234_567;

    Dictionary<ulong, int> cache = new();

    public MyBot(uint searchDepth = 3, int seed = 0)
    {
        this.depth = searchDepth;
        this.seed = seed;
        this.lastEval = new Evaluation(-987654, new());
    }

    public bool IsDrawnGame(Board board)
    {
        return (board.IsInsufficientMaterial() || board.GameRepetitionHistory.Count(z => z == board.ZobristKey) >= 2 || board.FiftyMoveCounter >= 100 || (board.GetLegalMoves().Length == 0 && !board.IsInCheckmate()));
    }

    public int EvaluateForWhite(Board board, Timer timer, Stack<ulong> pseudoHistory)
    {
        int score = 0;

        List<ulong> fullHistory = new();
        fullHistory.AddRange(board.GameRepetitionHistory);
        fullHistory.AddRange(pseudoHistory);

        if (IsDrawnGame(board) || fullHistory.Count(z => z == board.ZobristKey) >= 3)
        {
            return 0;
        }

        if (cache.TryGetValue(board.ZobristKey, out score))
        {
            return score;
        }

        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? -softInfinity : softInfinity;
        }

        if (board.IsInCheck())
        {
            score += board.IsWhiteToMove ? -50 : 50;
        }

        score -= Math.Sign(score) * board.FiftyMoveCounter;

        var allMoves = board.GetLegalMoves();

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                int cofactor = piece.IsWhite ? 1 : -1;
                int value = pieceValues[(int)piece.PieceType];

                int centrality = 0; // (int)(70.0 - 10.0 * (Math.Abs((float)piece.Square.Rank - 3.5) + Math.Abs((float)piece.Square.File - 3.5)));
                int activity = 0;
                int kingSafety = 0;

                if (piece.PieceType == PieceType.King)
                {
                    kingSafety += Math.Abs(piece.Square.File * 10 - 35) * 5;
                } else if (piece.PieceType != PieceType.Pawn) 
                {
                    activity = 100 * allMoves.Count(m => m.StartSquare == piece.Square) / maxActivity[(int)piece.PieceType];
                } else
                {
                    var targetRank = piece.IsWhite ? 7 : 0;
                    activity -= 10 * Math.Abs(targetRank - piece.Square.Rank);
                }

                score += cofactor * (value + centrality + activity);

                //if (piece.IsPawn)
                //{
                //    // the further back any pawn is, the better it is for white ...
                //    score += ((piece.Square.Rank - 1) * 100) / 5;
                //}
            }
        }

        int capturePower = allMoves.Count(move => move.IsCapture);

        score += board.HasKingsideCastleRight(true) ? 25 : 0;
        score += board.HasQueensideCastleRight(true) ? 25 : 0;
        score -= board.HasKingsideCastleRight(false) ? 25 : 0;
        score -= board.HasQueensideCastleRight(false) ? 25 : 0;

        if (board.IsWhiteToMove)
        {
            score += capturePower;
        } else
        {
            score -= capturePower;
        }

        cache[board.ZobristKey] = score;
        return score;
    }

    public Evaluation NegaMax(Board board, Timer timer, bool maximizeForWhite, Stack<ulong> pseudoHistory, uint depth = 0)
    {
        List<Move> bestLine = new List<Move>();

        // Debug.WriteLine($"Evaluating the board: {board.GetFenString()}");
        if ( depth == 0 || IsDrawnGame(board) || board.IsInCheckmate())
        {
            return new Evaluation((maximizeForWhite ? 1 : -1) * EvaluateForWhite(board, timer, pseudoHistory), bestLine);
        }

        int bestScore = -softInfinity;
        List<Move> bestMoves = new();
        Move bestMove = new();
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            pseudoHistory.Push(board.ZobristKey);
            var eval = NegaMax(board, timer, !maximizeForWhite, pseudoHistory, depth - 1);
            eval.score *= -1;
            eval.score -= pseudoHistory.Count; // miniscul punishment for deeper searches
            board.UndoMove(move);
            pseudoHistory.Pop();

            if (eval.score > bestScore)
            {
                // Debug.WriteLine($"new Best from {bestScore} to {score}.");
                bestScore = eval.score;
                bestMove = move;
                bestMoves = eval.line;
            }
        }

        bestLine.Add(bestMove);
        bestLine.AddRange(bestMoves);

        var finalEval = new Evaluation(bestScore, bestLine);
        this.lastEval = finalEval;
        return finalEval;
    }
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        Random rng = new(seed);
        // Move moveToPlay = allMoves[rng.Next(allMoves.Length)];

        // int bestScore = int.MinValue;
        bool wePlayWhite = board.IsWhiteToMove;

        uint bonusDepth = 0;
        int dt = 0;
        int dt2 = 0;
        Evaluation eval;
        eval = NegaMax(board, timer, wePlayWhite, new Stack<ulong>(), this.depth + bonusDepth);
        //do
        //{
        //    dt = timer.MillisecondsRemaining;
        //    eval = NegaMax(board, timer, wePlayWhite, this.depth + bonusDepth);
        //    dt2 = timer.MillisecondsRemaining;
        //    Console.WriteLine($"{eval.score} @ {this.depth + bonusDepth}");
        //    bonusDepth += 1;

        //} while (dt - dt2 < 250 && eval.score < softInfinity && timer.MillisecondsElapsedThisTurn < 1500 && timer.MillisecondsRemaining > 10_000);

        return eval.line[0];

        //List<Move> bestMoves = new();


        //foreach (Move move in allMoves)
        //{
        //    board.MakeMove(move);
        //    int eval = NegaMax(board, timer, wePlayWhite, this.depth).score;
        //    board.UndoMove(move);

        //    if ( eval > bestScore)
        //    {
        //        bestScore = eval;
        //        bestMoves.Clear();
        //        bestMoves.Add(move);
        //    } else if (eval == bestScore)
        //    {
        //        bestMoves.Add(move);
        //    }
        //}

        //return bestMoves[rng.Next(bestMoves.Count)];
    }
}