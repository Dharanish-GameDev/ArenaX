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
    
    [Header("References")]
    [SerializeField] private RoomManager roomManager;

    private Lobby joinedLobby;
    private Lobby hostLobby;

    private const string START_GAME_KEY = "START";
    private const string ROOM_CODE_KEY = "CODE";
    private const string LOBBY_TYPE_KEY = "TYPE";
    private const string MAX_PLAYERS_KEY = "MAX";

    private const string PLAYER_UID_KEY = "UID";
    private const string PLAYER_NAME_KEY = "NAME";
    private const string PLAYER_AVATAR_KEY = "AVATAR";

    // Lobby types
    private const string LOBBY_TYPE_QUICKMATCH = "QM";
    private const string LOBBY_TYPE_FRIENDS = "FR";

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
        public string avatarIndex;
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
                avatarIndex = data != null && data.ContainsKey(PLAYER_AVATAR_KEY) ? data[PLAYER_AVATAR_KEY].Value : "0"
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
        
        if (roomManager == null)
            roomManager = FindObjectOfType<RoomManager>();
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

    public void CreateLobbyFromRoom(string roomCode, int maxPlayers, bool isQuickMatch = false)
{
    this.maxPlayers = maxPlayers;
    string lobbyType = isQuickMatch ? LOBBY_TYPE_QUICKMATCH : LOBBY_TYPE_FRIENDS;
    Debug.Log($"[LobbyManager] Creating lobby from room - isQuickMatch: {isQuickMatch}, type: {lobbyType}");
    CreateLobby(roomCode, lobbyType);
}

    public void JoinLobbyFromRoom(string roomCode)
    {
        JoinLobbyByRoomCode(roomCode);
    }
    
    public void StartQuickMatch(int maxPlayers, int coinAmount = 0, Action callback = null)
    {
        this.maxPlayers = maxPlayers;
        isJoinMatchmaking = true;
        
        QuickJoinOrCreate(coinAmount);
    }

    // ===================== PLAYER DATA =====================

    private Dictionary<string, PlayerDataObject> GetLocalPlayerData()
    {
        var user = UnifiedAuthManager.Instance.GetCurrentUser();

        return new Dictionary<string, PlayerDataObject>
        {
            { PLAYER_UID_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.id) },
            { PLAYER_NAME_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.username) },
            { PLAYER_AVATAR_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.profilePictureIndex.ToString()) }
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

   // ===================== CREATE LOBBY =====================

private async void CreateLobby(string roomCode, string lobbyType)
{
    try
    {
        // Always include START_GAME_KEY with "0" value
        var lobbyData = new Dictionary<string, DataObject>
        {
            { ROOM_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Public, roomCode) },
            { LOBBY_TYPE_KEY, new DataObject(DataObject.VisibilityOptions.Public, lobbyType) },
            { MAX_PLAYERS_KEY, new DataObject(DataObject.VisibilityOptions.Public, maxPlayers.ToString()) },
            { START_GAME_KEY, new DataObject(DataObject.VisibilityOptions.Member, "0") }
        };

        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = lobbyData
        };

        string lobbyName = lobbyType == LOBBY_TYPE_QUICKMATCH ? 
            $"QM_{roomCode}" : roomCode;
        
        Debug.Log($"[Lobby] Creating {lobbyType} lobby with data: " +
                  $"CODE={roomCode}, TYPE={lobbyType}, MAX={maxPlayers}, START=0");
        
        hostLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        joinedLobby = hostLobby;
        isHost = true;

        await UpdateMyPlayerDataAndRefresh();

        // Verify the START key was saved
        if (joinedLobby.Data != null && joinedLobby.Data.ContainsKey(START_GAME_KEY))
        {
            Debug.Log($"[Lobby] Verified START key exists with value: {joinedLobby.Data[START_GAME_KEY].Value}");
        }
        else
        {
            Debug.LogError("[Lobby] START key missing after lobby creation!");
        }

        Debug.Log($"[Lobby] Created {lobbyType} Lobby | Room: {roomCode} | Max Players: {maxPlayers}");
    }
    catch (Exception e)
    {
        Debug.LogError("[Lobby] Create Failed: " + e);
    }
}

    // ===================== JOIN LOBBY BY ROOM CODE =====================

    private async void JoinLobbyByRoomCode(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode) || isJoining) return;
        isJoining = true;

        try
        {
            // Query all lobbies (we can't filter by custom data directly)
            var queryOptions = new QueryLobbiesOptions
            {
                Count = 25
                // No filters since we'll filter manually
            };

            var response = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);
            
            Lobby targetLobby = null;
            
            // Manual filtering for friends lobby with matching room code
            foreach (var lobby in response.Results)
            {
                if (lobby.Data != null && 
                    lobby.Data.ContainsKey(ROOM_CODE_KEY) && 
                    lobby.Data[ROOM_CODE_KEY].Value == roomCode &&
                    lobby.Data.ContainsKey(LOBBY_TYPE_KEY) &&
                    lobby.Data[LOBBY_TYPE_KEY].Value == LOBBY_TYPE_FRIENDS)
                {
                    targetLobby = lobby;
                    break;
                }
            }

            if (targetLobby == null)
            {
                // Debug.LogError($"[Lobby] No friends lobby found with room code: {roomCode}");
                return;
            }

            // Verify max players
            if (targetLobby.Data.ContainsKey(MAX_PLAYERS_KEY))
            {
                int lobbyMaxPlayers = int.Parse(targetLobby.Data[MAX_PLAYERS_KEY].Value);
                if (lobbyMaxPlayers != maxPlayers)
                {
                    Debug.LogError($"[Lobby] Max players mismatch. Expected: {maxPlayers}, Found: {lobbyMaxPlayers}");
                    return;
                }
            }

            joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(targetLobby.Id);
            isHost = joinedLobby.HostId == AuthenticationService.Instance.PlayerId;

            await UpdateMyPlayerDataAndRefresh();

            Debug.Log($"[Lobby] Joined friends Lobby | Room: {roomCode}");
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
    
    // ===================== QUICK JOIN OR CREATE =====================

   // ===================== QUICK JOIN OR CREATE =====================

