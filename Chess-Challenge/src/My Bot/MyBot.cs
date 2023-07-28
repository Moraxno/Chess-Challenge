using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static System.Formats.Asn1.AsnWriter;

public class MyBot : IChessBot
{
    int depth = 0;
    int seed = 0;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int softInfinity = 1_000_000;

    Dictionary<ulong, int> cache = new();

    public MyBot(int depth = 2, int seed = 0)
    {
        this.depth = depth;
        this.seed = seed;
    }

    public bool IsDrawnGame(Board board)
    {
        return (board.IsInsufficientMaterial() || board.IsRepeatedPosition() || board.FiftyMoveCounter >= 50 || board.GetLegalMoves().Length == 0 && !board.IsInCheckmate());
    }

    public int EvaluateForWhite(Board board, Timer timer)
    {
        int score = 0;

        if (cache.TryGetValue(board.ZobristKey, out score))
        {
            return score;
        }

        if (IsDrawnGame(board))
        {
            return 0;
        }

        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? -softInfinity : softInfinity;
        }
        
        if (board.IsInCheck())
        {
            score += board.IsWhiteToMove ? -25 : 25;
        }

        // score = 5 * Math.Max(0, board.FiftyMoveCounter - 40);

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                int cofactor = piece.IsWhite ? 1 : -1;
                int value = pieceValues[(int)piece.PieceType];

                int centrality = 0; // (int)(70.0 - 10.0 * (Math.Abs((float)piece.Square.Rank - 3.5) + Math.Abs((float)piece.Square.File - 3.5)));
                score += cofactor * (value + centrality);

                //if (piece.IsPawn)
                //{
                //    // the further back any pawn is, the better it is for white ...
                //    score += ((piece.Square.Rank - 1) * 100) / 5;
                //}
            }
        }

        cache[board.ZobristKey] = score;
        return score;
    }

    public int NegaMax(Board board, Timer timer, bool maximizeForWhite, int depth = 0)
    {
        // Debug.WriteLine($"Evaluating the board: {board.GetFenString()}");
        if ( depth == 0 || IsDrawnGame(board) || board.IsInCheckmate())
        {
            return (maximizeForWhite ? 1 : -1 ) * EvaluateForWhite(board, timer);
        }

        int bestScore = -softInfinity;
        int score = -softInfinity;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            score = -NegaMax(board, timer, !maximizeForWhite, depth - 1);
            board.UndoMove(move);

            //Debug.WriteLine($"{move.MovePieceType}:{move} > {score}");

            if (score > bestScore)
            {
                // Debug.WriteLine($"new Best from {bestScore} to {score}.");
                bestScore = score;
            }
        }

        return bestScore;
    }
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        Random rng = new(seed);
        // Move moveToPlay = allMoves[rng.Next(allMoves.Length)];

        int bestScore = int.MinValue;
        bool wePlayWhite = board.IsWhiteToMove;

        List<Move> bestMoves = new();


        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int eval = NegaMax(board, timer, wePlayWhite, this.depth);
            board.UndoMove(move);

            if ( eval > bestScore)
            {
                bestScore = eval;
                bestMoves.Clear();
                bestMoves.Add(move);
            } else if (eval == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        return bestMoves[rng.Next(bestMoves.Count)];
    }
}