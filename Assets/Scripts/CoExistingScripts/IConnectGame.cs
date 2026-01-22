using System.Collections.Generic;
using UnityEngine;

public interface IConnectGame
{
    IConnectBoard CreateBoard();
    bool IsGameFinished(IConnectBoard board);
    double EvaluateBoard(IConnectBoard board, PlayerAlliance alliance);
    List<Vector2Int> GetWinningCells(IConnectBoard board);
    int WinCount { get; }
}