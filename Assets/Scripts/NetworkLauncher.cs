using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Arena.API.Models;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkLauncher : MonoBehaviourPunCallbacks
{
    [Header("Photon Settings")]
    [SerializeField] private string gameVersion = "connect4_v1";
    [SerializeField] private byte maxPlayers = 2;

    [Header("References")]
    [SerializeField] private ConnectRoomManager roomManager;

    private bool intentionalDisconnect;
    private bool isHost;
    private string pendingRoomCode;
    private int pendingCoinAmount;
    private bool isJoiningRandom;

    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool IsInRoom => PhotonNetwork.InRoom;
    public bool IsHost => isHost;

    public event Action OnPhotonConnected;
    public event Action OnPhotonRoomJoined;
    public event Action<Photon.Realtime.Player> OnPhotonPlayerJoined;
    public event Action<Photon.Realtime.Player> OnPhotonPlayerLeft;
    public event Action<DisconnectCause> OnPhotonDisconnected;

    private void Awake()
    {
        if (roomManager == null)
            roomManager = FindObjectOfType<ConnectRoomManager>();

        if (roomManager == null)
        {
            GameObject managerObj = new GameObject("ConnectRoomManager");
            roomManager = managerObj.AddComponent<ConnectRoomManager>();
            DontDestroyOnLoad(managerObj);
        }

        // Subscribe to backend events
        roomManager.OnRoomCreated += HandleBackendRoomCreated;
        roomManager.OnRoomJoined += HandleBackendRoomJoined;
        roomManager.OnRoomLeft += HandleBackendRoomLeft;
        roomManager.OnRoomError += HandleBackendRoomError;
    }

    private void OnDestroy()
    {
        if (roomManager != null)
        {
            roomManager.OnRoomCreated -= HandleBackendRoomCreated;
            roomManager.OnRoomJoined -= HandleBackendRoomJoined;
            roomManager.OnRoomLeft -= HandleBackendRoomLeft;
            roomManager.OnRoomError -= HandleBackendRoomError;
        }
    }

    #region PUBLIC METHODS

    public void StartRandomMatchmaking(int coinAmount = 1000000)
    {
        Debug.Log("[NetworkLauncher] Starting random matchmaking...");

        pendingCoinAmount = coinAmount;
        isJoiningRandom = true;
        pendingRoomCode = null;

        ConnectToPhoton();
    }

    public void JoinRoomByCode(string roomCode)
    {
        Debug.Log($"[NetworkLauncher] Joining room by code: {roomCode}");

        pendingRoomCode = roomCode;
        isJoiningRandom = false;

        ConnectToPhoton();
    }

    public void LeaveRoom()
    {
        Debug.Log("[NetworkLauncher] Leaving room...");

        if (roomManager != null && roomManager.IsInRoom)
        {
            roomManager.LeaveRoom();
        }
        else
        {
            LeavePhotonRoom();
        }
    }

    public void DisconnectAndGoToMainMenu()
    {
        Debug.Log("[NetworkLauncher] Disconnecting and going to main menu...");

        intentionalDisconnect = true;

        if (roomManager != null && roomManager.IsInRoom)
        {
            roomManager.LeaveRoom();
        }
        else if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            LoadMainMenu();
        }
    }

    #endregion

    #region PHOTON CONNECTION

    private void ConnectToPhoton()
    {
        Debug.Log("[NetworkLauncher] Connecting to Photon...");

        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[NetworkLauncher] Connected to Photon Master");
        OnPhotonConnected?.Invoke();

        if (isJoiningRandom)
        {
            Debug.Log("[NetworkLauncher] Trying to join random Photon room...");
            PhotonNetwork.JoinRandomRoom();
        }
        else if (!string.IsNullOrEmpty(pendingRoomCode))
        {
            Debug.Log($"[NetworkLauncher] Trying to join specific room: {pendingRoomCode}");
            PhotonNetwork.JoinRoom(pendingRoomCode);
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[NetworkLauncher] No random Photon rooms available. Creating backend room...");

        if (roomManager != null)
        {
            roomManager.CreateRoom(pendingCoinAmount);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[NetworkLauncher] Failed to join specific room: {message}");

        pendingRoomCode = null;
        isJoiningRandom = false;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[NetworkLauncher] Joined Photon room: {PhotonNetwork.CurrentRoom.Name} " +
                  $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayers}");

        string roomName = PhotonNetwork.CurrentRoom.Name;

        if (roomManager != null)
        {
            // Check if we're already in a backend room (as creator)
            if (roomManager.IsInRoom)
            {
                Debug.Log($"[NetworkLauncher] Already in backend room as creator, ready to start");
                isHost = PhotonNetwork.IsMasterClient;
                pendingRoomCode = null;
                isJoiningRandom = false;
                OnPhotonRoomJoined?.Invoke();
            }
            else if (isJoiningRandom || !string.IsNullOrEmpty(pendingRoomCode))
            {
                // We joined a random or specific room, need to register with backend
                Debug.Log($"[NetworkLauncher] Registering room join with backend: {roomName}");
                roomManager.JoinRoomByCode(roomName);
            }
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("[NetworkLauncher] Photon room created successfully");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[NetworkLauncher] Failed to create Photon room: {message}");

        if (roomManager != null && roomManager.IsInRoom)
        {
            roomManager.LeaveRoom();
        }

        isJoiningRandom = false;
        pendingRoomCode = null;
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"[NetworkLauncher] Player joined - ActorNumber: {newPlayer.ActorNumber}");
        OnPhotonPlayerJoined?.Invoke(newPlayer);

        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"[NetworkLauncher] Player left - ActorNumber: {otherPlayer.ActorNumber}");
        OnPhotonPlayerLeft?.Invoke(otherPlayer);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[NetworkLauncher] Disconnected from Photon: {cause}");

        OnPhotonDisconnected?.Invoke(cause);

        if (intentionalDisconnect)
        {
            intentionalDisconnect = false;
            LoadMainMenu();
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[NetworkLauncher] Left Photon room");
        ClearPhotonState();
    }

    private void LeavePhotonRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[NetworkLauncher] Leaving Photon room");
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            ClearPhotonState();
        }
    }

    private void ClearPhotonState()
    {
        pendingRoomCode = null;
        isJoiningRandom = false;
        isHost = false;
    }

    #endregion

    #region BACKEND EVENT HANDLERS

    private void HandleBackendRoomCreated(CreateRoomResponse response)
    {
        Debug.Log($"[NetworkLauncher] Backend room created: {response.roomCode} (ID: {response.roomId})");

        pendingRoomCode = response.roomCode;
        isJoiningRandom = false;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsOpen = true,
            IsVisible = true,
            CleanupCacheOnLeave = true
        };

        Debug.Log($"[NetworkLauncher] Creating Photon room: {response.roomCode}");
        PhotonNetwork.CreateRoom(response.roomCode, options, TypedLobby.Default);
    }

    private void HandleBackendRoomJoined(CreateRoomResponse response)
    {
        Debug.Log($"[NetworkLauncher] Backend room joined: {response.roomCode} (ID: {response.roomId})");

        isHost = PhotonNetwork.IsMasterClient;
        pendingRoomCode = null;
        isJoiningRandom = false;

        OnPhotonRoomJoined?.Invoke();
    }

    private void HandleBackendRoomLeft()
    {
        Debug.Log("[NetworkLauncher] Backend room left");
        LeavePhotonRoom();
    }

    private void HandleBackendRoomError(string error)
    {
        Debug.LogError($"[NetworkLauncher] Backend error: {error}");

        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[NetworkLauncher] Leaving Photon room due to backend error");
            PhotonNetwork.LeaveRoom();
        }

        pendingRoomCode = null;
        isJoiningRandom = false;
    }

    #endregion

    private void LoadMainMenu()
    {
        SceneLoader loader = FindFirstObjectByType<SceneLoader>(FindObjectsInactive.Include);
        if (loader != null)
        {
            loader.LoadSceneIndex(0);
        }
    }
}