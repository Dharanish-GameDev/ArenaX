using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private GameObject gameplayPanel;

    [SerializeField] private RectTransform playerUISetsParent;
    [SerializeField] private PlayerUISet playerUISetRef;

    [SerializeField] private TextMeshProUGUI roundCountText;

    //[SerializeField] private GameObject timerObject;
    //[SerializeField] private TextMeshProUGUI timeText;
    private Coroutine countdownRoutine;

    [SerializeField] private GameObject afterRoundObj;
    [SerializeField] private TextMeshProUGUI targetNumberText;
   // [SerializeField] private TextMeshProUGUI winnerNameText;

    // [SerializeField] private GameObject winnerUIObj;
    // [SerializeField] private TextMeshProUGUI overallWinnerText;

    [SerializeField] private GameObject winnerAnnouncementPanel;
    [SerializeField] private GameObject winnerUIObj;
    [SerializeField] private GameObject loserUIObj;

    [SerializeField] private Button startMatchButton;

    [SerializeField] private TextMeshProUGUI timerText;
    
    [SerializeField] private GameObject waitingUIObj;

    private void Awake()
    {
        connectionPanel.SetActive(true);
        gameplayPanel.SetActive(false);
        startMatchButton.gameObject.SetActive(false);
        startMatchButton.onClick.AddListener(() =>
        {
            startMatchButton.gameObject.SetActive(false);
            NetworkGameManager.Instance.StartCountDown();
        });
    }

    private void Start()
    {
        NetworkCallsManager.Instance.RegisterOnConnectedToNetwork(EnableGamePlayPanel);
        NetworkGameManager.Instance.RegisterOnCurrentRoundValueChanged(OnRoundValueChanged);
        NetworkGameManager.Instance.RegisterTimerValueChanged(OnTimerValueChanged);
        NetworkGameManager.Instance.OnTimerEnd += () =>
        {
            // timerText.gameObject.SetActive(false);
        };
    }

    public void AnnounceWinner(int winnerId)
    {
        winnerAnnouncementPanel.gameObject.SetActive(true);
        if (winnerId == (int)NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("I Win");
            winnerUIObj.SetActive(true);
            loserUIObj.SetActive(false);
        }
        else
        {
            Debug.Log("I Lose");
            winnerUIObj.SetActive(false);
            loserUIObj.SetActive(true);
        }
    }

    public void EnableTimerText()
    {
        timerText.gameObject.SetActive(true);
    }

    public void ShowStartMatchButton()
    {
        startMatchButton.gameObject.SetActive(true);
    }
    public void EnableGamePlayPanel()
    {
        connectionPanel.SetActive(false);
        gameplayPanel.SetActive(true);
    }

    public PlayerUISet CreatePlayerUISet()
    {
        PlayerUISet temp = Instantiate(playerUISetRef, playerUISetsParent);
        return temp;
    }

    public void DisableWaitingScreen()
    {
        if (waitingUIObj != null)
        {
            waitingUIObj.SetActive(false);
        }
    }

    private void OnRoundValueChanged(int previousRound, int currentRound)
    {
        if (roundCountText != null)
        {
            roundCountText.text = currentRound.ToString();
        }
        timerText.gameObject.SetActive(true);
    }

    private void OnTimerValueChanged(int prev, int current)
    {
        if (timerText != null)
        {
            int minutes = current / 60;
            int seconds = current % 60;
            
            timerText.text = $"{minutes:00}:{seconds:00}";
            
            timerText.text = $"{minutes:00}.{seconds:00}";

            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void StartCountdown(int timeInSeconds)
    {
        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);
        // timerObject.SetActive(true);
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
        int minutes = time / 60;
        int seconds = time % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // 🔹 PATCHED: handle multiple winners or no winner

    private void DisableAfterRoundUI()
    {
        afterRoundObj.SetActive(false);
    }
    
    public void UpdateAfterRoundUI(int[] winnerIds, float targetNumber)
    {
        afterRoundObj.SetActive(true);
        //
        // // Show winner(s) or no winner
        // if (winnerIds == null || winnerIds.Length == 0)
        // {
        //     winnerNameText.text = "No Winner";
        // }
        // else
        // {
        //     winnerNameText.text = string.Join(", ", winnerIds.Select(id => "Player_" + id));
        // }
        
        

        targetNumberText.text = Mathf.RoundToInt(targetNumber).ToString();
        Invoke(nameof(DisableAfterRoundUI),2.5f);
    }
}
