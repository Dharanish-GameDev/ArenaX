public class Connect5Board : ConnectBoardBase
{
    public override int NumRows => Board5Utils.NUM_ROWS;
    public override int NumCols => Board5Utils.NUM_COLS;
    
    public Connect5Board()
    {
        Table = Board5Utils.GetDefaultBoard();
    }
    
    public Connect5Board(Tile[,] board)
    {
        Table = new Tile[NumRows, NumCols];
        for (int i = 0; i < NumRows; i++)
        for (int j = 0; j < NumCols; j++)
            Table[i, j] = board[i, j];
    }
}