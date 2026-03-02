using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Newtonsoft.Json;
using Arena.API.Models;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [SerializeField] private PanelManager _panelManager;
    public PanelManager PanelManager => _panelManager;

    [Range(1, 10)]
    [SerializeField] private int countDownSeconds = 3;

    private NetworkVariable<int> currentRound = new NetworkVariable<int>(1);
    public NetworkVariable<int> timer = new NetworkVariable<int>(25);

    private List<NetworkPlayer> players = new List<NetworkPlayer>();

    public event Action OnTimerEnd;

    [SerializeField] private int DefaultWaitTime = 25;
    private Coroutine countDownCoroutine;

    private bool winnerFound = false;
    public static int defaultLives = 3;

    // 🔥 Backend Tracking
    private float matchStartTime;
    private bool resultSentToBackend = false;
    private List<string> finalWinnerUIDs = new List<string>();
    private List<string> finalLoserUIDs = new List<string>();

    #region REGISTER VALUE CHANGE EVENTS

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

    #endregion

    private void Awake() => Instance = this;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong obj) 
    {
        // Handle player disconnection
        var disconnectedPlayer = players.FirstOrDefault(p => (ulong)p.GetPlayerID() == obj);
        if (disconnectedPlayer != null && IsServer)
        {
            Debug.Log($"Player {obj} disconnected");
            disconnectedPlayer.SetPlayerState(PlayerState.Disconnected);
            CheckForGameOver();
        }
    }

    private void OnClientConnected(ulong obj)
    {
        if (!IsServer) return;

        int currentPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (currentPlayers > LobbyManager.Instance.MaxPlayers)
        {
            NetworkManager.Singleton.DisconnectClient(obj);
        }

        if (currentPlayers == LobbyManager.Instance.MaxPlayers)
        {
            DisableWaitingUIClientRPC();
            StartMatch();
        }
    }

    #region MATCH START

    private void StartMatch()
    {
        matchStartTime = Time.time;
        StartCountDown();
    }

    #endregion

    #region TIMER

    public void StartCountDown()
    {
        if (winnerFound) return;

        if (countDownCoroutine != null)
            StopCoroutine(countDownCoroutine);

        EnableTimerTextClientRPC();
        countDownCoroutine = StartCoroutine(CountDownCoroutine());
    }

    private IEnumerator CountDownCoroutine()
    {
        timer.Value = DefaultWaitTime;

        while (timer.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            timer.Value--;
        }

        FireTimerCountdownEndClientRPC();
    }

    [ClientRpc]
    private void FireTimerCountdownEndClientRPC()
    {
        OnTimerEnd?.Invoke();
    }

    [ClientRpc]
    private void EnableTimerTextClientRPC()
        => _panelManager.EnableTimerText();

    #endregion

    #region REGISTER PLAYERS

    public void RegisterMeToTheMatch(NetworkPlayer player)
    {
        if (!players.Contains(player))
            players.Add(player);
    }

    #endregion

    #region SUBMISSIONS

    private Dictionary<int, int> submittedNumbersDict = new Dictionary<int, int>();

    public void SubmittedNumber(int playerId, int currentNumber)
    {
        if (currentNumber < 0) return;

        if (!submittedNumbersDict.ContainsKey(playerId))
            submittedNumbersDict.Add(playerId, currentNumber);

        var activePlayers = GetActivePlayers();

        if (IsServer &&
            activePlayers.Count > 0 &&
            activePlayers.Count == submittedNumbersDict.Count)
        {
            if (countDownCoroutine != null)
                StopCoroutine(countDownCoroutine);

            FireTimerCountdownEndClientRPC();
            StartCountDownClientRPC(countDownSeconds);

            Invoke(nameof(AdvanceRound), countDownSeconds);
        }
    }

    #endregion

    #region ROUND FLOW

    private void AdvanceRound()
    {
        if (!IsServer || winnerFound) return;

        currentRound.Value += 1;

        Dictionary<int, int> playerScores = GetPlayerScores();

        RoundResult result = RuleManager.Instance.ProcessSubmissions(submittedNumbersDict);

        if (result != null)
        {
            int[] winners = GetRoundWinners(result, submittedNumbersDict).ToArray();
            UpdateAfterRoundUIClientRPC(winners, result.calculatedTarget);

            var pointChanges = result.GetPointChanges();
            foreach (var change in pointChanges)
                playerScores[change.Key] += change.Value;

            UpdatePointChanges(playerScores);
            
            // Check for eliminated players before starting next round
            CheckForEliminatedPlayers();
            
            // Only start next round if game hasn't ended
            if (!winnerFound)
                StartCountDown();
        }

        submittedNumbersDict.Clear();
    }

    private List<int> GetRoundWinners(RoundResult result, Dictionary<int, int> submissions)
    {
        List<int> winners = new List<int>();

        if (submissions.Count == 0)
            return winners;

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

    #region SCORING & GAME OVER

    private void UpdatePointChanges(Dictionary<int, int> pointChanges)
    {
        foreach (var change in pointChanges)
        {
            NetworkPlayer player = players.FirstOrDefault(p => p.GetPlayerID() == change.Key);
            if (player != null)
            {
                player.UpdateCurrentScore(change.Value);
            }
        }
        
        // Check for game over after score updates
        CheckForGameOver();
    }

    private void CheckForEliminatedPlayers()
    {
        foreach (var player in players)
        {
            // If player score is 0 or less, eliminate them
            if (player.GetCurrentScore() <= 0 && player.GetCurrentPlayerState() == PlayerState.Active)
            {
                Debug.Log($"Player {player.GetPlayerID()} eliminated! Score: {player.GetCurrentScore()}");
                player.SetPlayerState(PlayerState.Eliminated);
            }
        }
    }

    private void CheckForGameOver()
    {
        if (winnerFound) return;
        
        var activePlayers = GetActivePlayers();
        Debug.Log("Active Players: " + activePlayers.Count);

        if (activePlayers.Count == 1)
        {
            // One player left - they are the winner
            winnerFound = true;
            NetworkPlayer winner = activePlayers[0];
            
            Debug.Log($"🏆 Match Over. Winner: Player {winner.GetPlayerID()} with score {winner.GetCurrentScore()}");
            
            AnnounceWinnerClientRPC(winner.GetPlayerID());
            
            StopAllCoroutines();
            
            // Build winner & loser lists
            finalWinnerUIDs.Clear();
            finalLoserUIDs.Clear();
            
            finalWinnerUIDs.Add(winner.GetBackendUserId());
            
            foreach (var p in players)
            {
                if (p != winner && p.GetCurrentPlayerState() != PlayerState.Disconnected)
                {
                    finalLoserUIDs.Add(p.GetBackendUserId());
                    Debug.Log($"Loser: Player {p.GetPlayerID()} - {p.GetBackendUserId()}");
                }
            }
            
            SendMatchResultToBackend();
        }
        else if (activePlayers.Count == 0)
        {
            // No players left? This should rarely happen, but handle it
            winnerFound = true;
            Debug.LogError("No active players left! Something went wrong.");
        }
    }

    #endregion

    #region BACKEND POST

    private void SendMatchResultToBackend()
    {
        if (!IsServer || resultSentToBackend)
            return;

        resultSentToBackend = true;
        
        RoomManager roomManager = FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include);
        if (roomManager == null) 
        {
            Debug.LogError("[MatchResult] RoomManager not found!");
            return;
        }
        
        int durationSec = Mathf.RoundToInt(Time.time - matchStartTime);
        string roomId = roomManager.CurrentRoomId;
        int coinAmount = roomManager.CurrentRoomData != null ? roomManager.CurrentRoomData.coinAmount : 0;

        MatchResultRequest request = new MatchResultRequest
        {
            Game = "pick_or_perish",
            RoomId = roomId,
            CoinAmount = coinAmount,
            Winners = new List<PlayerResult>(),
            Losers = new List<PlayerResult>()
        };

        foreach (var uid in finalWinnerUIDs)
        {
            request.Winners.Add(new PlayerResult
            {
                UserId = uid,
                PlacementId = 1,
                Kills = 0,
                DurationSec = durationSec
            });
        }

        foreach (var uid in finalLoserUIDs)
        {
            request.Losers.Add(new PlayerResult
            {
                UserId = uid,
                PlacementId = 2,
                Kills = 0,
                DurationSec = durationSec
            });
        }

        string json = JsonConvert.SerializeObject(request, Formatting.Indented);

        Debug.Log("🔥 Sending Match Result:\n" + json);

        ApiManager.Instance.SendRequest<SubmitGameResultResponse>(
            ApiEndPoints.Games.SubmitResult,
            RequestMethod.POST,
            (res) => Debug.Log("✅ Match result sent successfully."),
            (err) => 
            {
                Debug.LogError("❌ Match result failed: " + err);
                resultSentToBackend = false; // Allow retry
            },
            json
        );
    }

    #endregion

    #region HELPERS

    private Dictionary<int, int> GetPlayerScores()
    {
        Dictionary<int, int> scores = new Dictionary<int, int>();
        foreach (var p in players)
            scores[p.GetPlayerID()] = p.GetCurrentScore();
        return scores;
    }

    private List<NetworkPlayer> GetActivePlayers()
    {
        return players.Where(p => p.GetCurrentPlayerState() == PlayerState.Active).ToList();
    }

    #endregion

    #region RPCs

    [ClientRpc]
    private void StartCountDownClientRPC(int seconds)
        => _panelManager.StartCountdown(seconds);

    [ClientRpc]
    private void UpdateAfterRoundUIClientRPC(int[] winnerIds, float target)
        => _panelManager.UpdateAfterRoundUI(winnerIds, target);

    [ClientRpc]
    private void AnnounceWinnerClientRPC(int winnerId)
        => _panelManager.AnnounceWinner(winnerId);

    [ClientRpc]
    private void DisableWaitingUIClientRPC()
        => _panelManager.DisableWaitingScreen();

    #endregion
}