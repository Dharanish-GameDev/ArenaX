using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkPlayer : NetworkBehaviour
{
    private PlayerUISet _playerUISet;
    
    // NetworkVariables
    private NetworkVariable<int> currentNumber = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> currentScore = new NetworkVariable<int>(NetworkGameManager.defaultLives, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> playerActiveState = new NetworkVariable<int>(0); // 0 is Active, 1 - Eliminated, 2 - Disconnected

    private bool isSubmitted = false;
    
    public int GetPlayerID() => (int)OwnerClientId;
    
    public string GetBackendUserId()
    {
        if (_playerUISet != null)
        {
            return _playerUISet.UID;
        }
        return "";
    }

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
    
    public void ForceSetScore(int score)
    {
        if (!IsServer) return;
        currentScore.Value = score;
    }

    public int GetCurrentScore()
    {
        return currentScore.Value;
    }

    public PlayerState GetCurrentPlayerState()
    {
        return (PlayerState)playerActiveState.Value;
    }
    
    public void SetPlayerState(PlayerState newState)
    {
        if (!IsServer) 
        {
            Debug.LogError("SetPlayerState called on non-server!");
            return;
        }
        
        Debug.Log($"Player {GetPlayerID()} state changing from {(PlayerState)playerActiveState.Value} to {newState}");
        playerActiveState.Value = (int)newState;
    }
    
    public bool IsConnected()
    {
        return NetworkManager.Singleton.ConnectedClients.ContainsKey(OwnerClientId);
    }

    #region Networking Methods

    public override void OnNetworkSpawn()
    {
        // Need to Instantiate UI Set
        name = "Player_" + OwnerClientId.ToString();
        _playerUISet = NetworkGameManager.Instance.PanelManager?.CreatePlayerUISet();
        if (_playerUISet != null)
        {
            _playerUISet.SetPlayer(this);
        }
        NetworkGameManager.Instance.RegisterOnCurrentRoundValueChanged(OnCurrentRoundChanged);
        NetworkGameManager.Instance.RegisterMeToTheMatch(this);
        NetworkGameManager.Instance.OnTimerEnd += () =>
        {
            if (!isSubmitted && playerActiveState.Value == 0 && IsOwner) // Means Still Playing And Not Submitted
            {
                // Making it Force Submit
                _playerUISet.SetSubmitButtonInteractable(false);
                int value = _playerUISet.GetInputValue();
                if (value > 0)
                {
                    SetCurrentNumber(value);
                    Debug.Log("Force Submitting");
                }
                else
                {
                    Debug.Log("Submitting Previous Number : " + lastSubmitterValue);
                    SetCurrentNumber(lastSubmitterValue);
                }
            }
        };
        RegisterCurrentNumberValueChanged(OnCurrentNumberChanged);
        RegisterCurrentScoreValueChanged(OnCurrentScoreChanged);
    }

    public override void OnNetworkDespawn()
    {
        // Need to Destroy the UI Set
        if (_playerUISet != null)
        {
            Destroy(_playerUISet.gameObject);
        }
    }

    int lastSubmitterValue = 0;
    
    private void OnCurrentNumberChanged(int prev, int current)
    {
        if (current < 0 || !IsOwner) return;
        lastSubmitterValue = current;
        isSubmitted = true;
        SubmitCurrentNumberServerRPC();
    }
    
    private void OnCurrentScoreChanged(int prev, int current)
    {
        // This will trigger on all clients when score changes
        Debug.Log($"Player {GetPlayerID()} score changed from {prev} to {current}");
        
        if (IsServer && current <= 0 && playerActiveState.Value == 0)
        {
            // Server eliminates player when score reaches 0
            Debug.Log($"Player {GetPlayerID()} eliminated due to score {current}");
            playerActiveState.Value = 1; // Eliminated
        }
        
        // // Update UI if needed
        // if (_playerUISet != null)
        // {
        //     _playerUISet.UpdateScore(current);
        // }
    }
    
    private void OnCurrentRoundChanged(int prevRound, int currentRound)
    {
        if (IsOwner)
        {
            SetCurrentNumber(-1); // Means To Clear it Up
        }
        if (_playerUISet != null)
        {
            // Only enable submit button if player is still active
            _playerUISet.SetSubmitButtonInteractable(playerActiveState.Value == 0);
        }
        isSubmitted = false;
    }
    
    public void UpdateCurrentScore(int score)
    {
        if (!IsServer) 
        {
            Debug.LogError("UpdateCurrentScore called on non-server!");
            return;
        }
        
        Debug.Log($"Player_{GetPlayerID()}_New Score: {score}");
        currentScore.Value = score;
        
        // Check for elimination immediately
        if (currentScore.Value <= 0 && playerActiveState.Value == 0)
        {
            Debug.Log($"Player {GetPlayerID()} eliminated immediately after score update");
            playerActiveState.Value = 1; // Eliminated
        }
    }
    
    // RPC Methods
    public void SetCurrentNumber(int number)
    {
        SetCurrentNumberServerRPC(number);
    }
    
    [ServerRpc]
    private void SetCurrentNumberServerRPC(int number)
    {
        if (IsServer)
        {
            currentNumber.Value = number;
        }
    }
    
    [ServerRpc]
    public void SubmitCurrentNumberServerRPC()
    {
        NetworkGameManager.Instance.SubmittedNumber(GetPlayerID(), currentNumber.Value);
    }
    
    #endregion
}