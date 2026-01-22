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
                // Convert coordinates to cells
                for (int i = 0; i < WinCount; i++)
                {
                    int row = coords.StartRow + (coords.EndRow > coords.StartRow ? i : -i);
                    int col = coords.StartColumn + (coords.EndColumn > coords.StartColumn ? i : -i);
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