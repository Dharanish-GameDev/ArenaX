using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public enum ConnectType : byte
{
    Connect4,
    Connect5
}

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager instance;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }
    
    [SerializeField] private ConnectType connectType;
    
    // Game interface for the current game type
    private IConnectGame currentGame;
    
    // Board interface
    public IConnectBoard GameBoard { get; private set; }
    
    // UI References
    [SerializeField] private RectTransform[] rowRects;
    [SerializeField] private RectTransform[] columnRects;
    [SerializeField] private Image[] columnHighlights;
    
    [Header("Canvas Reference")]
    [SerializeField] private RectTransform piecesParent;
    public RectTransform PiecesParent => piecesParent;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI winnerText;

    [Header("Win Line")]
    [SerializeField] private RectTransform winLineImage;
    [SerializeField] private float winLineThickness = 14f;
    [SerializeField] private Color winLineColor = new Color(1f, 1f, 1f, 0.9f);
    private Image winLineImgComponent;

    [Header("Column Highlight")]
    [SerializeField] private Color columnHighlightColor = new Color(1f, 1f, 1f, 0.18f);

    private RectTransform boardSpaceRoot;
    private bool isGameOver;
    private PlayerAlliance currentTurn = PlayerAlliance.RED;
    private bool localClickLock;

    private const string PROP_TURN = "TURN";
    private const string PROP_BOARD = "BOARD";
    private const string PROP_READY = "READY";
    private const string PROP_GAME_TYPE = "GAME_TYPE";

    private readonly Dictionary<int, Connect4Piece> pieceMap = new Dictionary<int, Connect4Piece>();
    private int CellKey(int r, int c) => (r * 1000) + c;

    public void RegisterPiece(int r, int c, Connect4Piece piece)
    {
        if (piece == null) return;
        pieceMap[CellKey(r, c)] = piece;
    }

    private void Start()
    {
        ResolveUIRefs();
        InitializeGame();
        
        boardSpaceRoot = (rowRects != null && rowRects.Length > 0) ? rowRects[0].parent as RectTransform : null;

        EnsureWinLineImageExists();
        HideWinLine();

        EnsureColumnHighlightsExist();
        HideAllColumnHighlights();

        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(true);
            winnerText.text = "";
        }

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                EnsureRoomPropertiesExist();
                MasterUpdateReadyAndInitTurn();
            }

            ReadTurnFromRoom();
            LoadBoardFromProperty();
        }

        UpdateStatus();
        UpdateUndoButtonState(false);
    }
    
    private void InitializeGame()
    {
        // Create the appropriate game based on connectType
        currentGame = connectType switch
        {
            ConnectType.Connect4 => new Connect4Game(),
            ConnectType.Connect5 => new Connect5Game(),
            _ => new Connect4Game()
        };
        
        // Create board
        GameBoard = currentGame.CreateBoard();
        
        isGameOver = false;
        localClickLock = false;
        
        // Validate UI references match board size
        if (rowRects != null && rowRects.Length != GameBoard.NumRows)
            Debug.LogWarning($"Row rects count ({rowRects.Length}) doesn't match board rows ({GameBoard.NumRows})");
            
        if (columnRects != null && columnRects.Length != GameBoard.NumCols)
            Debug.LogWarning($"Column rects count ({columnRects.Length}) doesn't match board columns ({GameBoard.NumCols})");
    }

    // ================================================================
    // INPUT
    // ================================================================
    public void MakeMove(int column)
    {
        if (localClickLock) return;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            SetStatus("Connecting...");
            return;
        }

        if (!IsRoomReady())
        {
            SetStatus("Waiting for opponent...");
            return;
        }

        ReadTurnFromRoom();

        if (!CanLocalPlayerInput())
        {
            SetStatus("Not your turn");
            return;
        }

        // Use dynamic board size
        if (column < 0 || column >= GameBoard.NumCols) return;

        if (photonView == null || photonView.ViewID == 0)
        {
            Debug.LogError("[GM] PhotonView missing or ViewID=0. Add PhotonView + assign Scene View ID!");
            return;
        }

        localClickLock = true;
        photonView.RPC(nameof(RPC_RequestMove), RpcTarget.MasterClient, column, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    private bool CanLocalPlayerInput()
    {
        if (isGameOver) return false;
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return false;
        if (PhotonNetwork.CurrentRoom == null) return false;
        if (!IsRoomReady()) return false;

        if (PhotonNetwork.IsMasterClient && currentTurn == PlayerAlliance.RED) return true;
        if (!PhotonNetwork.IsMasterClient && currentTurn == PlayerAlliance.BLACK) return true;

        return false;
    }

    // ================================================================
    // MASTER AUTHORITATIVE MOVE
    // ================================================================
    [PunRPC]
    private void RPC_RequestMove(int column, int senderActor, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        MasterUpdateReadyAndInitTurn();

        if (!IsRoomReady() || isGameOver || column < 0 || column >= GameBoard.NumCols)
        {
            photonView.RPC(nameof(RPC_UnlockInput), RpcTarget.All, senderActor);
            return;
        }

        if (!CanActorPlayNow(senderActor))
        {
            photonView.RPC(nameof(RPC_UnlockInput), RpcTarget.All, senderActor);
            return;
        }

        int dropRow = FindDropRow(column);
        if (dropRow < 0)
        {
            photonView.RPC(nameof(RPC_UnlockInput), RpcTarget.All, senderActor);
            return;
        }

        try
        {
            GameBoard.SetPiece(column, currentTurn);

            photonView.RPC(nameof(RPC_SetColumnHighlight), RpcTarget.All, column, true);

            SpawnPiece_MasterOnly(column, dropRow, currentTurn);
            SetBoardProperty();

            Tile placedTile = (currentTurn == PlayerAlliance.RED) ? Tile.RED : Tile.BLACK;

            // ✅ WIN CHECK using current game's logic
            if (currentGame.IsGameFinished(GameBoard))
            {
                isGameOver = true;

                // Get winning cells from current game
                var winCells = currentGame.GetWinningCells(GameBoard);
                
                // Convert to arrays for RPC
                int[] rows = new int[winCells.Count];
                int[] cols = new int[winCells.Count];
                for (int i = 0; i < winCells.Count; i++)
                {
                    rows[i] = winCells[i].x;
                    cols[i] = winCells[i].y;
                }
                photonView.RPC(nameof(RPC_GlowWinningPieces), RpcTarget.AllBuffered, rows, cols);

                // Get line endpoints for UI
                Vector2 aAnch = GetAnchoredPosForCell(winCells[0].x, winCells[0].y);
                Vector2 bAnch = GetAnchoredPosForCell(winCells[winCells.Count - 1].x, winCells[winCells.Count - 1].y);

                photonView.RPC(nameof(RPC_GameOverWithUILine), RpcTarget.AllBuffered,
                    (byte)currentTurn,
                    aAnch.x, aAnch.y,
                    bAnch.x, bAnch.y);

                photonView.RPC(nameof(RPC_UnlockInput), RpcTarget.All, senderActor);
                return;
            }

            currentTurn = (currentTurn == PlayerAlliance.RED) ? PlayerAlliance.BLACK : PlayerAlliance.RED;
            SetTurnProperty(currentTurn);
            photonView.RPC(nameof(RPC_SyncTurn), RpcTarget.AllBuffered, (byte)currentTurn);

            photonView.RPC(nameof(RPC_UnlockInput), RpcTarget.All, senderActor);
        }
        catch
        {
            photonView.RPC(nameof(RPC_UnlockInput), RpcTarget.All, senderActor);
        }
    }

    private bool CanActorPlayNow(int actor)
    {
        int masterActor = PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.ActorNumber : -1;

        if (currentTurn == PlayerAlliance.RED) return actor == masterActor;
        if (currentTurn == PlayerAlliance.BLACK) return actor != masterActor;
        return false;
    }

    private int FindDropRow(int column)
    {
        for (int r = GameBoard.NumRows - 1; r >= 0; r--)
        {
            if (GameBoard.Table[r, column] == Tile.EMPTY)
                return r;
        }
        return -1;
    }

    // ================================================================
    // RPCs
    // ================================================================
    [PunRPC]
    private void RPC_SyncTurn(byte turnB)
    {
        currentTurn = (turnB == 0) ? PlayerAlliance.RED : PlayerAlliance.BLACK;
        UpdateStatus();
        localClickLock = false;
    }

    [PunRPC]
    private void RPC_GameOverWithUILine(byte winnerB, float ax, float ay, float bx, float by)
    {
        ResolveUIRefs();
        EnsureWinLineImageExists();

        var winnerAlliance = (winnerB == 0) ? PlayerAlliance.RED : PlayerAlliance.BLACK;
        isGameOver = true;

        HideAllColumnHighlights();

        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(true);
            winnerText.text = (winnerAlliance == PlayerAlliance.RED) ? "RED WON" : "YELLOW WON";
            winnerText.ForceMeshUpdate(true, true);
            winnerText.transform.SetAsLastSibling();
        }

        SetStatus("Game Over");

        Vector2 a = new Vector2(ax, ay);
        Vector2 b = new Vector2(bx, by);
        ShowWinLineBetweenAnchors(a, b);

        localClickLock = false;
    }

    [PunRPC]
    private void RPC_UnlockInput(int senderActor)
    {
        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.ActorNumber == senderActor)
        {
            localClickLock = false;
        }
    }

    [PunRPC]
    private void RPC_SetColumnHighlight(int column, bool on)
    {
        if (columnHighlights == null || columnHighlights.Length != GameBoard.NumCols) return;

        if (!on)
        {
            if (column >= 0 && column < columnHighlights.Length && columnHighlights[column] != null)
                columnHighlights[column].gameObject.SetActive(false);
            return;
        }

        for (int i = 0; i < columnHighlights.Length; i++)
            if (columnHighlights[i] != null) columnHighlights[i].gameObject.SetActive(false);

        if (column >= 0 && column < columnHighlights.Length && columnHighlights[column] != null)
            columnHighlights[column].gameObject.SetActive(true);
    }

    [PunRPC]
    private void RPC_GlowWinningPieces(int[] rows, int[] cols)
    {
        if (rows == null || cols == null || rows.Length != cols.Length) return;

        for (int i = 0; i < rows.Length; i++)
        {
            int key = CellKey(rows[i], cols[i]);
            if (pieceMap.TryGetValue(key, out Connect4Piece piece) && piece != null)
            {
                piece.SetGlow(true);
            }
        }
    }

    public void ClearColumnHighlightLocal(int column)
    {
        if (isGameOver) return;
        if (columnHighlights == null || columnHighlights.Length != GameBoard.NumCols) return;
        if (column < 0 || column >= columnHighlights.Length) return;
        if (columnHighlights[column] == null) return;

        columnHighlights[column].gameObject.SetActive(false);
    }

    public string BlackPiecePrefabName;
    public string RedPiecePrefabName;

    // ================================================================
    // PIECE SPAWN (MASTER ONLY)
    // ================================================================
    private void SpawnPiece_MasterOnly(int column, int row, PlayerAlliance alliance)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Vector2 anchoredPosition = GetAnchoredPosForCell(row, column);
        string prefabName = (alliance == PlayerAlliance.BLACK) ? BlackPiecePrefabName : RedPiecePrefabName;

        PhotonView parentPhotonView = piecesParent.GetComponent<PhotonView>();
        int parentViewID = parentPhotonView != null ? parentPhotonView.ViewID : 0;

        PhotonNetwork.InstantiateRoomObject(
            prefabName,
            Vector3.zero,
            Quaternion.identity,
            0,
            new object[] { anchoredPosition.x, anchoredPosition.y, parentViewID, column, row }
        );
    }

    private Vector2 GetAnchoredPosForCell(int boardRow, int boardCol)
    {
        int uiRowIndex = GameBoard.NumRows - boardRow - 1;
        if (boardCol < columnRects.Length && uiRowIndex < rowRects.Length)
        {
            RectTransform col = columnRects[boardCol];
            RectTransform row = rowRects[uiRowIndex];
            return new Vector2(col.anchoredPosition.x, row.anchoredPosition.y);
        }
        return Vector2.zero;
    }

    // ================================================================
    // WIN LINE UI IMAGE
    // ================================================================
    private void EnsureWinLineImageExists()
    {
        if (winLineImage != null && winLineImgComponent != null) return;

        if (winLineImage == null)
        {
            GameObject go = new GameObject("WinLineImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(piecesParent, false);

            winLineImage = go.GetComponent<RectTransform>();
            winLineImgComponent = go.GetComponent<Image>();
            winLineImgComponent.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        else
        {
            winLineImgComponent = winLineImage.GetComponent<Image>();
            if (winLineImgComponent == null) winLineImgComponent = winLineImage.gameObject.AddComponent<Image>();
        }

        winLineImgComponent.color = winLineColor;
        winLineImgComponent.raycastTarget = false;

        winLineImage.anchorMin = new Vector2(0.5f, 0.5f);
        winLineImage.anchorMax = new Vector2(0.5f, 0.5f);
        winLineImage.pivot = new Vector2(0.5f, 0.5f);
        winLineImage.SetAsLastSibling();
    }

    private void HideWinLine()
    {
        if (winLineImage != null)
            winLineImage.gameObject.SetActive(false);
    }

    private void ShowWinLineBetweenAnchors(Vector2 a, Vector2 b)
    {
        if (winLineImage == null) return;

        winLineImage.gameObject.SetActive(true);
        winLineImage.SetAsLastSibling();

        Vector2 mid = (a + b) * 0.5f;
        float length = Vector2.Distance(a, b);
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

        winLineImage.anchoredPosition = mid;
        winLineImage.sizeDelta = new Vector2(length, winLineThickness);
        winLineImage.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ================================================================
    // COLUMN HIGHLIGHT SETUP
    // ================================================================
    private void EnsureColumnHighlightsExist()
    {
        if (boardSpaceRoot == null) return;

        if (columnHighlights != null && columnHighlights.Length == GameBoard.NumCols)
            return;

        columnHighlights = new Image[GameBoard.NumCols];

        for (int c = 0; c < GameBoard.NumCols; c++)
        {
            GameObject go = new GameObject($"ColHighlight_{c}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(boardSpaceRoot, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            Image img = go.GetComponent<Image>();

            img.color = columnHighlightColor;
            img.raycastTarget = false;

            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float width = (columnRects != null && columnRects.Length > c) ? Mathf.Max(60f, columnRects[c].rect.width) : 80f;
            rt.sizeDelta = new Vector2(width, 0f);

            float x = (columnRects != null && columnRects.Length > c) ? columnRects[c].anchoredPosition.x : 0f;
            rt.anchoredPosition = new Vector2(x, 0f);

            go.SetActive(false);
            columnHighlights[c] = img;
        }
    }

    private void HideAllColumnHighlights()
    {
        if (columnHighlights == null) return;
        for (int i = 0; i < columnHighlights.Length; i++)
            if (columnHighlights[i] != null)
                columnHighlights[i].gameObject.SetActive(false);
    }

    // ================================================================
    // ROOM PROPERTIES / STATUS
    // ================================================================
    private void EnsureRoomPropertiesExist()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_GAME_TYPE))
            SetGameTypeProperty(connectType);

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_READY))
            SetReadyProperty(false);

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_TURN))
            SetTurnProperty(PlayerAlliance.RED);

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_BOARD))
            SetBoardProperty();
    }
    
    private void SetGameTypeProperty(ConnectType type)
    {
        var props = new Hashtable();
        props[PROP_GAME_TYPE] = (byte)type;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void MasterUpdateReadyAndInitTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        bool ready = PhotonNetwork.CurrentRoom.PlayerCount >= 2;
        SetReadyProperty(ready);

        if (ready && !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_TURN))
            SetTurnProperty(PlayerAlliance.RED);
    }

    private bool IsRoomReady()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PROP_READY, out object val))
        {
            return (int)val == 1;
        }
        return false;
    }

    private void SetReadyProperty(bool ready)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        var props = new Hashtable();
        props[PROP_READY] = ready ? 1 : 0;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void SetTurnProperty(PlayerAlliance turn)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        var props = new Hashtable();
        props[PROP_TURN] = (turn == PlayerAlliance.RED) ? 0 : 1;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void ReadTurnFromRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PROP_TURN, out object val))
        {
            int t = (int)val;
            currentTurn = (t == 0) ? PlayerAlliance.RED : PlayerAlliance.BLACK;
        }
        else
        {
            currentTurn = PlayerAlliance.RED;
        }
    }

    private void SetBoardProperty()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder(GameBoard.NumRows * GameBoard.NumCols);

        for (int r = 0; r < GameBoard.NumRows; r++)
        {
            for (int c = 0; c < GameBoard.NumCols; c++)
            {
                Tile tile = GameBoard.Table[r, c];
                if (tile == Tile.EMPTY) sb.Append('0');
                else if (tile == Tile.RED) sb.Append('1');
                else sb.Append('2');
            }
        }

        var props = new Hashtable();
        props[PROP_BOARD] = sb.ToString();
        props["BOARD_ROWS"] = GameBoard.NumRows;
        props["BOARD_COLS"] = GameBoard.NumCols;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void LoadBoardFromProperty()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        // Check if game type matches
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PROP_GAME_TYPE, out object typeObj))
        {
            ConnectType storedType = (ConnectType)(byte)typeObj;
            if (storedType != connectType)
            {
                Debug.LogWarning($"Stored game type ({storedType}) doesn't match local ({connectType})");
                // Optionally: Force switch to stored type
                // connectType = storedType;
                // InitializeGame();
            }
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PROP_BOARD, out object val))
            return;

        string s = val as string;
        if (string.IsNullOrEmpty(s) || s.Length != GameBoard.NumRows * GameBoard.NumCols)
            return;

        int idx = 0;
        for (int r = 0; r < GameBoard.NumRows; r++)
        {
            for (int c = 0; c < GameBoard.NumCols; c++)
            {
                char ch = s[idx++];
                if (ch == '1') GameBoard.Table[r, c] = Tile.RED;
                else if (ch == '2') GameBoard.Table[r, c] = Tile.BLACK;
                else GameBoard.Table[r, c] = Tile.EMPTY;
            }
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null) return;

        if (propertiesThatChanged.ContainsKey(PROP_READY) || propertiesThatChanged.ContainsKey(PROP_TURN))
        {
            ReadTurnFromRoom();
            UpdateStatus();
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        else if (!string.IsNullOrEmpty(msg)) Debug.Log(msg);
    }

    private void UpdateStatus()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            SetStatus("Connecting...");
            return;
        }

        if (!IsRoomReady())
        {
            SetStatus("Waiting for opponent...");
            return;
        }

        if (isGameOver)
        {
            SetStatus("Game Over");
            return;
        }

        ReadTurnFromRoom();

        if (!CanLocalPlayerInput())
        {
            SetStatus("Opponent's turn");
            return;
        }

        SetStatus("Your turn");
    }

    private void UpdateUndoButtonState(bool enabled)
    {
        GameObject btn = GameObject.Find("UndoButton");
        if (btn != null)
        {
            Button undoButton = btn.GetComponent<Button>();
            if (undoButton != null) undoButton.interactable = enabled;
        }
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            EnsureRoomPropertiesExist();
            MasterUpdateReadyAndInitTurn();
        }

        ReadTurnFromRoom();
        LoadBoardFromProperty();
        UpdateStatus();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
            MasterUpdateReadyAndInitTurn();

        UpdateStatus();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
            MasterUpdateReadyAndInitTurn();

        SetStatus("Opponent left");
        localClickLock = false;
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            EnsureRoomPropertiesExist();
            MasterUpdateReadyAndInitTurn();
        }

        UpdateStatus();
    }
    
    // Helper methods
    private void ResolveUIRefs()
    {
        if (winnerText == null)
        {
            var go = GameObject.Find("WinnerText");
            if (go != null)
            {
                winnerText = go.GetComponent<TextMeshProUGUI>();
                if (winnerText == null) winnerText = go.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }
}