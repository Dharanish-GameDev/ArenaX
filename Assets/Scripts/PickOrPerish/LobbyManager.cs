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

    private float heartbeatTimer;
    private const float HEARTBEAT_INTERVAL = 15f;
    
    private float lobbyPollTimer;
    private const float LOBBY_POLL_INTERVAL = 2.0f;
    
    public int MaxPlayers => maxPlayers;


    
    // public event Action<Lobby> OnLobbyUpdated;
    // public event Action<bool> OnHostChanged;
    // private void RaiseLobbyEvents()
    // {
    //     OnLobbyUpdated?.Invoke(joinedLobby);
    //     OnHostChanged?.Invoke(isHost);
    // }


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

    // ===================== CORE LOBBY =====================

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
            //RaiseLobbyEvents();
            Debug.Log($"[Lobby] Created Lobby: {roomCode}");
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Create Failed: " + e);
        }
    }

    private bool isJoining = false;
    private async void JoinLobby(string roomCode)
    {
        if(isJoining) return;
        try
        {
            // 🔥 Step 1: Check if already inside a lobby
            var joined = await Lobbies.Instance.GetJoinedLobbiesAsync();

            if (joined.Count > 0)
            {
                Debug.Log("[Lobby] Already in a lobby — reusing session");

                joinedLobby = await Lobbies.Instance.GetLobbyAsync(joined[0]);
                isHost = joinedLobby.HostId == AuthenticationService.Instance.PlayerId;

                Debug.Log($"[Lobby] Reconnected Lobby: {joinedLobby.Name}");
                return;
            }

            // 🔥 Step 2: Search lobbies (pagination safe)
            string continuationToken = null;

            do
            {
                var response = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                {
                    Count = 25,
                    ContinuationToken = continuationToken
                });

                foreach (var lobby in response.Results)
                {
                    if (lobby.Data != null &&
                        lobby.Data.ContainsKey(ROOM_CODE_KEY) &&
                        lobby.Data[ROOM_CODE_KEY].Value == roomCode)
                    {
                        joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id);
                        isHost = false;
                        //RaiseLobbyEvents();
                        Debug.Log($"[Lobby] Joined Lobby: {roomCode}");
                        isJoining = false;
                        return;
                    }
                }

                continuationToken = response.ContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            Debug.LogError("[Lobby] No lobby found with room code: " + roomCode);
            isJoining = false;
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Join Failed: " + e);
            isJoining = false;
        }
    }


    // ===================== GAME START =====================

    public async void StartGame()
    {
        if (!isHost || joinedLobby == null) return;

        try
        {
            string relayCode = await CreateRelay();

            await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { START_GAME_KEY, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });

            Debug.Log("[Lobby] Game Started");
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
        try
        {
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            networkConnectionHandler.StartAsClient();
            isGameStarted = true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Relay] Join Failed: " + e);
        }
    }

    // ===================== LOBBY SYNC =====================
    private bool isPolling;

    private async void PollLobby()
    {
        if (joinedLobby == null || isPolling) return;

        isPolling = true;

        try
        {
            joinedLobby = await Lobbies.Instance.GetLobbyAsync(joinedLobby.Id);

            if (!isHost &&
                joinedLobby.Data.ContainsKey(START_GAME_KEY) &&
                joinedLobby.Data[START_GAME_KEY].Value != "0")
            {
                JoinRelay(joinedLobby.Data[START_GAME_KEY].Value);
                joinedLobby = null;
            }
        }
        catch { }
        finally
        {
            isPolling = false;
        }
    }



    private async void HandleHeartbeat()
    {
        if (hostLobby == null) return;

        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = HEARTBEAT_INTERVAL;
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    }
}
