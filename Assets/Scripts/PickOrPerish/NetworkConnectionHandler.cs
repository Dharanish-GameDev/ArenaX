using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkConnectionHandler : MonoBehaviour
{
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
}
