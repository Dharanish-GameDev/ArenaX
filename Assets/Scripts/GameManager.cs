using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public enum ConnectType : byte
{
    Connect4,
    Connect5
}

// ✅ Game modes (your 3 buttons)
public enum GameMode : byte
{
    VsAI,
    PassAndPlay,
    Multiplayer
}

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager instance;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private IEnumerator ShowEndScreenDelayed(PlayerAlliance winnerAlliance, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowEndScreen(winnerAlliance);
    }

    [SerializeField] private ConnectType connectType;

    // ✅ Mode selection
    [Header("Mode Select Panel")]
    [SerializeField] private GameObject modeSelectPanel;
    [SerializeField] private GameMode currentMode = GameMode.Multiplayer;

    // ✅ Local-mode prefabs (UI prefabs, NOT Photon prefabs)
    [Header("Local Mode Piece Prefabs (UI)")]
    [Header("Panels")]
    [SerializeField] private GameObject bgPanel;   // ✅ assign BG panel here

    [SerializeField] private GameObject localRedPiecePrefab;
    [SerializeField] private GameObject localYellowPiecePrefab;

    [Header("Local Drop Settings")]
    [SerializeField] private float localDropSpeed = 1200f;
    [SerializeField] private float localDropStartPadding = 120f;

    // ✅ Local coroutine safety (fixes MissingReferenceException)
    private Coroutine localMoveCoroutine;
    private int localRunToken = 0; // increment to cancel old coroutines immediately

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

    [Header("End Screen")]
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private Image endScreenImage;
    [SerializeField] private Sprite youWinSprite;
    [SerializeField] private Sprite youLostSprite;

    [Header("Turn Indicator Discs (UI)")]
    [SerializeField] private Image redTurnDisc;
    [SerializeField] private Image yellowTurnDisc;
    [SerializeField] private Outline redTurnOutline;
    [SerializeField] private Outline yellowTurnOutline;

    [Header("Turn Glow Settings")]
    [SerializeField] private float onAlpha = 1f;
    [SerializeField] private float offAlpha = 0.35f;
    [SerializeField] private Vector2 glowOutlineSize = new Vector2(10f, 10f);
    [SerializeField] private Vector2 offOutlineSize = new Vector2(0f, 0f);

    private RectTransform boardSpaceRoot;
    private bool isGameOver;
    private PlayerAlliance currentTurn = PlayerAlliance.RED;
    private bool localClickLock;

    private const string PROP_TURN = "TURN";
    private const string PROP_BOARD = "BOARD";
    private const string PROP_READY = "READY";
    private const string PROP_GAME_TYPE = "GAME_TYPE";
    private const string PROP_RESTART_MASK = "RESTART_MASK";

    private readonly Dictionary<int, Connect4Piece> pieceMap = new Dictionary<int, Connect4Piece>();
    private readonly HashSet<int> pendingGlowKeys = new HashSet<int>();

    private int CellKey(int r, int c) => (r * 1000) + c;

    public void RegisterPiece(int r, int c, Connect4Piece piece)
    {
        if (piece == null) return;

        int key = CellKey(r, c);
        pieceMap[key] = piece;

        if (pendingGlowKeys.Contains(key))
            piece.SetGlow(true);
    }

    // ================================================================
    // TURN INDICATOR
    // ================================================================
    private void UpdateTurnIndicatorUI()
    {
        if (isGameOver)
        {
            SetTurnDiscState(redTurnDisc, redTurnOutline, false);
            SetTurnDiscState(yellowTurnDisc, yellowTurnOutline, false);
            return;
        }

        bool redOn = (currentTurn == PlayerAlliance.RED);
        bool yellowOn = (currentTurn == PlayerAlliance.BLACK);

        SetTurnDiscState(redTurnDisc, redTurnOutline, redOn);
        SetTurnDiscState(yellowTurnDisc, yellowTurnOutline, yellowOn);
    }

    private void SetTurnDiscState(Image disc, Outline outline, bool on)
    {
        if (disc != null)
        {
            Color c = disc.color;
            c.a = on ? onAlpha : offAlpha;
            disc.color = c;
        }

        if (outline != null)
        {
            outline.enabled = on;
            outline.effectDistance = on ? glowOutlineSize : offOutlineSize;
        }
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

        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(true);
            winnerText.text = "";
        }

        if (modeSelectPanel != null)
            modeSelectPanel.SetActive(true);

        // If we entered this scene while already in a room (multiplayer flow)
        if (currentMode == GameMode.Multiplayer && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                EnsureRoomPropertiesExist();
                MasterUpdateReadyAndInitTurn();
            }

            ReadTurnFromRoom();
            LoadBoardFromProperty();
            
            // Make sure game over state is false initially
            isGameOver = false;
            if (endScreenPanel != null) endScreenPanel.SetActive(false);
        }

        UpdateTurnIndicatorUI();
        UpdateStatus();
        UpdateUndoButtonState(false);
        if (modeSelectPanel != null) modeSelectPanel.SetActive(true);
        if (bgPanel != null) bgPanel.SetActive(false);   // ✅ hide bg until mode chosen
    }

    private void InitializeGame()
    {
        currentGame = connectType switch
        {
            ConnectType.Connect4 => new Connect4Game(),
            ConnectType.Connect5 => new Connect5Game(),
            _ => new Connect4Game()
        };

        GameBoard = currentGame.CreateBoard();

        isGameOver = false;
        localClickLock = false;

        pieceMap.Clear();
        pendingGlowKeys.Clear();

        if (rowRects != null && rowRects.Length != GameBoard.NumRows)
            Debug.LogWarning($"Row rects count ({rowRects.Length}) doesn't match board rows ({GameBoard.NumRows})");

        if (columnRects != null && columnRects.Length != GameBoard.NumCols)
            Debug.LogWarning($"Column rects count ({columnRects.Length}) doesn't match board cols ({GameBoard.NumCols})");

        if (piecesParent != null)
        {
            Connect4Piece[] spawnedPieces = piecesParent.GetComponentsInChildren<Connect4Piece>();
            for (int i = 0; i < spawnedPieces.Length; i++)
            {
                if (currentMode != GameMode.Multiplayer)
                    Destroy(spawnedPieces[i].gameObject);
            }
        }
    }

    // ================================================================
    // MODE BUTTON HOOKS
    // ================================================================
    public void OnMode_VsAI()
    {
        currentMode = GameMode.VsAI;
        BeginLocalMode();
    }

    public void OnMode_PassAndPlay()
    {
        currentMode = GameMode.PassAndPlay;
        BeginLocalMode();
    }

    public void OnMode_Multiplayer()
    {
        currentMode = GameMode.Multiplayer;

        // cancel any local routines
        CancelLocalCoroutines();

        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);

        UpdateTurnIndicatorUI();
        UpdateStatus();
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
        if (bgPanel != null) bgPanel.SetActive(true);
    }

    private void BeginLocalMode()
    {
        // ✅ cancel any running local drop coroutine BEFORE destroying UI
        CancelLocalCoroutines();

        // leave room if needed (async)
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        InitializeGame();
        ClearAllPiecesLocalUI();

        HideAllColumnHighlights();
        HideWinLine();
        if (winnerText != null) winnerText.text = "";
        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        currentTurn = PlayerAlliance.RED;
        isGameOver = false;
        localClickLock = false;

        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);

        UpdateTurnIndicatorUI();
        UpdateStatus();
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
        if (bgPanel != null) bgPanel.SetActive(true);   // ✅ show bg panel now
    }

    private void CancelLocalCoroutines()
    {
        // ✅ invalidate all old coroutines immediately
        localRunToken++;

        if (localMoveCoroutine != null)
        {
            StopCoroutine(localMoveCoroutine);
            localMoveCoroutine = null;
        }

        localClickLock = false;
    }

    private void ClearAllPiecesLocalUI()
    {
        // if (piecesParent == null) return;
        //
        // for (int i = piecesParent.childCount - 1; i >= 0; i--)
        // {
        //     Transform ch = piecesParent.GetChild(i);
        //     if (!ch) continue;
        //
        //     // Keep WinLineImage if it exists under piecesParent
        //     if (winLineImage != null && ch == winLineImage.transform) continue;
        //
        //     // If it has PhotonView, don't destroy here (Photon handles it)
        //     if (ch.GetComponent<PhotonView>() != null) continue;
        //
        //     Destroy(ch.gameObject);
        // }
    }

    // ================================================================
    // INPUT
    // ================================================================
    public void MakeMove(int column)
    {
        if (localClickLock) return;
        if (isGameOver) return;

        // ✅ LOCAL MODES
        if (currentMode == GameMode.VsAI || currentMode == GameMode.PassAndPlay)
        {
            if (localMoveCoroutine != null) StopCoroutine(localMoveCoroutine);
            localMoveCoroutine = StartCoroutine(LocalMakeMoveRoutine(column, ++localRunToken));
            return;
        }

        // ✅ MULTIPLAYER
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

        if (column < 0 || column >= GameBoard.NumCols) return;

        if (photonView == null || photonView.ViewID == 0)
        {
            Debug.LogError("[GM] PhotonView missing or ViewID=0. Add PhotonView + assign Scene View ID!");
            return;
        }

        localClickLock = true;
        photonView.RPC(nameof(RPC_RequestMove), RpcTarget.MasterClient, column, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    private IEnumerator LocalMakeMoveRoutine(int column, int runToken)
    {
        if (runToken != localRunToken) yield break;
        if (column < 0 || column >= GameBoard.NumCols) yield break;

        int dropRow = FindDropRow(column);
        if (dropRow < 0) yield break;

        localClickLock = true;

        // Place piece on board
        GameBoard.SetPiece(column, currentTurn);

        // Column highlight
        RPC_SetColumnHighlight(column, true);

        // Spawn local UI piece + animate drop
        yield return StartCoroutine(SpawnPiece_Local(column, dropRow, currentTurn, runToken));

        if (runToken != localRunToken) yield break; // game reset while dropping

        // After it lands, clear highlight
        ClearColumnHighlightLocal(column);

        // Check win
        if (currentGame.IsGameFinished(GameBoard))
        {
            isGameOver = true;

            var winCells = currentGame.GetWinningCells(GameBoard);

            if (winnerText != null)
            {
                winnerText.gameObject.SetActive(true);
                winnerText.text = (currentTurn == PlayerAlliance.RED) ? "RED WON" : "YELLOW WON";
                winnerText.ForceMeshUpdate(true, true);
                winnerText.transform.SetAsLastSibling();
            }

            Vector2 aAnch = GetAnchoredPosForCell(winCells[0].x, winCells[0].y);
            Vector2 bAnch = GetAnchoredPosForCell(winCells[winCells.Count - 1].x, winCells[winCells.Count - 1].y);
            ShowWinLineBetweenAnchors(aAnch, bAnch);

            SetStatus("Game Over");
            UpdateTurnIndicatorUI();

            if (endScreenPanel != null)
            {
                if (currentMode == GameMode.VsAI)
                {
                    bool didHumanWin = (currentTurn == PlayerAlliance.RED);
                    if (endScreenImage != null)
                    {
                        endScreenImage.sprite = didHumanWin ? youWinSprite : youLostSprite;
                        endScreenImage.preserveAspect = true;
                    }
                }
                endScreenPanel.SetActive(true);
            }

            localClickLock = false;
            yield break;
        }

        // Switch turn
        currentTurn = (currentTurn == PlayerAlliance.RED) ? PlayerAlliance.BLACK : PlayerAlliance.RED;
        UpdateTurnIndicatorUI();
        UpdateStatus();

        localClickLock = false;

        // ✅ If VsAI, let AI play as YELLOW
        if (currentMode == GameMode.VsAI && !isGameOver && currentTurn == PlayerAlliance.BLACK)
        {
            yield return new WaitForSeconds(0.15f);
            if (runToken != localRunToken) yield break;

            int aiCol = GetAIMoveColumn();
            if (aiCol >= 0)
            {
                // start AI move as a new "run" (still guarded)
                if (localMoveCoroutine != null) StopCoroutine(localMoveCoroutine);
                localMoveCoroutine = StartCoroutine(LocalMakeMoveRoutine(aiCol, localRunToken));
            }
        }
    }

    private IEnumerator SpawnPiece_Local(int column, int row, PlayerAlliance alliance, int runToken)
    {
        if (runToken != localRunToken) yield break;
        if (!piecesParent) yield break;

        GameObject prefab = (alliance == PlayerAlliance.RED) ? localRedPiecePrefab : localYellowPiecePrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[GM] Local piece prefab missing. Assign localRedPiecePrefab/localYellowPiecePrefab.");
            yield break;
        }

        GameObject go = Instantiate(prefab, piecesParent);
        if (!go) yield break;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        Vector2 target = GetAnchoredPosForCell(row, column);
        float startY = GetLocalDropStartHeight();

        if (!rt) yield break;
        rt.anchoredPosition = new Vector2(target.x, startY);
        go.transform.SetAsLastSibling();

        while (true)
        {
            // ✅ IMPORTANT: stops MissingReferenceException if object got destroyed mid-drop
            if (runToken != localRunToken) yield break;
            if (!rt) yield break;

            Vector2 cur = rt.anchoredPosition;
            if ((cur - target).sqrMagnitude <= 0.25f) break;

            rt.anchoredPosition = Vector2.MoveTowards(cur, target, localDropSpeed * Time.deltaTime);
            yield return null;
        }

        if (rt) rt.anchoredPosition = target;
    }

    private float GetLocalDropStartHeight()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRT = canvas.transform as RectTransform;
            if (canvasRT != null) return (canvasRT.rect.height * 0.5f) + localDropStartPadding;
            return (canvas.pixelRect.height * 0.5f) + localDropStartPadding;
        }

        RectTransform parent = piecesParent;
        if (parent != null) return (parent.rect.height * 0.5f) + localDropStartPadding;
        return 600f;
    }

    // ================================================================
    // SIMPLE AI
    // ================================================================
    private int GetAIMoveColumn()
    {
        int winCol = FindWinningMoveFor(PlayerAlliance.BLACK);
        if (winCol >= 0) return winCol;

        int blockCol = FindWinningMoveFor(PlayerAlliance.RED);
        if (blockCol >= 0) return blockCol;

        List<int> valid = new List<int>();
        for (int c = 0; c < GameBoard.NumCols; c++)
            if (FindDropRow(c) >= 0) valid.Add(c);

        if (valid.Count == 0) return -1;
        return valid[Random.Range(0, valid.Count)];
    }

    private int FindWinningMoveFor(PlayerAlliance who)
    {
        string backup = SerializeBoardToString();

        for (int c = 0; c < GameBoard.NumCols; c++)
        {
            if (FindDropRow(c) < 0) continue;

            GameBoard.SetPiece(c, who);
            bool wins = currentGame.IsGameFinished(GameBoard);

            LoadBoardFromString(backup);

            if (wins) return c;
        }
        return -1;
    }

    private string SerializeBoardToString()
    {
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
        return sb.ToString();
    }

    private void LoadBoardFromString(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length != GameBoard.NumRows * GameBoard.NumCols) return;

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

    // ================================================================
    // MULTIPLAYER INPUT CHECK
    // ================================================================
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
    // MASTER AUTHORITATIVE MOVE (MULTIPLAYER)
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

            if (currentGame.IsGameFinished(GameBoard))
            {
                isGameOver = true;

                var winCells = currentGame.GetWinningCells(GameBoard);

                int[] rows = new int[winCells.Count];
                int[] cols = new int[winCells.Count];
                for (int i = 0; i < winCells.Count; i++)
                {
                    rows[i] = winCells[i].x;
                    cols[i] = winCells[i].y;
                }
                photonView.RPC(nameof(RPC_GlowWinningPieces), RpcTarget.AllBuffered, rows, cols);

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
            if (GameBoard.Table[r, column] == Tile.EMPTY)
                return r;
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
        UpdateTurnIndicatorUI();
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

        UpdateTurnIndicatorUI();

        StartCoroutine(ShowEndScreenDelayed(winnerAlliance, 2f));
        localClickLock = false;
    }

    // NEW: RPC to reset game state for all clients
    [PunRPC]
    private void RPC_ResetGameState()
    {
        isGameOver = false;
        localClickLock = false;
        currentTurn = PlayerAlliance.RED;

        pieceMap.Clear();
        pendingGlowKeys.Clear();

        // Clear UI elements
        HideWinLine();
        HideAllColumnHighlights();
        if (winnerText != null) winnerText.text = "";
        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        // Re-initialize the game board
        InitializeGame();

        UpdateTurnIndicatorUI();
        UpdateStatus();
    }

    public void OnHomePressed()
    {
        // ✅ cancel local routines to avoid MissingReferenceException during scene change
        CancelLocalCoroutines();

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.RemoveRPCs(photonView);
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            LoadHomeScene();
        }
    }

    public override void OnLeftRoom()
    {
        // If we left because we picked local mode, do NOT force home.
        if (currentMode == GameMode.VsAI || currentMode == GameMode.PassAndPlay) return;
        LoadHomeScene();
    }

    private void LoadHomeScene()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void ShowEndScreen(PlayerAlliance winnerAlliance)
    {
        if (endScreenPanel == null || endScreenImage == null) return;

        PlayerAlliance localAlliance = PhotonNetwork.IsMasterClient ? PlayerAlliance.RED : PlayerAlliance.BLACK;
        bool didLocalWin = (winnerAlliance == localAlliance);

        endScreenImage.sprite = didLocalWin ? youWinSprite : youLostSprite;
        endScreenImage.preserveAspect = true;

        endScreenPanel.SetActive(true);
    }

    public void OnPlayAgainPressed()
    {
        // Local modes: restart locally
        if (currentMode == GameMode.VsAI || currentMode == GameMode.PassAndPlay)
        {
            BeginLocalMode();
            return;
        }

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            SetStatus("Not in a room");
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            SetStatus("Waiting for opponent...");
            // Reset the end screen if opponent left
            if (endScreenPanel != null) endScreenPanel.SetActive(false);
            return;
        }

        SetRestartPressedBitLocal();
        SetStatus("Waiting for both players...");

        // Immediately hide the end screen
        if (endScreenPanel != null) endScreenPanel.SetActive(false);
    }

    private void SetRestartPressedBitLocal()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        int myBit = PhotonNetwork.IsMasterClient ? 1 : 2;

        int currentMask = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PROP_RESTART_MASK, out object val) &&
            val is int vInt)
        {
            currentMask = vInt;
        }

        int newMask = currentMask | myBit;

        var props = new Hashtable();
        props[PROP_RESTART_MASK] = newMask;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        if (PhotonNetwork.IsMasterClient && newMask == 3)
            RestartMatch_MasterOnly();
    }

    private void RestartMatch_MasterOnly()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom == null) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2) return;

        // Reset the room properties first
        ResetRoomState_MasterOnly();

        // Then destroy pieces
        DestroyAllSpawnedPieces_MasterOnly();

        // Call RPC to reset all clients' state
        photonView.RPC(nameof(RPC_ResetGameState), RpcTarget.AllBuffered);

        // Clear restart mask
        var props = new Hashtable();
        props[PROP_RESTART_MASK] = 0;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void ResetRoomState_MasterOnly()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        isGameOver = false;
        localClickLock = false;
        currentTurn = PlayerAlliance.RED;

        InitializeGame();

        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        HideWinLine();
        if (winnerText != null) winnerText.text = "";

        var props = new Hashtable();
        props[PROP_READY] = (PhotonNetwork.CurrentRoom.PlayerCount >= 2) ? 1 : 0;
        props[PROP_TURN] = 0;
        props[PROP_RESTART_MASK] = 0;

        System.Text.StringBuilder sb = new System.Text.StringBuilder(GameBoard.NumRows * GameBoard.NumCols);
        for (int r = 0; r < GameBoard.NumRows; r++)
            for (int c = 0; c < GameBoard.NumCols; c++)
                sb.Append('0');

        props[PROP_BOARD] = sb.ToString();
        props["BOARD_ROWS"] = GameBoard.NumRows;
        props["BOARD_COLS"] = GameBoard.NumCols;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        HideAllColumnHighlights();
        UpdateTurnIndicatorUI();
    }

    [PunRPC]
    private void RPC_ReloadScene()
    {
        isGameOver = false;
        localClickLock = false;

        pieceMap.Clear();
        pendingGlowKeys.Clear();

        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        HideWinLine();
        if (winnerText != null) winnerText.text = "";

        currentTurn = PlayerAlliance.RED;
        UpdateTurnIndicatorUI();

        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
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

        pendingGlowKeys.Clear();

        for (int i = 0; i < rows.Length; i++)
        {
            int r = rows[i];
            int c = cols[i];

            int keyRC = CellKey(r, c);
            pendingGlowKeys.Add(keyRC);

            if (pieceMap.TryGetValue(keyRC, out Connect4Piece p1) && p1 != null)
                p1.SetGlow(true);
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

    private void SpawnPiece_MasterOnly(int column, int row, PlayerAlliance alliance)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Vector2 anchoredPosition = GetAnchoredPosForCell(row, column);
        string prefabName = (alliance == PlayerAlliance.BLACK) ? BlackPiecePrefabName : RedPiecePrefabName;

        PhotonView parentPhotonView = piecesParent != null ? piecesParent.GetComponent<PhotonView>() : null;
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
        if (piecesParent == null || columnRects == null || rowRects == null) return Vector2.zero;
        if (boardCol < 0 || boardCol >= columnRects.Length) return Vector2.zero;

        int uiRowIndex = GameBoard.NumRows - boardRow - 1;
        if (uiRowIndex < 0 || uiRowIndex >= rowRects.Length) return Vector2.zero;

        RectTransform colRT = columnRects[boardCol];
        RectTransform rowRT = rowRects[uiRowIndex];
        if (colRT == null || rowRT == null) return Vector2.zero;

        // ✅ Use world positions (stable across different parents)
        Vector3 worldPoint = new Vector3(colRT.position.x, rowRT.position.y, 0f);

        Canvas canvas = piecesParent.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        // Convert world -> screen -> local point in piecesParent
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPoint);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                piecesParent, screenPoint, cam, out Vector2 localPoint))
        {
            return localPoint;
        }

        return Vector2.zero;
    }


    private void EnsureWinLineImageExists()
    {
        if (winLineImage != null && winLineImgComponent != null) return;

        if (winLineImage == null)
        {
            GameObject go = new GameObject("WinLineImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (piecesParent != null) go.transform.SetParent(piecesParent, false);

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

    private void EnsureColumnHighlightsExist()
    {
        if (boardSpaceRoot == null || GameBoard == null) return;

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

            float width = (columnRects != null && columnRects.Length > c && columnRects[c] != null)
                ? Mathf.Max(60f, columnRects[c].rect.width)
                : 80f;

            rt.sizeDelta = new Vector2(width, 0f);

            float x = (columnRects != null && columnRects.Length > c && columnRects[c] != null)
                ? columnRects[c].anchoredPosition.x
                : 0f;

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
    // ROOM PROPERTIES / STATUS (MULTIPLAYER)
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

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_RESTART_MASK))
        {
            var p = new Hashtable();
            p[PROP_RESTART_MASK] = 0;
            PhotonNetwork.CurrentRoom.SetCustomProperties(p);
        }
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

        UpdateTurnIndicatorUI();
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
           UpdateTurnIndicatorUI();
       }
   
       if (propertiesThatChanged.ContainsKey(PROP_BOARD))
       {
           // Clear local piece map when board is reset
           if (propertiesThatChanged[PROP_BOARD] is string boardStr &&
               boardStr.All(c => c == '0'))
           {
               pieceMap.Clear();
               pendingGlowKeys.Clear();
           }
           LoadBoardFromProperty();
       }
   
       if (propertiesThatChanged.ContainsKey(PROP_RESTART_MASK))
       {
           if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null &&
               PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PROP_RESTART_MASK, out object v) &&
               v is int maskValue && maskValue == 3)
           {
               RestartMatch_MasterOnly();
           }
           else if (!PhotonNetwork.IsMasterClient)
           {
               // Non-master clients should also reset their state when restart mask changes
               var restartMaskValue = PhotonNetwork.CurrentRoom.CustomProperties[PROP_RESTART_MASK] as int?;
               if (restartMaskValue == 0)
               {
                   // Reset local state when mask is cleared
                   isGameOver = false;
                   localClickLock = false;
                   if (endScreenPanel != null) endScreenPanel.SetActive(false);
                   UpdateStatus();
               }
           }
       }
   }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        else if (!string.IsNullOrEmpty(msg)) Debug.Log(msg);
    }

    private void DestroyAllSpawnedPieces_MasterOnly()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Connect4Piece[] allPieces = FindObjectsOfType<Connect4Piece>(true);

        for (int i = 0; i < allPieces.Length; i++)
        {
            if (allPieces[i] == null) continue;

            PhotonView pv = allPieces[i].GetComponent<PhotonView>();

            if (pv != null && pv.IsRoomView)
                PhotonNetwork.Destroy(pv);
            else
                Destroy(allPieces[i].gameObject);
        }
    }

    private void UpdateStatus()
    {
        // ✅ LOCAL MODES
        if (currentMode == GameMode.VsAI || currentMode == GameMode.PassAndPlay)
        {
            if (isGameOver)
            {
                SetStatus("Game Over");
                return;
            }

            if (currentMode == GameMode.VsAI)
                SetStatus(currentTurn == PlayerAlliance.RED ? "Your turn" : "AI thinking...");
            else
                SetStatus(currentTurn == PlayerAlliance.RED ? "RED's turn" : "YELLOW's turn");

            return;
        }

        // ✅ MULTIPLAYER
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
        if (currentMode != GameMode.Multiplayer) return;

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureRoomPropertiesExist();
            MasterUpdateReadyAndInitTurn();
        }

        ReadTurnFromRoom();
        LoadBoardFromProperty();
        
        // Ensure initial state
        isGameOver = false;
        if (endScreenPanel != null) endScreenPanel.SetActive(false);
        
        UpdateStatus();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (currentMode != GameMode.Multiplayer) return;

        if (PhotonNetwork.IsMasterClient)
            MasterUpdateReadyAndInitTurn();

        UpdateStatus();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (currentMode != GameMode.Multiplayer) return;

        if (PhotonNetwork.IsMasterClient)
        {
            MasterUpdateReadyAndInitTurn();

            if (PhotonNetwork.CurrentRoom != null)
            {
                var p = new Hashtable();
                p[PROP_RESTART_MASK] = 0;
                PhotonNetwork.CurrentRoom.SetCustomProperties(p);
            }
        }

        SetStatus("Opponent left");
        localClickLock = false;
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (currentMode != GameMode.Multiplayer) return;

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