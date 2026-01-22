using System;

/// <summary>
/// Class for utilitaries such as game over
/// </summary>
public class Connect5Utils
{
    //constants
    public static double INF = double.PositiveInfinity;
    public static double NEG_INF = double.NegativeInfinity;

    /// <summary>
    /// Function which checks if the game is finished
    /// </summary>
    /// <param name="connect5Board">board to be checked</param>
    /// <returns></returns>
    public static bool Finished(Connect5Board connect5Board)
    {
        return FinishedVertically(connect5Board) || FinishedHorizontally(connect5Board) || FinishedDiagonal(connect5Board);
    }

    /// <summary>
    /// Function to check if the game is done on a vertical line
    /// </summary>
    /// <param name="connect5Board"></param>
    /// <returns></returns>
    private static bool FinishedVertically(Connect5Board connect5Board)
    {
        var table = connect5Board.Table;
        for (int col = 0; col < Board5Utils.NUM_COLS; col++)
        {
            for (int row = 0; row < Board5Utils.NUM_ROWS - 4; row++) // Changed for 5-in-a-row
            {
                if (table[row, col] != Tile.EMPTY &&
                    table[row, col] == table[row + 1, col] &&
                    table[row + 1, col] == table[row + 2, col] &&
                    table[row + 2, col] == table[row + 3, col] &&
                    table[row + 3, col] == table[row + 4, col]) // Added 5th check
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
    /// <param name="connect5Board"></param>
    /// <returns></returns>
    private static bool FinishedHorizontally(Connect5Board connect5Board)
    {
        var table = connect5Board.Table;
        for (int row = 0; row < Board5Utils.NUM_ROWS; row++)
        {
            for (int col = 0; col < Board5Utils.NUM_COLS - 4; col++) // Changed for 5-in-a-row
            {
                if (table[row, col] != Tile.EMPTY &&
                    table[row, col] == table[row, col + 1] &&
                    table[row, col + 1] == table[row, col + 2] &&
                    table[row, col + 2] == table[row, col + 3] &&
                    table[row, col + 3] == table[row, col + 4]) // Added 5th check
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Function to check if the game is done on diagonal
    /// </summary>
    /// <param name="connect5Board"></param>
    /// <returns></returns>
    private static bool FinishedDiagonal(Connect5Board connect5Board)
    {
        var table = connect5Board.Table;
        for (int row = 0; row < Board5Utils.NUM_ROWS; row++)
        {
            for (int col = 0; col < Board5Utils.NUM_COLS; col++)
            {
                // Check diagonal down-right (\) - 5 in a row
                if (row + 4 < Board5Utils.NUM_ROWS && col + 4 < Board5Utils.NUM_COLS)
                {
                    if (table[row, col] != Tile.EMPTY &&
                        table[row, col] == table[row + 1, col + 1] &&
                        table[row + 1, col + 1] == table[row + 2, col + 2] &&
                        table[row + 2, col + 2] == table[row + 3, col + 3] &&
                        table[row + 3, col + 3] == table[row + 4, col + 4])
                    {
                        return true;
                    }
                }
                
                // Check diagonal down-left (/) - 5 in a row
                if (row + 4 < Board5Utils.NUM_ROWS && col - 4 >= 0)
                {
                    if (table[row, col] != Tile.EMPTY &&
                        table[row, col] == table[row + 1, col - 1] &&
                        table[row + 1, col - 1] == table[row + 2, col - 2] &&
                        table[row + 2, col - 2] == table[row + 3, col - 3] &&
                        table[row + 3, col - 3] == table[row + 4, col - 4])
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Get the coordinates of the winning line
    /// </summary>
    public static EndGameCoordintaes GetEndGameCoordinates(Connect5Board connect5Board)
    {
        if (!Finished(connect5Board))
        {
            throw new GameNotFinishedException();
        }

        var table = connect5Board.Table;
        
        // Check vertical wins
        for (int col = 0; col < Board5Utils.NUM_COLS; col++)
        {
            for (int row = 0; row < Board5Utils.NUM_ROWS - 4; row++)
            {
                if (table[row, col] != Tile.EMPTY &&
                    table[row, col] == table[row + 1, col] &&
                    table[row + 1, col] == table[row + 2, col] &&
                    table[row + 2, col] == table[row + 3, col] &&
                    table[row + 3, col] == table[row + 4, col])
                {
                    return new EndGameCoordintaes(col, col, row, row + 4);
                }
            }
        }

        // Check horizontal wins
        for (int row = 0; row < Board5Utils.NUM_ROWS; row++)
        {
            for (int col = 0; col < Board5Utils.NUM_COLS - 4; col++)
            {
                if (table[row, col] != Tile.EMPTY &&
                    table[row, col] == table[row, col + 1] &&
                    table[row, col + 1] == table[row, col + 2] &&
                    table[row, col + 2] == table[row, col + 3] &&
                    table[row, col + 3] == table[row, col + 4])
                {
                    return new EndGameCoordintaes(col, col + 4, row, row);
                }
            }
        }

        // Check diagonal wins
        for (int row = 0; row < Board5Utils.NUM_ROWS; row++)
        {
            for (int col = 0; col < Board5Utils.NUM_COLS; col++)
            {
                // Diagonal down-right
                if (row + 4 < Board5Utils.NUM_ROWS && col + 4 < Board5Utils.NUM_COLS)
                {
                    if (table[row, col] != Tile.EMPTY &&
                        table[row, col] == table[row + 1, col + 1] &&
                        table[row + 1, col + 1] == table[row + 2, col + 2] &&
                        table[row + 2, col + 2] == table[row + 3, col + 3] &&
                        table[row + 3, col + 3] == table[row + 4, col + 4])
                    {
                        return new EndGameCoordintaes(col, col + 4, row, row + 4);
                    }
                }
                
                // Diagonal down-left
                if (row + 4 < Board5Utils.NUM_ROWS && col - 4 >= 0)
                {
                    if (table[row, col] != Tile.EMPTY &&
                        table[row, col] == table[row + 1, col - 1] &&
                        table[row + 1, col - 1] == table[row + 2, col - 2] &&
                        table[row + 2, col - 2] == table[row + 3, col - 3] &&
                        table[row + 3, col - 3] == table[row + 4, col - 4])
                    {
                        return new EndGameCoordintaes(col, col - 4, row, row + 4);
                    }
                }
            }
        }
        
        throw new GameNotFinishedException();
    }
}
