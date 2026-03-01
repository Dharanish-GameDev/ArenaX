using System;
using System.Collections.Generic;

[Serializable]
public class MatchStateSnapshot
{
    public int currentRound;
    public int timer;
    public bool winnerFound;

    public List<ulong> playerIds = new();
    public List<int> playerScores = new();
    public List<int> playerStates = new();
}