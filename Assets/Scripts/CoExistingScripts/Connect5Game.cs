using System.Collections.Generic;
using UnityEngine;

public class Connect5Game : IConnectGame
{
    public int WinCount => 5;
    
    public IConnectBoard CreateBoard() => new Connect5Board();
    
    public bool IsGameFinished(IConnectBoard board)
    {
        return Connect5Utils.Finished(board as Connect5Board);
    }
    
    public double EvaluateBoard(IConnectBoard board, PlayerAlliance alliance)
    {
        if (board is Connect5Board connect5Board)
        {
            return Board5Utils.EvaluateBoard(connect5Board, alliance);
        }
        return 0;
    }

    public List<Vector2Int> GetWinningCells(IConnectBoard board)
    {
        var winCells = new List<Vector2Int>();

        if (board is Connect5Board connect5Board && Connect5Utils.Finished(connect5Board))
        {
            try
            {
                var coords = Connect5Utils.GetEndGameCoordinates(connect5Board);

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