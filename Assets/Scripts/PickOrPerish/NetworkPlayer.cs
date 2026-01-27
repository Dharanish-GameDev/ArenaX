using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer LocalInstance { get; private set; }

    // ---- NetworkVariables ----
    private readonly NetworkVariable<int> playerId = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> currentScore = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> currentNumber = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<PlayerState> playerState = new NetworkVariable<PlayerState>(
        PlayerState.Active, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ✅ Lives (starts at 3)
    private readonly NetworkVariable<int> lives = new NetworkVariable<int>(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool uiSpawned;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
            LocalInstance = this;

        if (IsServer)
        {
            playerId.Value = (int)OwnerClientId;

            // ✅ Always start with 3 lives
            lives.Value = 3;

            if (NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.RegisterMeToTheMatch(this);
        }

        // ✅ Create Player UI on ALL clients once ready
        StartCoroutine(SpawnUIWhenReady());
    }

    private IEnumerator SpawnUIWhenReady()
    {
        // wait for manager + panel
        while (NetworkGameManager.Instance == null || NetworkGameManager.Instance.PanelManager == null)
            yield return null;

        // wait until playerId is assigned by server
        while (GetPlayerID() < 0)
            yield return null;

        if (uiSpawned) yield break;
        uiSpawned = true;

        var ui = NetworkGameManager.Instance.PanelManager.CreatePlayerUISet();
        if (ui != null)
            ui.SetPlayer(this);
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
            LocalInstance = null;
    }

    // -------------------- GETTERS --------------------
    public int GetPlayerID() => playerId.Value;
    public int GetCurrentScore() => currentScore.Value;
    public int GetCurrentNumber() => currentNumber.Value;
    public PlayerState GetCurrentPlayerState() => playerState.Value;
    public int GetLives() => lives.Value;

    // -------------------- SCORE --------------------
    public void UpdateCurrentScore(int newScore)
    {
        if (IsServer) currentScore.Value = newScore;
        else SetScoreServerRpc(newScore);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetScoreServerRpc(int newScore)
    {
        currentScore.Value = newScore;
    }

    // -------------------- NUMBER --------------------
    public void SetCurrentNumber(int number)
    {
        if (IsServer) currentNumber.Value = number;
        else SetNumberServerRpc(number);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetNumberServerRpc(int number)
    {
        currentNumber.Value = number;
    }

    // -------------------- LIVES --------------------
    public void SetLives(int v)
    {
        if (IsServer) lives.Value = v;
        else SetLivesServerRpc(v);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetLivesServerRpc(int v)
    {
        lives.Value = v;
    }

    // -------------------- STATE --------------------
    public void SetPlayerState(PlayerState newState)
    {
        if (IsServer) playerState.Value = newState;
        else SetStateServerRpc(newState);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetStateServerRpc(PlayerState newState)
    {
        playerState.Value = newState;
    }

    // -------------------- UI REGISTRATION HELPERS --------------------
    public void RegisterCurrentScoreValueChanged(NetworkVariable<int>.OnValueChangedDelegate onValueChanged)
    {
        currentScore.OnValueChanged -= onValueChanged;
        currentScore.OnValueChanged += onValueChanged;
    }

    public void RegisterCurrentNumberValueChanged(NetworkVariable<int>.OnValueChangedDelegate onValueChanged)
    {
        currentNumber.OnValueChanged -= onValueChanged;
        currentNumber.OnValueChanged += onValueChanged;
    }

    public void RegisterPlayerStateChanges(NetworkVariable<PlayerState>.OnValueChangedDelegate onValueChanged)
    {
        playerState.OnValueChanged -= onValueChanged;
        playerState.OnValueChanged += onValueChanged;
    }

    public void RegisterLivesValueChanged(NetworkVariable<int>.OnValueChangedDelegate onValueChanged)
    {
        lives.OnValueChanged -= onValueChanged;
        lives.OnValueChanged += onValueChanged;
    }
}
