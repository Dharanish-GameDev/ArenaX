using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyUIController : MonoBehaviour
{
    [Header("Lobby Info")]
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button startGameButton;

    [Header("Host Slot")]
    [SerializeField] private LobbyPlayerSlotUI hostSlot;

    [Header("Player Slots (Pre-Created in Scene)")]
    [SerializeField] private List<LobbyPlayerSlotUI> slots;

    private void Start()
    {
        startGameButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance?.StartGame();
        });

        // Refresh UI every 0.5s to handle async lobby updates safely
        InvokeRepeating(nameof(UpdateUI), 0.5f, 0.5f);
    }

    private void UpdateUI()
    {
        var manager = LobbyManager.Instance;
        if (manager == null) return;

        var lobby = manager.CurrentLobby;
        if (lobby == null) return;

        roomCodeText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count} / {lobby.MaxPlayers}";

        startGameButton.gameObject.SetActive(manager.IsHost && !manager.IsJoinedMatchmaking);
        if (lobby.Players.Count == lobby.MaxPlayers && manager.IsHost && manager.IsJoinedMatchmaking)
        {
            startGameButton.onClick.Invoke();
            startGameButton.onClick.RemoveAllListeners();
        }
        startGameButton.interactable = lobby.Players.Count >= 2;

        // Always refresh slots (avatars arrive async)
        RefreshSlots(manager.GetJoinedPlayers(), manager.IsHost);
    }

    private void RefreshSlots(List<LobbyManager.LobbyPlayerInfo> players, bool isLocalHost)
    {
        if (players.Count == 0) return;

        // Host = First player (Unity lobby owner always index 0)
        var host = players[0];
        hostSlot.gameObject.SetActive(true);
        hostSlot.Set(host.name, host.avatarIndex);

        int slotIndex = 0;

        for (int i = 1; i < players.Count && slotIndex < slots.Count; i++)
        {
            var p = players[i];

            slots[slotIndex].gameObject.SetActive(true);
            slots[slotIndex].Set(p.name, p.avatarIndex);

            slotIndex++;
        }

        // // Disable unused slots
        // for (int i = slotIndex; i < slots.Count; i++)
        // {
        //     slots[i].gameObject.SetActive(false);
        // }
    }
}
