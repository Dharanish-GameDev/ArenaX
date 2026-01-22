/// <summary>
/// Interface for AIs
/// </summary>
public interface AI
{
    /// <summary>
    /// Function which must be implemented by the AI
    /// </summary>
    /// <param name="currentPlayer"></param>
    /// <param name="connect4Board"></param>
    /// <returns></returns>
    int GetBestMove(Player currentPlayer, Connect4Board connect4Board);
}