// ===================== QUICK JOIN OR CREATE =====================

private async void QuickJoinOrCreate(int coinAmount)
{
    if (isJoining) return;
    isJoining = true;

    try
    {
        // Query lobbies with available slots
        var queryOptions = new QueryLobbiesOptions
        {
            Count = 25,
            Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"
                )
            }
        };

        var response = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);
        
        // Log all lobbies found for debugging
        Debug.Log($"[Matchmaking] Found {response.Results.Count} lobbies with available slots");
        
        List<Lobby> potentialLobbies = new List<Lobby>();
        
        foreach (var lobby in response.Results)
        {
            string lobbyType = "UNKNOWN";
            string maxPlayersStr = "UNKNOWN";
            string startGame = "0";
            
            if (lobby.Data != null)
            {
                if (lobby.Data.ContainsKey(LOBBY_TYPE_KEY))
                    lobbyType = lobby.Data[LOBBY_TYPE_KEY].Value;
                    
                if (lobby.Data.ContainsKey(MAX_PLAYERS_KEY))
                    maxPlayersStr = lobby.Data[MAX_PLAYERS_KEY].Value;
                    
                if (lobby.Data.ContainsKey(START_GAME_KEY))
                    startGame = lobby.Data[START_GAME_KEY].Value;
            }
            
            Debug.Log($"[Matchmaking] Lobby: {lobby.Name}, Type: {lobbyType}, Max: {maxPlayersStr}, Players: {lobby.Players.Count}/{lobby.MaxPlayers}, Started: {startGame}");
            
            // Store all lobbies for later filtering
            potentialLobbies.Add(lobby);
        }
        
        Lobby eligibleLobby = null;
        
        // First pass: try to find lobbies with all keys (preferred)
        foreach (var lobby in potentialLobbies)
        {
            if (lobby.Data == null) continue;
            
            // Check if it has all required keys
            bool hasAllKeys = lobby.Data.ContainsKey(LOBBY_TYPE_KEY) && 
                              lobby.Data.ContainsKey(MAX_PLAYERS_KEY) && 
                              lobby.Data.ContainsKey(START_GAME_KEY);
            
            if (!hasAllKeys) continue;
            
            string lobbyType = lobby.Data[LOBBY_TYPE_KEY].Value;
            string maxPlayersStr = lobby.Data[MAX_PLAYERS_KEY].Value;
            string gameStarted = lobby.Data[START_GAME_KEY].Value;
            
            // Parse max players
            if (!int.TryParse(maxPlayersStr, out int lobbyMaxPlayers)) continue;
            
            // Check if it's a quick match lobby with matching max players and game hasn't started
            if (lobbyType == LOBBY_TYPE_QUICKMATCH && 
                lobbyMaxPlayers == maxPlayers && 
                gameStarted == "0" &&
                lobby.AvailableSlots > 0)
            {
                eligibleLobby = lobby;
                Debug.Log($"[Matchmaking] Found eligible lobby (with all keys): {lobby.Name}");
                break;
            }
        }
        
        // Second pass: if no lobby with all keys, try lobbies missing START key (assume not started)
        if (eligibleLobby == null)
        {
            foreach (var lobby in potentialLobbies)
            {
                if (lobby.Data == null) continue;
                
                // Check if it has at least type and max players
                bool hasRequiredKeys = lobby.Data.ContainsKey(LOBBY_TYPE_KEY) && 
                                       lobby.Data.ContainsKey(MAX_PLAYERS_KEY);
                
                if (!hasRequiredKeys) continue;
                
                string lobbyType = lobby.Data[LOBBY_TYPE_KEY].Value;
                string maxPlayersStr = lobby.Data[MAX_PLAYERS_KEY].Value;
                
                // Parse max players
                if (!int.TryParse(maxPlayersStr, out int lobbyMaxPlayers)) continue;
                
                // Check if it's a quick match lobby with matching max players and has available slots
                // If START key is missing, assume game hasn't started
                if (lobbyType == LOBBY_TYPE_QUICKMATCH && 
                    lobbyMaxPlayers == maxPlayers && 
                    lobby.AvailableSlots > 0)
                {
                    eligibleLobby = lobby;
                    Debug.Log($"[Matchmaking] Found eligible lobby (missing START key, assuming not started): {lobby.Name}");
                    break;
                }
            }
        }

        if (eligibleLobby != null)
        {
            // Join existing lobby
            Debug.Log($"[Matchmaking] Attempting to join lobby: {eligibleLobby.Id} - {eligibleLobby.Name}");
            
            try
            {
                joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(eligibleLobby.Id);
                isHost = joinedLobby.HostId == AuthenticationService.Instance.PlayerId;

                await UpdateMyPlayerDataAndRefresh();

                Debug.Log($"[Matchmaking] Successfully joined existing quick match lobby with {maxPlayers} players");
                
                // Join backend room
                if (roomManager != null && joinedLobby.Data != null && joinedLobby.Data.ContainsKey(ROOM_CODE_KEY))
                {
                    string roomCode = joinedLobby.Data[ROOM_CODE_KEY].Value;
                    Debug.Log($"[Matchmaking] Joining backend room with code: {roomCode}");
                    roomManager.JoinRoom(roomCode);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] Failed to join lobby: {e.Message}");
                // If join fails, create new lobby
                await CreateNewQuickMatchLobby(coinAmount);
            }
        }
        else
        {
            await CreateNewQuickMatchLobby(coinAmount);
        }
    }
    catch (LobbyServiceException e)
    {
        Debug.LogError($"[Matchmaking] Failed with error: {e.Message}, Reason: {e.Reason}");
        await CreateNewQuickMatchLobby(coinAmount);
    }
    finally
    {
        isJoining = false;
    }
}

