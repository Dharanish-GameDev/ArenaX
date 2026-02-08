using System;
using UnityEngine;
using Arena.API.Models;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoomManager : MonoBehaviour
{
    #if UNITY_EDITOR
    [Header("Debug Settings")]
    [SerializeField] private bool useDebugMode = false;
    [SerializeField] private string debugCreateRoomResponse =
        "{\"roomId\":\"debug_room_123\",\"roomCode\":\"DEBUG1\",\"game\":\"DebugGame\",\"coinAmount\":100,\"maxPlayers\":4}";
    [SerializeField] private string debugJoinRoomResponse =
        "{\"roomId\":\"debug_room_456\",\"roomCode\":\"DEBUG2\",\"game\":\"DebugGame\",\"coinAmount\":200,\"maxPlayers\":6}";
    [SerializeField] private string debugLeaveRoomResponse = "{\"success\":true}";
    [SerializeField] private bool debugLogApiCalls = true;
    #endif

    [Header("References")]
    [SerializeField] private LobbyManager lobbyManager;

    private string _currentRoomId;
    private string _currentRoomCode;
    private CreateRoomResponse _currentRoomData;

    public string CurrentRoomId => _currentRoomId;
    public string CurrentRoomCode => _currentRoomCode;
    public CreateRoomResponse CurrentRoomData => _currentRoomData;
    public bool IsInRoom => !string.IsNullOrEmpty(_currentRoomId);

    public event Action<CreateRoomResponse> OnRoomCreated;
    public event Action<CreateRoomResponse> OnRoomJoined;
    public event Action OnRoomLeft;
    public event Action<string> OnRoomError;

    private void Awake()
    {
        if (lobbyManager == null)
            lobbyManager = FindObjectOfType<LobbyManager>();

        OnRoomCreated += HandleRoomCreated;
        OnRoomJoined += HandleRoomJoined;
    }

    #region Backend API

    public void CreateRoom(string game, int coinAmount, int maxPlayers)
    {
        #if UNITY_EDITOR
        if (useDebugMode)
        {
            Debug.Log($"<color=yellow>[DEBUG] CreateRoom: game={game}, coin={coinAmount}, max={maxPlayers}</color>");
            SimulateCreateRoom(game, coinAmount, maxPlayers);
            return;
        }
        #endif

        var request = new CreateRoomRequest
        {
            game = game,
            coinAmount = coinAmount,
            maxPlayers = maxPlayers
        };

        string json = JsonConvert.SerializeObject(request);

        #if UNITY_EDITOR
        if (debugLogApiCalls)
            Debug.Log($"<color=cyan>[API] CreateRoom → {json}</color>");
        #endif

        ApiManager.Instance.SendRequest<CreateRoomResponse>(
            ApiEndPoints.Rooms.Create,
            RequestMethod.POST,
            OnCreateSuccess,
            OnCreateError,
            json
        );
    }

    public void JoinRoom(string roomCode)
    {
        #if UNITY_EDITOR
        if (useDebugMode)
        {
            Debug.Log($"<color=yellow>[DEBUG] JoinRoom: {roomCode}</color>");
            SimulateJoinRoom(roomCode);
            return;
        }
        #endif

        var request = new JoinRoomRequest { roomCode = roomCode };
        string json = JsonConvert.SerializeObject(request);

        #if UNITY_EDITOR
        if (debugLogApiCalls)
            Debug.Log($"<color=cyan>[API] JoinRoom → {json}</color>");
        #endif

        ApiManager.Instance.SendRequest<CreateRoomResponse>(
            ApiEndPoints.Rooms.Join,
            RequestMethod.POST,
            OnJoinSuccess,
            OnJoinError,
            json
        );
    }

    public void LeaveRoom()
    {
        if (!IsInRoom) return;

        #if UNITY_EDITOR
        if (useDebugMode)
        {
            Debug.Log($"<color=yellow>[DEBUG] LeaveRoom</color>");
            ClearRoomData();
            OnRoomLeft?.Invoke();
            return;
        }
        #endif

        var request = new LeaveRoomRequest { roomId = _currentRoomId };
        string json = JsonConvert.SerializeObject(request);

        #if UNITY_EDITOR
        if (debugLogApiCalls)
            Debug.Log($"<color=cyan>[API] LeaveRoom → {json}</color>");
        #endif

        ApiManager.Instance.SendRequest<object>(
            ApiEndPoints.Rooms.Leave,
            RequestMethod.POST,
            (_) =>
            {
                ClearRoomData();
                OnRoomLeft?.Invoke();
            },
            (error) =>
            {
                Debug.LogError("[Room] Leave Failed: " + error);
                OnRoomError?.Invoke(error);
            },
            json
        );
    }

    #endregion

    #region API Callbacks

    private void OnCreateSuccess(CreateRoomResponse response)
    {
        _currentRoomId = response.roomId;
        _currentRoomCode = response.roomCode;
        _currentRoomData = response;

        OnRoomCreated?.Invoke(response);
        Debug.Log($"[Room] Created: {response.roomCode}");
    }

    private void OnCreateError(string error)
    {
        Debug.LogError("[Room] Create Failed: " + error);
        OnRoomError?.Invoke(error);
    }

    private void OnJoinSuccess(CreateRoomResponse response)
    {
        _currentRoomId = response.roomId;
        _currentRoomCode = response.roomCode;
        _currentRoomData = response;

        OnRoomJoined?.Invoke(response);
        Debug.Log($"[Room] Joined: {response.roomCode}");
    }

    private void OnJoinError(string error)
    {
        Debug.LogError("[Room] Join Failed: " + error);
        OnRoomError?.Invoke(error);
    }

    #endregion

    #region Debug Simulation

    #if UNITY_EDITOR
    private void SimulateCreateRoom(string game, int coinAmount, int maxPlayers)
    {
        var response = JsonConvert.DeserializeObject<CreateRoomResponse>(debugCreateRoomResponse);
        response.game = game;
        response.coinAmount = coinAmount;
        response.maxPlayers = maxPlayers;

        _currentRoomId = response.roomId;
        _currentRoomCode = response.roomCode;
        _currentRoomData = response;

        OnRoomCreated?.Invoke(response);
    }

    private void SimulateJoinRoom(string roomCode)
    {
        var response = JsonConvert.DeserializeObject<CreateRoomResponse>(debugJoinRoomResponse);
        response.roomCode = roomCode;

        _currentRoomId = response.roomId;
        _currentRoomCode = response.roomCode;
        _currentRoomData = response;

        OnRoomJoined?.Invoke(response);
    }
    #endif

    #endregion

    #region Lobby Bridge

    private void HandleRoomCreated(CreateRoomResponse response)
    {
        lobbyManager.CreateLobbyFromRoom(
            response.roomCode,
            response.maxPlayers
        );
    }

    private void HandleRoomJoined(CreateRoomResponse response)
    {
        lobbyManager.JoinLobbyFromRoom(
            response.roomCode
        );
    }

    #endregion

    #region Utilities

    public void CopyRoomCodeToClipboard()
    {
        if (string.IsNullOrEmpty(_currentRoomCode)) return;

        GUIUtility.systemCopyBuffer = _currentRoomCode;
        Debug.Log($"[Room] Copied Room Code: {_currentRoomCode}");
    }

    #endregion

    private void ClearRoomData()
    {
        _currentRoomId = null;
        _currentRoomCode = null;
        _currentRoomData = null;
    }
}
