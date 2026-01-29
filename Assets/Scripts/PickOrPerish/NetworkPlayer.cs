using System;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayer : NetworkBehaviour
{
    private PlayerUISet _playerUISet;

    private NetworkVariable<int> currentNumber = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> currentScore = new NetworkVariable<int>(
        NetworkGameManager.defaultLives, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> playerActiveState = new NetworkVariable<int>(0); // 0 Active, 1 Eliminated

    private bool isSubmitted = false;
    private int lastSubmitterValue = 0;

    public int GetPlayerID() => (int)OwnerClientId;
    public int GetCurrentScore() => currentScore.Value;
    public PlayerState GetCurrentPlayerState() => (PlayerState)playerActiveState.Value;

    public void RegisterCurrentNumberValueChanged(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        currentNumber.OnValueChanged -= callback;
        currentNumber.OnValueChanged += callback;
    }

    public void RegisterCurrentScoreValueChanged(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        currentScore.OnValueChanged -= callback;
        currentScore.OnValueChanged += callback;
    }

    public void RegisterPlayerStateChanges(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        playerActiveState.OnValueChanged -= callback;
        playerActiveState.OnValueChanged += callback;
    }

    public override void OnNetworkSpawn()
    {
        name = "Player_" + OwnerClientId;

        _playerUISet = NetworkGameManager.Instance.PanelManager?.CreatePlayerUISet();
        if (_playerUISet != null)
            _playerUISet.SetPlayer(this);

        NetworkGameManager.Instance.RegisterOnCurrentRoundValueChanged(OnCurrentRoundChanged);
        NetworkGameManager.Instance.RegisterMeToTheMatch(this);

        NetworkGameManager.Instance.OnTimerEnd += OnTimerEndedForceSubmit;

        RegisterCurrentNumberValueChanged(OnCurrentNumberChanged);
    }

    public override void OnNetworkDespawn()
    {
        if (_playerUISet != null)
            Destroy(_playerUISet.gameObject);

        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.OnTimerEnd -= OnTimerEndedForceSubmit;
    }

    private void OnTimerEndedForceSubmit()
    {
        if (!IsOwner) return;
        if (playerActiveState.Value != 0) return;
        if (isSubmitted) return;

        _playerUISet.SetSubmitButtonInteractable(false);

        int value = _playerUISet.GetInputValue();
        if (value > 0)
        {
            SetCurrentNumber(value);
        }
        else
        {
            SetCurrentNumber(lastSubmitterValue);
        }
    }

    private void OnCurrentNumberChanged(int prev, int current)
    {
        if (current < 0 || !IsOwner) return;

        lastSubmitterValue = current;
        isSubmitted = true;

        SubmitCurrentNumberServerRPC();
    }

    private void OnCurrentRoundChanged(int prevRound, int currentRound)
    {
        if (IsOwner)
            SetCurrentNumber(-1);

        if (_playerUISet != null)
            _playerUISet.SetSubmitButtonInteractable(true);

        isSubmitted = false;
    }

    // ✅ Server-only score update (call this from NetworkGameManager on server)
    public void UpdateCurrentScore(int score)
    {
        if (!IsServer) return;

        Debug.Log($"Player_{GetPlayerID()}_New Score: {score}");

        currentScore.Value = score;

        if (currentScore.Value <= 0 && playerActiveState.Value == 0)
        {
            playerActiveState.Value = 1; // eliminated

            // ✅ Tell server game manager to stop timer + declare winner if only one left
            NetworkGameManager.Instance.NotifyPlayerEliminated();
        }
    }

    public void SetCurrentNumber(int number)
    {
        SetCurrentNumberServerRPC(number);
    }

    [ServerRpc]
    private void SetCurrentNumberServerRPC(int number)
    {
        currentNumber.Value = number;
    }

    [ServerRpc]
    private void SubmitCurrentNumberServerRPC()
    {
        NetworkGameManager.Instance.SubmittedNumber(GetPlayerID(), currentNumber.Value);
    }
}