private async Task CreateNewQuickMatchLobby(int coinAmount)
{
    // No eligible lobby found, create new one
    Debug.Log($"[Matchmaking] No {maxPlayers}-player quick match lobby found → Creating new lobby");
    
    // Generate a unique room code
    string roomCode = UnityEngine.Random.Range(1000, 9999).ToString();
    
    // Create backend room with the generated code - PASS true FOR isQuickMatch
    if (roomManager != null)
    {
        Debug.Log($"[Matchmaking] Creating backend room with coin: {coinAmount}, max players: {maxPlayers}, code: {roomCode}");
        
        // Set flag that we're in quick match mode
        isJoinMatchmaking = true;
        
        // Create the backend room with quick match flag
        roomManager.CreateRoom("pick_or_perish", coinAmount, maxPlayers, true);
        
        // Note: The lobby will be created when RoomManager calls back to CreateLobbyFromRoom
        // with isQuickMatch=true, which will create a QM lobby with all required keys
    }
    else
    {
        // Fallback: create quick match lobby directly
        CreateLobby(roomCode, LOBBY_TYPE_QUICKMATCH);
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
    
    public async void JoinRelayAfterMigration(string joinCode)
    {
        try
        {
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartClient();

            Debug.Log("🟢 Reconnected to new host.");
        }
        catch (Exception e)
        {
            Debug.LogError("Reconnect failed: " + e);
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
    
    public async Task<string> RecreateRelayAsHost()
    {
        Allocation allocation = await Relay.Instance.CreateAllocationAsync(MaxPlayers);
        string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

        NetworkManager.Singleton.StartHost();

        await Lobbies.Instance.UpdateLobbyAsync(CurrentLobby.Id,
            new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { START_GAME_KEY, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            });

        return joinCode;
    }

    public void SetIsMatchMaking(bool flag)
    {
        isJoinMatchmaking = flag;
    }

    public void SetMatchmakingMaxPlayers(int count)
    {
        networkConnectionHandler.SetMaxPlayersCount(count);
    }
}