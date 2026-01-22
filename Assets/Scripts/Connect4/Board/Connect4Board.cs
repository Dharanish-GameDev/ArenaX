public class Connect4Board : ConnectBoardBase
{
    public override int NumRows => Board4Utils.NUM_ROWS;
    public override int NumCols => Board4Utils.NUM_COLS;
    
    public Connect4Board()
    {
        Table = Board4Utils.GetDefaultBoard();
    }
    
    public Connect4Board(Tile[,] board)
    {
        Table = new Tile[NumRows, NumCols];
        for (int i = 0; i < NumRows; i++)
        for (int j = 0; j < NumCols; j++)
            Table[i, j] = board[i, j];
    }
}