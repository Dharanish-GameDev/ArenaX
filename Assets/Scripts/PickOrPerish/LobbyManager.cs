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

    private const string PLAYER_UID_KEY = "UID";
    private const string PLAYER_NAME_KEY = "NAME";
    private const string PLAYER_AVATAR_KEY = "AVATAR";

    private bool isHost;
    private bool isGameStarted;
    private int maxPlayers;

    private float heartbeatTimer;
    private const float HEARTBEAT_INTERVAL = 15f;

    private float lobbyPollTimer;
    private const float LOBBY_POLL_INTERVAL = 3f;

    private bool isPolling;
    private bool isJoining;

    public Lobby CurrentLobby => joinedLobby;
    public bool IsHost => isHost;
    public int MaxPlayers => maxPlayers;

    public bool IsJoinedMatchmaking => isJoinMatchmaking;
    private bool isJoinMatchmaking = false;

    // ===================== PLAYER INFO =====================

    public struct LobbyPlayerInfo
    {
        public string uid;
        public string name;
        public string avatarUrl;
    }

    public List<LobbyPlayerInfo> GetJoinedPlayers()
    {
        var list = new List<LobbyPlayerInfo>();

        if (joinedLobby?.Players == null)
            return list;

        foreach (var player in joinedLobby.Players)
        {
            var data = player.Data;

            list.Add(new LobbyPlayerInfo
            {
                uid = data != null && data.ContainsKey(PLAYER_UID_KEY) ? data[PLAYER_UID_KEY].Value : "",
                name = data != null && data.ContainsKey(PLAYER_NAME_KEY) ? data[PLAYER_NAME_KEY].Value : "Unknown",
                avatarUrl = data != null && data.ContainsKey(PLAYER_AVATAR_KEY) ? data[PLAYER_AVATAR_KEY].Value : ""
            });
        }

        return list;
    }

    // ===================== UNITY =====================

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject);
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
        isJoinMatchmaking = false;
        CreateLobby(roomCode);
    }

    public void JoinLobbyFromRoom(string roomCode)
    {
        JoinLobby(roomCode);
        isJoinMatchmaking = false;
    }
    
    public void StartQuickMatch(int maxPlayers, Action callback = null)
    {
        this.maxPlayers = maxPlayers;
        QuickJoinOrCreate();
        isJoinMatchmaking = true;
    }


    // ===================== PLAYER DATA =====================

    private Dictionary<string, PlayerDataObject> GetLocalPlayerData()
    {
        var user = UnifiedAuthManager.Instance.GetCurrentUser();

        return new Dictionary<string, PlayerDataObject>
        {
            { PLAYER_UID_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.id) },
            { PLAYER_NAME_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.username) },
            { PLAYER_AVATAR_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.profilePictureUrl) }
        };
    }

    private async Task UpdateMyPlayerDataAndRefresh()
    {
        if (joinedLobby == null) return;

        await Lobbies.Instance.UpdatePlayerAsync(
            joinedLobby.Id,
            AuthenticationService.Instance.PlayerId,
            new UpdatePlayerOptions { Data = GetLocalPlayerData() });

        joinedLobby = await Lobbies.Instance.GetLobbyAsync(joinedLobby.Id);
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

            await UpdateMyPlayerDataAndRefresh();

            Debug.Log($"[Lobby] Created Lobby | Room: {roomCode}");
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Create Failed: " + e);
        }
    }

    // ===================== JOIN LOBBY =====================

    private async void JoinLobby(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode) || isJoining) return;
        isJoining = true;

        try
        {
            var response = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions { Count = 25 });

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

            await UpdateMyPlayerDataAndRefresh();

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
    
    private async void QuickJoinOrCreate()
    {
        if (isJoining) return;
        isJoining = true;

        try
        {
            joinedLobby = await Lobbies.Instance.QuickJoinLobbyAsync();
            isHost = joinedLobby.HostId == AuthenticationService.Instance.PlayerId;

            await UpdateMyPlayerDataAndRefresh();

            Debug.Log("[Matchmaking] Quick Joined Lobby");
        }
        catch (LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.NoOpenLobbies)
            {
                Debug.Log("[Matchmaking] No lobby found → Creating new lobby");

                CreateLobby("MM_" + UnityEngine.Random.Range(1000, 9999));
            }
            else
            {
                Debug.LogError("[Matchmaking] Failed: " + e);
            }
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
        isGameStarted = true;

        try
        {
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));
            networkConnectionHandler.StartAsClient();

            Debug.Log("[Relay] Client Connected");
        }
        catch (Exception e)
        {
            isGameStarted = false;
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
                joinedLobby.Data.TryGetValue(START_GAME_KEY, out var startData) &&
                startData.Value != "0")
            {
                Debug.Log("[Lobby] Game Start Detected → Joining Relay");
                JoinRelay(startData.Value);
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
