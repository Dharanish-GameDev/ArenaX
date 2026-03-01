using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class MigrationManager : MonoBehaviour
{
    public static MigrationManager Instance;

    private bool migrationInProgress = false;
    private bool waitingForNewHost = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (migrationInProgress)
            return;

        // If server disconnected → host died
        if (clientId == NetworkManager.ServerClientId)
        {
            Debug.Log("⚠ Host Disconnected. Starting migration.");
            migrationInProgress = true;

            StartMigration();
        }
    }

    private async void StartMigration()
    {
        // Clean shutdown of dead NGO session
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        await Task.Delay(500); // small delay to ensure shutdown completes

        var lobby = LobbyManager.Instance.CurrentLobby;

        if (lobby == null)
        {
            Debug.LogError("Migration failed: Lobby not found.");
            migrationInProgress = false;
            return;
        }

        // Refresh lobby to get latest player list
        lobby = await Lobbies.Instance.GetLobbyAsync(lobby.Id);

        if (lobby.Players.Count <= 1)
        {
            Debug.Log("Not enough players to continue match.");
            migrationInProgress = false;
            return;
        }

        // Elect new host = first player in lobby list
        string newHostPlayerId = lobby.Players[0].Id;

        if (newHostPlayerId == AuthenticationService.Instance.PlayerId)
        {
            Debug.Log("🟢 I am elected as new host.");
            await BecomeNewHost(lobby);
        }
        else
        {
            Debug.Log("🟡 Waiting for new host to rebuild session...");
            waitingForNewHost = true;
        }
    }

    private async Task BecomeNewHost(Lobby lobby)
    {
        try
        {
            string newRelayCode = await LobbyManager.Instance.RecreateRelayAsHost();

            Debug.Log("🟢 New Relay Created: " + newRelayCode);

            // Restore match state
           // NetworkGameManager.Instance.RestoreFromSnapshot();

            migrationInProgress = false;
        }
        catch (Exception e)
        {
            Debug.LogError("Migration failed while becoming host: " + e);
            migrationInProgress = false;
        }
    }

    public async void CheckForReconnection()
    {
        if (!waitingForNewHost)
            return;

        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby == null)
            return;

        lobby = await Lobbies.Instance.GetLobbyAsync(lobby.Id);

        const string START_GAME_KEY = "START_GAME";

        if (lobby.Data != null &&
            lobby.Data.ContainsKey(START_GAME_KEY))
        {
            string relayCode = lobby.Data[START_GAME_KEY].Value;

            if (!string.IsNullOrEmpty(relayCode) && relayCode != "0")
            {
                Debug.Log("🔄 New relay detected. Reconnecting...");

                LobbyManager.Instance.JoinRelayAfterMigration(relayCode);

                waitingForNewHost = false;
                migrationInProgress = false;
            }
        }
    }
}