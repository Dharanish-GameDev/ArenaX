public class EndGameCoordintaes
{
    public int StartColumn { get; set; }
    public int EndColumn { get; set; }
    public int StartRow { get; set; }
    public int EndRow { get; set; }

    public EndGameCoordintaes(int startColumn, int endColumn, int startRow, int endRow)
    {
        StartColumn = startColumn;
        EndColumn = endColumn;
        StartRow = startRow;
        EndRow = endRow;
    }
}