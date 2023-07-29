using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

/*
 * 1000 games vs Base EvilBot
 *      +678 =249 -73
 *      (7 timeouts)
 */

public class MyBot : IChessBot
{
    Random rng = new();

    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 100 };

    Board gameBoard;
    Timer turnTimer;
    bool botIsWhite;

    int maxDepth;
    int leafNodesEvaluated;
    Move moveToPlay;
    Move[] rootMoves;
    int[] rootScores;

    public Move Think(Board board, Timer timer)
    {
        gameBoard = board;
        turnTimer = timer;
        botIsWhite = board.IsWhiteToMove;

        rootMoves = board.GetLegalMoves();
        rootScores = new int[rootMoves.Length];
        moveToPlay = rootMoves[0];

        if (rootMoves.Length == 1) return moveToPlay;

        for (maxDepth = 1; maxDepth <= 50; maxDepth++)
        {
            if (ExceededTurnTime() || rootScores.Max() > 900) break;

			leafNodesEvaluated = 0;

			MiniMax(1, true, -1000, 1000);
            moveToPlay = SortMoves();
        }

        return moveToPlay;
    }

    int MiniMax(int ply, bool isBotTurn, int alpha,  int beta)
    {
        Move[] moves = ply == 1 ? rootMoves : gameBoard.GetLegalMoves();
        int bestEval = isBotTurn ? -1000 : 1000;

        if (moves.Length == 0 || ply > maxDepth)
            return EvaluateBoard(ply / 2);

        for (int index = 0; index < moves.Length; index++)
        {
            if (ExceededTurnTime()) break;

            gameBoard.MakeMove(moves[index]);
			int eval = MiniMax(ply + 1, !isBotTurn, alpha, beta);
			gameBoard.UndoMove(moves[index]);

            bestEval = isBotTurn ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
            alpha = isBotTurn ? Math.Max(bestEval, alpha) : alpha;
            beta = isBotTurn ? beta : Math.Min(bestEval, beta);

            if (ply == 1)
                rootScores[index] = eval;

            if (alpha > beta) break;
        }

        return bestEval;
    }

    int EvaluateBoard(int moveCount)
	{
		leafNodesEvaluated++;

        if (gameBoard.IsInCheckmate())
            return (botIsWhite == gameBoard.IsWhiteToMove ? moveCount - 1000 : 1000 - moveCount);

        if (gameBoard.IsDraw())
            return 0;

        int eval = 0;

        foreach (PieceList pieceList in gameBoard.GetAllPieceLists())
        {
            int pieceValue = pieceValues[(int)pieceList.TypeOfPieceInList];
            int pieceCount = pieceList.Count;
            int playerMultiplier = (botIsWhite == pieceList.IsWhitePieceList) ? 1 : -1;
            eval += (pieceValue * pieceCount * playerMultiplier);
        }

        return eval;
    }

    Move SortMoves()
    {
        Array.Sort(rootScores, rootMoves);
        rootMoves = rootMoves.Reverse().ToArray();
        rootScores = rootScores.Reverse().ToArray();

        List<Move> bestMoves = new();
        for (int index = 0; index < rootMoves.Length; index++)
            if (rootScores[index] == rootScores.Max()) bestMoves.Add(rootMoves[index]);

#if DEBUG
        if (maxDepth == 1) Console.WriteLine($"\n+-+-+-+-+-+-+-+-+-+-+- NEW TURN -+-+-+-+-+-+-+-+-+-+-+");

        Console.Write($"\nFinished Search At Depth {maxDepth} ({turnTimer.MillisecondsElapsedThisTurn:N0}ms) ({leafNodesEvaluated:N0} nodes)");
        for (int index = 0; index < rootMoves.Length; index++)
        {
            Console.Write($"\n\t{index + 1,2}. [{rootMoves[index].MovePieceType} {rootMoves[index].StartSquare.Name}-{rootMoves[index].TargetSquare.Name}]\t[{rootScores[index],4}]");
            if (rootMoves[index].IsCapture) Console.Write($"[Taking {rootMoves[index].CapturePieceType}]");
        }
        Console.WriteLine();
#endif

        return bestMoves[rng.Next(bestMoves.Count - 1)];
    }

    bool ExceededTurnTime()
    {
        return turnTimer.MillisecondsElapsedThisTurn > turnTimer.MillisecondsRemaining / 40;
    }
}