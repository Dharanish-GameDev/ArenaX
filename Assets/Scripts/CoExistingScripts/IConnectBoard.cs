using System.Collections.Generic;

public interface IConnectBoard
{
    Tile[,] Table { get; set; }
    int NumRows { get; }
    int NumCols { get; }
    void SetPiece(int column, PlayerAlliance playerAlliance);
    List<int> GetValidMoves();
}

public abstract class ConnectBoardBase : IConnectBoard
{
    public Tile[,] Table { get; set; }
    public abstract int NumRows { get; }
    public abstract int NumCols { get; }
    
    public virtual void SetPiece(int column, PlayerAlliance playerAlliance)
    {
        if (column < 0 || column >= NumCols)
            throw new InvalidColumnException();
            
        int availableRow = NumRows - 1;
        while (availableRow >= 0 && Table[availableRow, column] != Tile.EMPTY)
            availableRow--;
            
        if (availableRow < 0)
            throw new ColumnOccupiedException($"Column {column} is occupied");
            
        Table[availableRow, column] = playerAlliance == PlayerAlliance.RED ? Tile.RED : Tile.BLACK;
    }
    
    public virtual List<int> GetValidMoves()
    {
        List<int> validMoves = new List<int>();
        for (int i = 0; i < NumCols; i++)
            if (Table[0, i] == Tile.EMPTY)
                validMoves.Add(i);
        return validMoves;
    }
}