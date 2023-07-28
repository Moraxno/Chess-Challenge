using ChessChallenge.API;
using System;
using System.Collections.Generic;
using static System.Formats.Asn1.AsnWriter;

public class MyBot : IChessBot
{
    int depth = 0;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int softInfinity = 1_000_000;

    Dictionary<ulong, int> cache = new();
    Dictionary<string, int> slowCache = new();

    public MyBot(int depth = 0)
    {
        this.depth = depth;
    }

    public int EvaluateForWhite(Board board, Timer timer)
    {
        int score = 0;

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
            score += board.IsWhiteToMove ? -100 : 100;
        }

        // minimally punish shuffling
        score -= board.FiftyMoveCounter;

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
        slowCache[board.GetFenString()] = score;
        return score;
    }

    public int NegaMax(Board board, Timer timer, bool maximizeForWhite, int depth = 0)
    {
        
        if ( depth == 0 )
        {
            return (maximizeForWhite ? 1 : -1 ) * EvaluateForWhite(board, timer);
        }

        int value = int.MinValue;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            value = Math.Max(value, -NegaMax(board, timer, !maximizeForWhite, depth - 1));
            board.UndoMove(move);
        }

        return value;
    }
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        Random rng = new();
        // Move moveToPlay = allMoves[rng.Next(allMoves.Length)];

        int bestScore = int.MinValue;
        bool wePlayWhite = board.IsWhiteToMove;

        List<Move> bestMoves = new();


        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int eval = NegaMax(board, timer, wePlayWhite, this.depth);
            board.UndoMove(move);

            Console.WriteLine($"{move} {eval}");

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