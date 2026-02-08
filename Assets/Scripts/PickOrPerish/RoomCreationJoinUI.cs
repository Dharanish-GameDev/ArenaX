using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arena.API.Models;

public class RoomCreationJoinUI : MonoBehaviour
{
    [Header("References")]
    public RoomManager roomManager;

    [Header("Create Room UI")]
    public string gameName = "pick_or_perish";
    public int coinAmount = 1000000;
    public TMP_Dropdown maxPlayersDropdown;
    public Button createRoomButton;

    [Header("Join Room UI")]
    public TMP_InputField roomCodeInput;
    public Button joinRoomButton;

    [Header("Current Room UI")]
    public GameObject roomPanel;
    public GameObject creationJoinPanel;
    public Button leaveRoomButton;
    public Button copyCodeButton;
    public TextMeshProUGUI roomCodeText;

    [Header("Max Players Dropdown Mapping")]
    [Tooltip("Index maps to dropdown option index")]
    public int[] maxPlayerValues = { 2, 4, 6, 8 };

    private bool isJoiningRoom;

    [SerializeField] private GameObject LobbyUI;
    
    [SerializeField] private Button nextToLobbyButton;

    private void Start()
    {
        if (roomManager == null)
        {
            roomManager = FindObjectOfType<RoomManager>();
            if (roomManager == null)
            {
                Debug.LogError("No RoomManager found in scene!");
                return;
            }
        }

        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
        leaveRoomButton.onClick.AddListener(LeaveRoom);
        copyCodeButton.onClick.AddListener(CopyRoomCode);

        roomManager.OnRoomCreated += OnRoomCreated;
        roomManager.OnRoomJoined += OnRoomJoined;
        roomManager.OnRoomLeft += OnRoomLeft;
        roomManager.OnRoomError += OnRoomError;

        UpdateRoomUI();
    }

    private void CreateRoom()
    {
        isJoiningRoom = false;
        int maxPlayers = GetMaxPlayersFromDropdown();
        roomManager.CreateRoom(gameName, coinAmount, maxPlayers);
    }
    private bool isJoining;
    private void JoinRoom()
    {
        if (isJoining) return;
        if (string.IsNullOrEmpty(roomCodeInput.text))
        {
            Debug.LogError("Room code is required");
            return;
        }

        isJoiningRoom = true;
        roomManager.JoinRoom(roomCodeInput.text.Trim().ToUpper());
        Invoke(nameof(ResetJoinFlag), 2f);
    }
    private void ResetJoinFlag()
    {
        isJoining = false;
    }

    private int GetMaxPlayersFromDropdown()
    {
        if (maxPlayersDropdown == null || maxPlayerValues == null || maxPlayerValues.Length == 0)
            return 4;

        int index = maxPlayersDropdown.value;

        if (index < 0 || index >= maxPlayerValues.Length)
            return 4;

        return maxPlayerValues[index];
    }

    private void LeaveRoom()
    {
        roomManager.LeaveRoom();
    }

    private void CopyRoomCode()
    {
        roomManager.CopyRoomCodeToClipboard();
    }

    private void OnRoomCreated(CreateRoomResponse response)
    {
        Debug.Log($"Room Created! Code: {response.roomCode}");
        UpdateRoomUI();
        nextToLobbyButton.onClick.RemoveAllListeners();
        nextToLobbyButton.onClick.AddListener(() =>
        {
            roomPanel.SetActive(false);
            creationJoinPanel.SetActive(false);
            LobbyUI.SetActive(true);
        });
    }

    private void OnRoomJoined(CreateRoomResponse response)
    {
        Debug.Log($"Joined Room: {response.roomCode}");
        roomCodeInput.text = "";
        UpdateRoomUI();
        roomPanel.gameObject.SetActive(false);
        creationJoinPanel.gameObject.SetActive(false);
        LobbyUI.SetActive(true);
    }

    private void OnRoomLeft()
    {
        Debug.Log("Left the room");
        isJoiningRoom = false;
        UpdateRoomUI();
    }

    private void OnRoomError(string error)
    {
        Debug.LogError($"Room Error: {error}");
        isJoiningRoom = false;
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        bool inRoom = roomManager.IsInRoom;

        roomPanel.SetActive(inRoom && !isJoiningRoom);
        creationJoinPanel.SetActive(!inRoom);

        if (inRoom)
        {
            var roomData = roomManager.CurrentRoomData;
            roomCodeText.text = roomData.roomCode;
        }

        createRoomButton.interactable = !inRoom;
        joinRoomButton.interactable = !inRoom;
    }

    private void OnDestroy()
    {
        if (roomManager != null)
        {
            roomManager.OnRoomCreated -= OnRoomCreated;
            roomManager.OnRoomJoined -= OnRoomJoined;
            roomManager.OnRoomLeft -= OnRoomLeft;
            roomManager.OnRoomError -= OnRoomError;
        }
    }
}
