using System.Collections.Generic;
using UnityEngine;

public class Connect4Game : IConnectGame
{
    public int WinCount => 4;
    
    public IConnectBoard CreateBoard() => new Connect4Board();
    
    public bool IsGameFinished(IConnectBoard board)
    {
        return Connect4Utils.Finished(board as Connect4Board);
    }
    
    public double EvaluateBoard(IConnectBoard board, PlayerAlliance alliance)
    {
        if (board is Connect4Board connect4Board)
        {
            return Board4Utils.EvaluateBoard(connect4Board, alliance);
        }
        return 0;
    }

    public List<Vector2Int> GetWinningCells(IConnectBoard board)
    {
        var winCells = new List<Vector2Int>();

        if (board is Connect4Board connect4Board && Connect4Utils.Finished(connect4Board))
        {
            try
            {
                var coords = Connect4Utils.GetEndGameCoordinates(connect4Board);

                int dRow = coords.EndRow - coords.StartRow;
                int dCol = coords.EndColumn - coords.StartColumn;

                int stepRow = dRow == 0 ? 0 : (dRow > 0 ? 1 : -1);
                int stepCol = dCol == 0 ? 0 : (dCol > 0 ? 1 : -1);

                for (int i = 0; i < WinCount; i++)
                {
                    int row = coords.StartRow + stepRow * i;
                    int col = coords.StartColumn + stepCol * i;
                    winCells.Add(new Vector2Int(row, col));
                }
            }
            catch (GameNotFinishedException)
            {
                // Game not actually finished
            }
        }

        return winCells;
    }

}