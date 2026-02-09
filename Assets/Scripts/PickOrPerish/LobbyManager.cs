using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Networking")]
    [SerializeField] private UnityTransport transport;
    [SerializeField] private NetworkConnectionHandler networkConnectionHandler;

    private Lobby joinedLobby;
    private Lobby hostLobby;

    private const string START_GAME_KEY = "START_GAME";
    private const string ROOM_CODE_KEY = "ROOM_CODE";

    private bool isHost;
    private bool isGameStarted;
    private int maxPlayers;

    public Lobby CurrentLobby => joinedLobby;
    public bool IsHost => isHost;
    public int MaxPlayers => maxPlayers;

    private float heartbeatTimer;
    private const float HEARTBEAT_INTERVAL = 15f;

    private float lobbyPollTimer;
    private const float LOBBY_POLL_INTERVAL = 6f;

    private bool isPolling;
    private bool isJoining;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (isGameStarted) return;

        HandleHeartbeat();

        lobbyPollTimer -= Time.deltaTime;
        if (lobbyPollTimer <= 0f)
        {
            lobbyPollTimer = LOBBY_POLL_INTERVAL;
            PollLobby();
        }
    }

    // ===================== PUBLIC API =====================

    public void CreateLobbyFromRoom(string roomCode, int maxPlayers)
    {
        this.maxPlayers = maxPlayers;
        CreateLobby(roomCode);
    }

    public void JoinLobbyFromRoom(string roomCode)
    {
        JoinLobby(roomCode);
    }

    // ===================== CREATE LOBBY =====================

    private async void CreateLobby(string roomCode)
    {
        try
        {
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { ROOM_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Public, roomCode) },
                    { START_GAME_KEY, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };

            hostLobby = await Lobbies.Instance.CreateLobbyAsync(roomCode, maxPlayers, options);
            joinedLobby = hostLobby;
            isHost = true;

            Debug.Log($"[Lobby] Created Lobby | Room: {roomCode}");
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Create Failed: " + e);
        }
    }

    // ===================== JOIN LOBBY BY QUERY =====================

    private async void JoinLobby(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return;
        if (isJoining) return;
        isJoining = true;

        try
        {
            var options = new QueryLobbiesOptions
            {
                Count = 25
            };

            var response = await Lobbies.Instance.QueryLobbiesAsync(options);

            Lobby match = null;

            foreach (var lobby in response.Results)
            {
                if (lobby.Data != null &&
                    lobby.Data.ContainsKey(ROOM_CODE_KEY) &&
                    lobby.Data[ROOM_CODE_KEY].Value == roomCode)
                {
                    match = lobby;
                    break;
                }
            }

            if (match == null)
            {
                Debug.LogError("[Lobby] No lobby found with room code: " + roomCode);
                return;
            }

            joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(match.Id);
            isHost = joinedLobby.HostId == AuthenticationService.Instance.PlayerId;

            Debug.Log($"[Lobby] Joined Lobby | Room: {roomCode}");
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Join Failed: " + e);
        }
        finally
        {
            isJoining = false;
        }
    }


    // ===================== START GAME =====================

    public async void StartGame()
    {
        if (!isHost || joinedLobby == null || isGameStarted) return;

        try
        {
            string relayCode = await CreateRelay();

            await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { START_GAME_KEY, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

            Debug.Log("[Lobby] Relay Code Published: " + relayCode);
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] StartGame Failed: " + e);
        }
    }

    private async Task<string> CreateRelay()
    {
        Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxPlayers);
        string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));
        networkConnectionHandler.StartAsHost();

        isGameStarted = true;
        return joinCode;
    }

    private async void JoinRelay(string joinCode)
    {
        if (isGameStarted) return;

        try
        {
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));
            networkConnectionHandler.StartAsClient();

            isGameStarted = true;
            Debug.Log("[Relay] Client Connected");
        }
        catch (Exception e)
        {
            Debug.LogError("[Relay] Join Failed: " + e);
        }
    }

    // ===================== POLLING =====================

    private async void PollLobby()
    {
        if (joinedLobby == null || isPolling || isGameStarted) return;

        isPolling = true;

        try
        {
            joinedLobby = await Lobbies.Instance.GetLobbyAsync(joinedLobby.Id);

            if (!isHost &&
                joinedLobby.Data != null &&
                joinedLobby.Data.ContainsKey(START_GAME_KEY) &&
                joinedLobby.Data[START_GAME_KEY].Value != "0")
            {
                JoinRelay(joinedLobby.Data[START_GAME_KEY].Value);
                joinedLobby = null;
            }
        }
        catch (LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.RateLimited)
                Debug.LogWarning("[Lobby] Poll rate limited");
            else
                Debug.LogError("[Lobby] Poll Failed: " + e);
        }
        finally
        {
            isPolling = false;
        }
    }

    // ===================== HEARTBEAT =====================

    private async void HandleHeartbeat()
    {
        if (!isHost || hostLobby == null || isGameStarted) return;

        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = HEARTBEAT_INTERVAL;
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    }
}
