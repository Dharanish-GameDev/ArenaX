using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private GameObject gameplayPanel;

    [SerializeField] private RectTransform playerUISetsParent;
    [SerializeField] private PlayerUISet playerUISetRef;

    [SerializeField] private TextMeshProUGUI roundCountText;

    [SerializeField] private GameObject afterRoundObj;
    [SerializeField] private TextMeshProUGUI targetNumberText;

    [SerializeField] private Button startMatchButton;

    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Winner / Loser Panels")]
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private GameObject loserPanel;
    [SerializeField] private TextMeshProUGUI overallWinnerText;

    private Coroutine countdownRoutine;

    private void Awake()
    {
        if (connectionPanel != null) connectionPanel.SetActive(true);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);

        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(false);
            startMatchButton.onClick.RemoveAllListeners();
            startMatchButton.onClick.AddListener(() =>
            {
                startMatchButton.gameObject.SetActive(false);
                NetworkGameManager.Instance.StartCountDown();
            });
        }

        if (afterRoundObj != null) afterRoundObj.SetActive(false);

        if (winnerPanel != null) winnerPanel.SetActive(false);
        if (loserPanel != null) loserPanel.SetActive(false);
    }

    private void Start()
    {
        // You already had this in your project
        if (NetworkCallsManager.Instance != null)
            NetworkCallsManager.Instance.RegisterOnConnectedToNetwork(EnableGamePlayPanel);

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RegisterOnCurrentRoundValueChanged(OnRoundValueChanged);
            NetworkGameManager.Instance.RegisterTimerValueChanged(OnTimerValueChanged);
        }
    }

    public void EnableGamePlayPanel()
    {
        if (connectionPanel != null) connectionPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
    }

    public void EnableTimerText()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(true);
    }

    public void ShowStartMatchButton()
    {
        if (startMatchButton != null)
            startMatchButton.gameObject.SetActive(true);
    }

    public void HideStartMatchButton()
    {
        if (startMatchButton != null)
            startMatchButton.gameObject.SetActive(false);
    }

    public PlayerUISet CreatePlayerUISet()
    {
        if (playerUISetRef == null || playerUISetsParent == null) return null;
        PlayerUISet temp = Instantiate(playerUISetRef, playerUISetsParent);
        return temp;
    }

    private void OnRoundValueChanged(int previousRound, int currentRound)
    {
        if (roundCountText != null)
            roundCountText.text = currentRound.ToString();

        if (timerText != null)
            timerText.gameObject.SetActive(true);
    }

    private void OnTimerValueChanged(int prev, int current)
    {
        if (timerText == null) return;

        int minutes = current / 60;
        int seconds = current % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void StartCountdown(int timeInSeconds)
    {
        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);

        countdownRoutine = StartCoroutine(Countdown(timeInSeconds));
    }

    private IEnumerator Countdown(int time)
    {
        while (time > 0)
        {
            UpdateTimerUI(time);
            yield return new WaitForSeconds(1f);
            time--;
        }
        UpdateTimerUI(0);
    }

    private void UpdateTimerUI(int time)
    {
        if (timerText == null) return;

        int minutes = time / 60;
        int seconds = time % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void DisableAfterRoundUI()
    {
        if (afterRoundObj != null)
            afterRoundObj.SetActive(false);
    }

    public void UpdateAfterRoundUI(int[] winnerIds, float targetNumber)
    {
        if (afterRoundObj != null)
            afterRoundObj.SetActive(true);

        if (targetNumberText != null)
            targetNumberText.text = Mathf.RoundToInt(targetNumber).ToString();

        Invoke(nameof(DisableAfterRoundUI), 2.5f);
    }

    // ✅ Winner/Loser display
    public void ShowWinnerLoserPanel(bool isWinner, int winnerId)
    {
        if (winnerPanel != null) winnerPanel.SetActive(isWinner);
        if (loserPanel != null) loserPanel.SetActive(!isWinner);

        if (overallWinnerText != null)
            overallWinnerText.text = "Player_" + winnerId;
    }
}
