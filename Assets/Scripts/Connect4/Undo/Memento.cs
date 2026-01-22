using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Class for retaining the game state
/// </summary>
public class Memento
{
    public Memento(Player currentPlayer, Connect4Board connect4Board, GameObject piece)
    {
        CurrentPlayer = currentPlayer;
      
        Piece = piece;
        Connect4Board = new Connect4Board(connect4Board.Table);
    }

    #region Properties
    public Player CurrentPlayer { get; }
    public Connect4Board Connect4Board { get; }

    public GameObject Piece { get; }
    #endregion
}

