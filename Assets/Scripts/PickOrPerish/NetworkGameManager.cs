using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [SerializeField] private PanelManager _panelManager;
    public PanelManager PanelManager => _panelManager;

    [Range(1, 10)]
    [SerializeField] private int countDownSeconds = 3;

    private NetworkVariable<int> currentRound = new NetworkVariable<int>(1);

    private readonly List<NetworkPlayer> players = new List<NetworkPlayer>();

    // ✅ 1 min 30 sec = 90
    public NetworkVariable<int> timer = new NetworkVariable<int>(90);

    public event Action OnTimerEnd;

    [SerializeField] private int DefaultWaitTime = 90;
    private Coroutine countDownCoroutine;

    private bool winnerFound = false;
    private bool gameOver = false;

    [SerializeField] private List<PoP_PlayerDataSO> playerDatas = new List<PoP_PlayerDataSO>();

    private bool lobbyTimerStarted = false;

    private void Awake() => Instance = this;

    // -------------------- Timer --------------------
    public void StartCountDown()
    {
        if (!IsServer) return;
        if (winnerFound || gameOver) return;

        // if last player left -> winner
        if (TryStopGameIfSingleActive()) return;

        StopTimerServer();

        EnableTimerTextClientRPC();
        countDownCoroutine = StartCoroutine(CountDownCoroutine());
    }

    private IEnumerator CountDownCoroutine()
    {
        timer.Value = DefaultWaitTime;

        while (timer.Value > 0)
        {
            if (winnerFound || gameOver) yield break;
            if (TryStopGameIfSingleActive()) yield break;

            yield return new WaitForSeconds(1f);
            timer.Value--;
        }

        // ✅ TIMER HIT 0 => SERVER MUST PROCESS TIMEOUT ROUND
        if (IsServer && !winnerFound && !gameOver)
        {
            HandleRoundTimeout_Server();
        }

        FireTimerCountdownEndClientRPC();
    }

    private void StopTimerServer()
    {
        if (!IsServer) return;

        if (countDownCoroutine != null)
        {
            StopCoroutine(countDownCoroutine);
            countDownCoroutine = null;
        }
    }

    [ClientRpc]
    private void FireTimerCountdownEndClientRPC()
    {
        if (gameOver) return;
        OnTimerEnd?.Invoke();
    }

    [ClientRpc]
    private void EnableTimerTextClientRPC()
    {
        if (_panelManager != null)
            _panelManager.EnableTimerText();
    }

    // -------------------- Register --------------------
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
        if (!players.Contains(player))
            players.Add(player);

        // ✅ Auto start timer as soon as lobby has 2 active players
        if (IsServer)
            TryStartLobbyTimer();
    }

    private void TryStartLobbyTimer()
    {
        if (!IsServer) return;
        if (lobbyTimerStarted) return;
        if (winnerFound || gameOver) return;

        var activePlayers = GetActivePlayers();

        if (activePlayers.Count == 1)
        {
            TryStopGameIfSingleActive();
            return;
        }

        if (activePlayers.Count >= 2)
        {
            lobbyTimerStarted = true;
            StartCountDown();
        }
    }

    // -------------------- Submissions --------------------
    private readonly Dictionary<int, int> submittedNumbersDict = new Dictionary<int, int>();

    public void SubmittedNumber(int playerId, int currentNumber)
    {
        if (gameOver || winnerFound) return;
        if (currentNumber < 0) return;

        if (!submittedNumbersDict.ContainsKey(playerId))
            submittedNumbersDict.Add(playerId, currentNumber);

        var activePlayers = GetActivePlayers();

        // ✅ If everyone submitted early, finish round early
        if (IsServer && activePlayers.Count > 0 && activePlayers.Count == submittedNumbersDict.Count)
        {
            StopTimerServer();

            StartCountDownClientRPC(countDownSeconds);
            StartCoroutine(FinishRoundAfterDelay_Server(countDownSeconds));
        }
    }

    private IEnumerator FinishRoundAfterDelay_Server(int delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        AdvanceRound();
    }

    // ✅ TIMEOUT path: penalize non-submitters then advance round
    private void HandleRoundTimeout_Server()
    {
        if (!IsServer || winnerFound || gameOver) return;

        var activePlayers = GetActivePlayers();
        if (activePlayers.Count <= 0) return;

        // ✅ Non-submit = lose 1 life
        foreach (var p in activePlayers)
        {
            int id = p.GetPlayerID();
            if (!submittedNumbersDict.ContainsKey(id))
            {
                int newLives = Mathf.Max(0, p.GetLives() - 1);
                p.SetLives(newLives);

                if (newLives <= 0)
                    p.SetPlayerState(PlayerState.Eliminated);
            }
        }

        // if only one remains -> winner
        if (TryStopGameIfSingleActive())
        {
            submittedNumbersDict.Clear();
            return;
        }

        // Finish round now
        AdvanceRound();
    }

    // -------------------- Round Flow --------------------
    private void AdvanceRound()
    {
        if (!IsServer || winnerFound || gameOver) return;

        if (TryStopGameIfSingleActive()) return;

        currentRound.Value += 1;

        Dictionary<int, int> playerScores = GetPlayerScores();

        RoundResult result = RuleManager.Instance.ProcessSubmissions(submittedNumbersDict);
        if (result != null)
        {
            int[] winners = GetRoundWinners(result, submittedNumbersDict).ToArray();

            UpdateAfterRoundUIClientRPC(winners, result.calculatedTarget);

            // apply point changes
            var pointChanges = result.GetPointChanges();
            foreach (var change in pointChanges)
            {
                if (!playerScores.ContainsKey(change.Key))
                    playerScores.Add(change.Key, 0);

                playerScores[change.Key] += change.Value;
            }

            UpdateFinalScores(playerScores);

            if (TryStopGameIfSingleActive()) return;

            // ✅ Start next round timer
            StartCountDown();
        }

        // clear submissions after round completes
        submittedNumbersDict.Clear();
    }

    private List<int> GetRoundWinners(RoundResult result, Dictionary<int, int> submissions)
    {
        List<int> winners = new List<int>();
        if (submissions == null || submissions.Count == 0) return winners;

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

    private void UpdateFinalScores(Dictionary<int, int> finalScores)
    {
        foreach (var kv in finalScores)
        {
            NetworkPlayer player = players.FirstOrDefault(p => p != null && p.GetPlayerID() == kv.Key);
            if (player != null)
                player.UpdateCurrentScore(kv.Value);
        }
    }

    // -------------------- Game Over --------------------
    private bool TryStopGameIfSingleActive()
    {
        if (!IsServer || gameOver || winnerFound) return false;

        var active = GetActivePlayers();
        if (active.Count == 1)
        {
            int winnerId = active[0].GetPlayerID();
            StopGameServer(winnerId);
            return true;
        }
        return false;
    }

    private void StopGameServer(int winnerId)
    {
        if (gameOver) return;

        winnerFound = true;
        gameOver = true;

        StopTimerServer();
        submittedNumbersDict.Clear();

        StopGameClientRPC(winnerId);
    }

    [ClientRpc]
    private void StopGameClientRPC(int winnerId)
    {
        // hide number input UI for everyone
        if (NumbersHandler.Instance != null)
            NumbersHandler.Instance.SetInputVisibleAndInteractable(false, resetSelection: true);

        // ✅ show winner panel only for winner, loser panel for others
        int localId = -1;
        if (NetworkPlayer.LocalInstance != null)
            localId = NetworkPlayer.LocalInstance.GetPlayerID();

        bool isWinner = (localId == winnerId);

        if (_panelManager != null)
            _panelManager.ShowWinnerLoserPanel(isWinner, winnerId);
    }

    // -------------------- Helpers --------------------
    private Dictionary<int, int> GetPlayerScores()
    {
        Dictionary<int, int> scores = new Dictionary<int, int>();
        foreach (var p in GetActivePlayers())
            scores[p.GetPlayerID()] = p.GetCurrentScore();
        return scores;
    }

    private List<NetworkPlayer> GetActivePlayers()
    {
        return players
            .Where(p => p != null && p.GetCurrentPlayerState() == PlayerState.Active)
            .ToList();
    }

    // -------------------- RPCs --------------------
    [ClientRpc]
    private void StartCountDownClientRPC(int seconds)
    {
        if (_panelManager != null)
            _panelManager.StartCountdown(seconds);
    }

    [ClientRpc]
    private void UpdateAfterRoundUIClientRPC(int[] winnerIds, float targetNumber)
    {
        if (_panelManager != null)
            _panelManager.UpdateAfterRoundUI(winnerIds, targetNumber);
    }

    // -------------------- Data --------------------
    public PoP_PlayerDataSO GetPlayerDataForId(int playerId)
    {
        return playerDatas.FirstOrDefault(p => p.playerID == playerId);
    }
}
