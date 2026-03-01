using System;
using UnityEngine;
using Arena.API.Models;
using Newtonsoft.Json;

public class ConnectRoomManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private string gameName = "connect4";
    
    private string _currentRoomId;
    private string _currentRoomCode;
    private CreateRoomResponse _currentRoomData;

    public string CurrentRoomId => _currentRoomId;
    public string CurrentRoomCode => _currentRoomCode;
    public CreateRoomResponse CurrentRoomData => _currentRoomData;
    public string GameName => gameName;
    public bool IsInRoom => !string.IsNullOrEmpty(_currentRoomId);

    public event Action<CreateRoomResponse> OnRoomCreated;
    public event Action<CreateRoomResponse> OnRoomJoined;
    public event Action OnRoomLeft;
    public event Action<string> OnRoomError;

    #region BACKEND API CALLS

    public void CreateRoom(int coinAmount = 0)
    {
        Debug.Log("[ConnectRoomManager] Creating new room...");

        var request = new CreateRoomRequest
        {
            game = gameName,
            coinAmount = coinAmount,
            maxPlayers = 2
        };

        string json = JsonConvert.SerializeObject(request);

        ApiManager.Instance.SendRequest<CreateRoomResponse>(
            ApiEndPoints.Rooms.Create,
            RequestMethod.POST,
            OnCreateRoomSuccess,
            OnApiError,
            json
        );
    }

    public void JoinRoomByCode(string roomCode)
    {
        Debug.Log($"[ConnectRoomManager] Joining room by code: {roomCode}");

        var request = new JoinRoomRequest 
        { 
            roomCode = roomCode,
        };
        
        string json = JsonConvert.SerializeObject(request);

        ApiManager.Instance.SendRequest<CreateRoomResponse>(
            ApiEndPoints.Rooms.Join,
            RequestMethod.POST,
            OnJoinRoomSuccess,
            OnApiError,
            json
        );
    }

    public void LeaveRoom()
    {
        if (!IsInRoom)
        {
            Debug.Log("[ConnectRoomManager] Not in a room, nothing to leave");
            return;
        }

        Debug.Log($"[ConnectRoomManager] Leaving room: {_currentRoomId}");

        var request = new LeaveRoomRequest { roomId = _currentRoomId };
        string json = JsonConvert.SerializeObject(request);

        ApiManager.Instance.SendRequest<object>(
            "api/rooms/leave",
            RequestMethod.POST,
            (_) =>
            {
                Debug.Log("[ConnectRoomManager] Successfully left room");
                ClearRoomData();
                OnRoomLeft?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"[ConnectRoomManager] Failed to leave room: {error}");
                ClearRoomData();
                OnRoomError?.Invoke(error);
            },
            json
        );
    }

    #endregion

    #region API CALLBACKS

    private void OnCreateRoomSuccess(CreateRoomResponse response)
    {
        Debug.Log($"[ConnectRoomManager] Room created: {response.roomCode} (ID: {response.roomId}) (Coin Amount : {response.coinAmount})");
        
        _currentRoomId = response.roomId;
        _currentRoomCode = response.roomCode;
        _currentRoomData = response;

        OnRoomCreated?.Invoke(response);
    }

    private void OnJoinRoomSuccess(CreateRoomResponse response)
    {
        Debug.Log($"[ConnectRoomManager] Room joined: {response.roomCode} (ID: {response.roomId})");
        
        _currentRoomId = response.roomId;
        _currentRoomCode = response.roomCode;
        _currentRoomData = response;

        OnRoomJoined?.Invoke(response);
    }

    private void OnApiError(string error)
    {
        Debug.LogError($"[ConnectRoomManager] API Error: {error}");
        OnRoomError?.Invoke(error);
    }

    #endregion

    #region UTILITIES

    private void ClearRoomData()
    {
        _currentRoomId = null;
        _currentRoomCode = null;
        _currentRoomData = null;
    }

    public void CopyRoomCodeToClipboard()
    {
        if (!string.IsNullOrEmpty(_currentRoomCode))
        {
            GUIUtility.systemCopyBuffer = _currentRoomCode;
            Debug.Log($"[ConnectRoomManager] Copied: {_currentRoomCode}");
        }
    }

    #endregion
}