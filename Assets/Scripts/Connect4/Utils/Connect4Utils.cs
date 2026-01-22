using System;
/// <summary>
/// Class for utilitaries such as game over
/// </summary>
class Connect4Utils
{

    //constants
    public static double INF =  double.PositiveInfinity;
    public static double NEG_INF = double.NegativeInfinity;


    /// <summary>
    /// Function which checks if the game is finished
    /// </summary>
    /// <param name="connect4Board">board to be checked</param>
    /// <returns></returns>
    public static bool Finished(Connect4Board connect4Board)
    {
        return FinishedVertically(connect4Board) || FinishedHorizontally(connect4Board) || FinishedDiagonal(connect4Board);
    }

    /// <summary>
    /// Function to check if the game is done on a vertical line
    /// </summary>
    /// <param name="connect4Board"></param>
    /// <returns></returns>
    private static bool FinishedVertically(Connect4Board connect4Board)
    {
        var table = connect4Board.Table;
        for(int col = 0; col < Board4Utils.NUM_COLS; col++)
        {
            for(int row = 0; row < Board4Utils.NUM_ROWS - 3; row++)
            {
                if(table[row, col] == table[row + 1, col] && table[row + 1, col] == table[row + 2, col] && table[row + 2, col] == table[row + 3, col] && table[row, col] != Tile.EMPTY)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Function to check if the game is done on a horizontal line
    /// </summary>
    /// <param name="connect4Board"></param>
    /// <returns></returns>
    private static bool FinishedHorizontally(Connect4Board connect4Board)
    {
        var table = connect4Board.Table;
        for(int row = 0; row < Board4Utils.NUM_ROWS; row++)
        {
            for(int col = 0; col < Board4Utils.NUM_COLS - 3; col++)
            {
                if(table[row, col] == table[row, col + 1] && table[row, col + 1] == table[row, col + 2] && table[row, col + 2] == table[row, col + 3] && table[row, col] != Tile.EMPTY)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Function to check if te game is done on diagonal
    /// </summary>
    /// <param name="connect4Board"></param>
    /// <returns></returns>
    private static bool FinishedDiagonal(Connect4Board connect4Board)
    {
        var table = connect4Board.Table;
        for(int row = 0; row < Board4Utils.NUM_ROWS; row++)
        {
            for(int col = 0; col < Board4Utils.NUM_COLS; col++)
            {
                if(row + 3 < Board4Utils.NUM_ROWS && col + 3 < Board4Utils.NUM_COLS)
                {
                    if(table[row, col] == table[row + 1, col + 1] && table[row + 1, col + 1] == table[row + 2, col + 2] && table[row + 2, col + 2] == table[row + 3, col + 3] && table[row, col] != Tile.EMPTY)
                    {
                        return true;
                    }
                }
                if (row + 3 < Board4Utils.NUM_ROWS && col - 3 >= 0)
                {
                    if (table[row, col] == table[row + 1, col - 1] && table[row + 1, col - 1] == table[row + 2, col - 2] && table[row + 2, col - 2] == table[row + 3, col - 3] && table[row, col] != Tile.EMPTY)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public static EndGameCoordintaes GetEndGameCoordinates(Connect4Board connect4Board)
    {
        if (!Finished(connect4Board))
        {
            throw new GameNotFinishedException();
        }

        var table = connect4Board.Table;
        for (int col = 0; col < Board4Utils.NUM_COLS; col++)
        {
            for (int row = 0; row < Board4Utils.NUM_ROWS - 3; row++)
            {
                if (table[row, col] == table[row + 1, col] && table[row + 1, col] == table[row + 2, col] && table[row + 2, col] == table[row + 3, col] && table[row, col] != Tile.EMPTY)
                {
                    return new EndGameCoordintaes(col, col, row, row + 3);
                }
            }
        }

        for (int row = 0; row < Board4Utils.NUM_ROWS; row++)
        {
            for (int col = 0; col < Board4Utils.NUM_COLS - 3; col++)
            {
                if (table[row, col] == table[row, col + 1] && table[row, col + 1] == table[row, col + 2] && table[row, col + 2] == table[row, col + 3] && table[row, col] != Tile.EMPTY)
                {
                    return new EndGameCoordintaes(col, col + 3, row, row);
                }
            }
        }

        for (int row = 0; row < Board4Utils.NUM_ROWS; row++)
        {
            for (int col = 0; col < Board4Utils.NUM_COLS; col++)
            {
                if (row + 3 < Board4Utils.NUM_ROWS && col + 3 < Board4Utils.NUM_COLS)
                {
                    if (table[row, col] == table[row + 1, col + 1] && table[row + 1, col + 1] == table[row + 2, col + 2] && table[row + 2, col + 2] == table[row + 3, col + 3] && table[row, col] != Tile.EMPTY)
                    {
                        return new EndGameCoordintaes(col, col + 3, row, row + 3);
                    }
                }
                if (row + 3 < Board4Utils.NUM_ROWS && col - 3 >= 0)
                {
                    if (table[row, col] == table[row + 1, col - 1] && table[row + 1, col - 1] == table[row + 2, col - 2] && table[row + 2, col - 2] == table[row + 3, col - 3] && table[row, col] != Tile.EMPTY)
                    {
                        return new EndGameCoordintaes(col, col - 3, row, row + 3);
                    }
                }
            }
        }
        throw new GameNotFinishedException();
    }

}

