using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkConnectionHandler : MonoBehaviour
{
    private int MaxPlayersForQuickMatch = 4;
    public event Action OnNetworkJoined;
    public void StartAsHost()
    {
        NetworkManager.Singleton.StartHost();
        OnNetworkJoined?.Invoke();
    }

    public void StartAsClient()
    {
       NetworkManager.Singleton.StartClient();
       OnNetworkJoined?.Invoke();
    }

    public void JoinQuickMatch()
    {
        LobbyManager.Instance.StartQuickMatch(MaxPlayersForQuickMatch,1000000);
    }

    public void SetMaxPlayersCount(int count)
    {
        MaxPlayersForQuickMatch = count;
    }
}
