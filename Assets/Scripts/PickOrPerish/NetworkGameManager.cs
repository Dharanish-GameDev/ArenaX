using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [SerializeField] private PanelManager _panelManager;
    public PanelManager PanelManager => _panelManager;

    [Header("Round Start")]
    [SerializeField] private int minPlayersToStart = 2;   // ✅ start timer when >= 2 active players

    [Range(1, 10)]
    [SerializeField] private int countDownSeconds = 3;

    private NetworkVariable<int> currentRound = new NetworkVariable<int>(1);

    private List<NetworkPlayer> players = new List<NetworkPlayer>();

    public NetworkVariable<int> timer = new NetworkVariable<int>(25);

    public event Action OnTimerEnd;

    [SerializeField] private int DefaultWaitTime = 25;
    private Coroutine countDownCoroutine;

    private bool winnerFound = false;

    [SerializeField] private List<PoP_PlayerDataSO> playerDatas = new List<PoP_PlayerDataSO>();

    public static int defaultLives = 3;

    private bool roundStarted = false; // ✅ to prevent starting multiple times

    private void Awake() => Instance = this;

    #region Timer
    public void StartCountDown()
    {
        if (!IsServer) return;
        if (winnerFound) return;

        // ✅ Don't run timer if game isn't started yet
        if (!roundStarted) return;

        // ✅ Winner check before starting timer
        if (TryDeclareWinnerIfOneLeft()) return;

        if (countDownCoroutine != null)
        {
            StopCoroutine(countDownCoroutine);
            countDownCoroutine = null;
        }

        EnableTimerTextClientRPC();
        countDownCoroutine = StartCoroutine(CountDownCoroutine());
    }
    // called by NetworkPlayer on server whenever a player becomes eliminated
    public void NotifyPlayerEliminated()
    {
        if (!IsServer) return;

        // this should stop countdown and announce winner if one active remains
        // (use the TryDeclareWinnerIfOneLeft() method from the patched GameManager I gave you)
        TryDeclareWinnerIfOneLeft();
    }


    private IEnumerator CountDownCoroutine()
    {
        timer.Value = DefaultWaitTime;

        while (timer.Value > 0)
        {
            // ✅ stop immediately if winner is found mid-countdown
            if (winnerFound || TryDeclareWinnerIfOneLeft())
                yield break;

            yield return new WaitForSeconds(1f);
            timer.Value--;
        }

        FireTimerCountdownEndClientRPC();
    }

    [ClientRpc]
    private void FireTimerCountdownEndClientRPC()
    {
        Debug.Log($"Countdown finished in {countDownSeconds} Secs");
        OnTimerEnd?.Invoke();
    }

    [ClientRpc]
    private void EnableTimerTextClientRPC() => _panelManager.EnableTimerText();
    #endregion

    #region Register
    public void RegisterTimerValueChanged(NetworkVariable<int>.OnValueChangedDelegate onValueChanged)
    {
        timer.OnValueChanged -= onValueChanged;
        timer.OnValueChanged += onValueChanged;
    }

    public void RegisterOnCurrentRoundValueChanged(NetworkVariable<int>.OnValueChangedDelegate onValueChanged)
    {
        currentRound.OnValueChanged -= onValueChanged;
        currentRound.OnValueChanged += onValueChanged;
    }

    public void RegisterMeToTheMatch(NetworkPlayer player)
    {
        if (!IsServer) return;

        if (!players.Contains(player)) players.Add(player);

        // ✅ Start automatically when enough players are active
        TryAutoStartRound();
    }
    #endregion

    #region Auto Start / Winner Stop

    private void TryAutoStartRound()
    {
        if (!IsServer) return;
        if (winnerFound) return;

        var activePlayers = GetActivePlayers();

        // start only when we have enough players
        if (!roundStarted && activePlayers.Count >= minPlayersToStart)
        {
            roundStarted = true;

            // optional: reset round/timer if you want
            // currentRound.Value = 1;
            // timer.Value = DefaultWaitTime;

            StartCountDown();
        }
    }

    private bool TryDeclareWinnerIfOneLeft()
    {
        if (!IsServer) return false;
        if (winnerFound) return true;

        var active = GetActivePlayers();

        if (active.Count == 1)
        {
            int winnerId = active[0].GetPlayerID();
            winnerFound = true;

            StopRoundTimers();
            AnnounceWinnerClientRPC(winnerId);
            return true;
        }

        return false;
    }

    private void StopRoundTimers()
    {
        // stop countdown coroutine
        if (countDownCoroutine != null)
        {
            StopCoroutine(countDownCoroutine);
            countDownCoroutine = null;
        }

        // cancel any delayed round advance
        CancelInvoke(nameof(AdvanceRound));
    }

    #endregion

    #region Submissions
    private Dictionary<int, int> submittedNumbersDict = new Dictionary<int, int>();

    public void SubmittedNumber(int playerId, int currentNumber)
    {
        if (!IsServer) return;
        if (winnerFound) return;

        if (currentNumber < 0) return;

        if (!submittedNumbersDict.ContainsKey(playerId))
            submittedNumbersDict.Add(playerId, currentNumber);

        var activePlayers = GetActivePlayers();

        if (activePlayers.Count > 0 &&
            activePlayers.Count == submittedNumbersDict.Count)
        {
            Debug.Log($"Round Finishing in {countDownSeconds} Secs");

            StopRoundTimers();

            FireTimerCountdownEndClientRPC();
            StartCountDownClientRPC(countDownSeconds);

            Invoke(nameof(AdvanceRound), countDownSeconds);
        }
    }
    #endregion

    #region RoundFlow

    private void AdvanceRound()
    {
        if (!IsServer || winnerFound) return;

        // ✅ if only one is left, stop and announce
        if (TryDeclareWinnerIfOneLeft()) return;

        currentRound.Value += 1;

        Dictionary<int, int> playerScores = GetPlayerScores();

        RoundResult result = RuleManager.Instance.ProcessSubmissions(submittedNumbersDict);

        if (result != null)
        {
            int[] winners = GetRoundWinners(result, submittedNumbersDict).ToArray();

            if (winners.Length == 0)
                Debug.Log("❌ No Winner this round (duplicate rule triggered)");
            else
                Debug.Log("🏆 Winners: " + string.Join(", ", winners));

            UpdateAfterRoundUIClientRPC(winners, result.calculatedTarget);

            var pointChanges = result.GetPointChanges();
            foreach (var change in pointChanges)
                playerScores[change.Key] += change.Value;

            UpdatePointChanges(playerScores);

            // ✅ winner check after scoring
            if (!TryDeclareWinnerIfOneLeft())
                StartCountDown();
        }

        submittedNumbersDict.Clear();
    }

    private List<int> GetRoundWinners(RoundResult result, Dictionary<int, int> submissions)
    {
        List<int> winners = new List<int>();
        float minDiff = submissions.Min(p => Mathf.Abs(p.Value - result.calculatedTarget));

        foreach (var p in submissions)
        {
            if (Mathf.Approximately(Mathf.Abs(p.Value - result.calculatedTarget), minDiff))
            {
                if (!result.penalizedPlayers.Contains(p.Key))
                    winners.Add(p.Key);
            }
        }

        return winners;
    }
    #endregion

    #region Scoring
    private void UpdatePointChanges(Dictionary<int, int> pointChanges)
    {
        foreach (var change in pointChanges)
        {
            NetworkPlayer player = players.FirstOrDefault(p => p.GetPlayerID() == change.Key);
            if (player != null)
                player.UpdateCurrentScore(change.Value);
        }

        // ✅ winner check after point changes
        TryDeclareWinnerIfOneLeft();
    }
    #endregion

    #region Helpers
    private Dictionary<int, int> GetPlayerScores()
    {
        Dictionary<int, int> scores = new Dictionary<int, int>();
        foreach (var p in GetActivePlayers())
            scores[p.GetPlayerID()] = p.GetCurrentScore();
        return scores;
    }

    private List<NetworkPlayer> GetActivePlayers()
    {
        return players.Where(p => p != null && p.GetCurrentPlayerState() == PlayerState.Active).ToList();
    }
    #endregion

    #region RPCs
    [ClientRpc]
    private void StartCountDownClientRPC(int countDownSeconds)
        => _panelManager.StartCountdown(countDownSeconds);

    [ClientRpc]
    private void UpdateAfterRoundUIClientRPC(int[] winnerIds, float targetNumber)
        => _panelManager.UpdateAfterRoundUI(winnerIds, targetNumber);

    [ClientRpc]
    private void AnnounceWinnerClientRPC(int winnerId)
        => _panelManager.AnnounceWinner(winnerId);
    #endregion

    #region DataHandlers
    public PoP_PlayerDataSO GetPlayerDataForId(int playerId)
    {
        return playerDatas.FirstOrDefault(p => p.playerID == playerId);
    }
    #endregion

#if UNITY_EDITOR
    [Range(1, 10)]
    [SerializeField] private int roundDebug = 1;

    [ContextMenu("DebugCurrentRound")]
    private void DebugCurrentRound()
    {
        if (IsServer) currentRound.Value = roundDebug;
    }
#endif
}
