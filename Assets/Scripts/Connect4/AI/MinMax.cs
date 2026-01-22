using System;
using UnityEngine;

public class MinMax: AI
{
    private int depth;

    public MinMax(int depth)
    {
        this.depth = depth;
    }

    public int GetBestMove(Player currentPlayer, Connect4Board connect4Board)
    {
        int bestMove = 0;
        double bestScore = Connect4Utils.NEG_INF;

        foreach (int move in connect4Board.GetValidMoves())
        {
            Connect4Board child = new Connect4Board(connect4Board.Table);
            child.SetPiece(move, currentPlayer.Alliance);
            double score = minmax(child, depth - 1, currentPlayer, currentPlayer);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private double minmax(Connect4Board node, int depth, Player maximizingPlayer, Player currentPlayer)
    {
        if(depth == 0)
        {
            return Board4Utils.EvaluateBoard(node, currentPlayer.Alliance);
        }

        if(maximizingPlayer.Alliance == currentPlayer.Alliance)
        {
            double value = Connect4Utils.NEG_INF;
            foreach(int move in node.GetValidMoves())
            {
                Connect4Board child = new Connect4Board(node.Table);
                child.SetPiece(move, currentPlayer.Alliance);
                Player newCurrentPlayer = new Player(PlayerType.COMPUTER, currentPlayer.Alliance == PlayerAlliance.RED ? PlayerAlliance.BLACK : PlayerAlliance.RED);
                value = Math.Max(value, minmax(child, depth - 1, maximizingPlayer, newCurrentPlayer));
            }
            return value;
        }
        else
        {
            double value = Connect4Utils.INF;
            foreach (int move in node.GetValidMoves())
            {
                Connect4Board child = new Connect4Board(node.Table);
                child.SetPiece(move, currentPlayer.Alliance);
                Player newCurrentPlayer = new Player(PlayerType.COMPUTER, currentPlayer.Alliance == PlayerAlliance.RED ? PlayerAlliance.BLACK : PlayerAlliance.RED);
                value = Math.Min(value, minmax(child, depth - 1, maximizingPlayer, newCurrentPlayer));
            }
            return value;
        }
    }
}
