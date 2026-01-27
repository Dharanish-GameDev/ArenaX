using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUISet : MonoBehaviour
{
    private NetworkPlayer _networkPlayer;

    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image playerIconImage;

    // ✅ Use this as Lives text (rename in inspector if you want)
    [SerializeField] private TextMeshProUGUI coinsCountText;

    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private Button submitButton; // optional (can be null)
    [SerializeField] private GameObject elimitatedObject;

    private void Awake()
    {
        if (elimitatedObject != null)
            elimitatedObject.SetActive(false);
    }
    private void OnPlayerStateChanged(PlayerState oldState, PlayerState newState)
    {
        bool eliminated = newState == PlayerState.Eliminated;

        if (elimitatedObject != null)
            elimitatedObject.SetActive(eliminated);

        if (_networkPlayer != null && _networkPlayer.IsOwner)
            SetSubmitButtonInteractable(!eliminated);
    }


    public void SetPlayer(NetworkPlayer networkPlayer)
    {
        _networkPlayer = networkPlayer;
        if (_networkPlayer == null) return;

        int id = _networkPlayer.GetPlayerID();
        PoP_PlayerDataSO playerDataSO = NetworkGameManager.Instance.GetPlayerDataForId(id);

        if (playerDataSO != null)
        {
            name = playerDataSO.playerName;

            if (playerNameText != null)
                playerNameText.SetText(playerDataSO.playerName);

            if (playerIconImage != null)
                playerIconImage.sprite = playerDataSO.playerIcon;
        }
        else
        {
            // fallback
            if (playerNameText != null)
                playerNameText.SetText("Player_" + id);
        }

        // ✅ Score initial
        if (scoreText != null)
            scoreText.SetText(_networkPlayer.GetCurrentScore().ToString());

        // ✅ Lives initial (starts at 3)
        if (coinsCountText != null)
            coinsCountText.SetText(_networkPlayer.GetLives().ToString());

        // Owner sets submit event ONCE
        if (_networkPlayer.IsOwner && NumbersHandler.Instance != null)
        {
            NumbersHandler.Instance.ClearSubmitButtonEvents();

            NumbersHandler.Instance.SetSubmitButtonEvent(() =>
            {
                if (NumbersHandler.Instance.CurrentValue < 0) return;
                _networkPlayer.SetCurrentNumber(NumbersHandler.Instance.CurrentValue);
            });

            NumbersHandler.Instance.SetInputVisibleAndInteractable(true, resetSelection: true);
        }

        // register listeners
        _networkPlayer.RegisterCurrentScoreValueChanged(OnScoreValueChanged);
        _networkPlayer.RegisterCurrentNumberValueChanged(OnCurrentNumberChanged);
        _networkPlayer.RegisterPlayerStateChanges(OnPlayerStateChanged);
        _networkPlayer.RegisterLivesValueChanged(OnLivesChanged);

        // apply eliminated state initially
        // apply eliminated state initially
        OnPlayerStateChanged(PlayerState.Active, _networkPlayer.GetCurrentPlayerState());

    }

    public void SetSubmitButtonInteractable(bool interactable)
    {
        if (submitButton != null)
            submitButton.interactable = interactable;

        if (_networkPlayer != null && _networkPlayer.IsOwner && NumbersHandler.Instance != null)
        {
            NumbersHandler.Instance.SetInputVisibleAndInteractable(interactable, resetSelection: !interactable);
        }
    }

   

    private void OnScoreValueChanged(int oldScore, int newScore)
    {
        if (scoreText != null)
            scoreText.SetText(newScore.ToString());
    }

    private void OnLivesChanged(int oldLives, int newLives)
    {
        if (coinsCountText != null)
            coinsCountText.SetText(newLives.ToString());
    }

    private void OnCurrentNumberChanged(int oldNumber, int newNumber)
    {
        // When new round clears number (<0), reset local selection
        if (newNumber < 0 && _networkPlayer != null && _networkPlayer.IsOwner && NumbersHandler.Instance != null)
        {
            NumbersHandler.Instance.ResetValues();
            NumbersHandler.Instance.SetInputVisibleAndInteractable(true, resetSelection: true);
        }
    }
}
