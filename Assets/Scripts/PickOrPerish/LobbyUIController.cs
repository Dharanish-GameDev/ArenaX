using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button startGameButton;

    private void Start()
    {
        startGameButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance?.StartGame();
        });

        InvokeRepeating(nameof(UpdateUI), 0.5f, 0.5f);
    }

    private void UpdateUI()
    {
        if (LobbyManager.Instance == null) return;

        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby == null) return;

        roomCodeText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count} / {lobby.MaxPlayers}";

        startGameButton.gameObject.SetActive(LobbyManager.Instance.IsHost);
        startGameButton.interactable = lobby.Players.Count == lobby.MaxPlayers;
    }
}