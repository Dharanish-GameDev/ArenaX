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

    private Coroutine countdownRoutine;

    [SerializeField] private GameObject afterRoundObj;
    [SerializeField] private TextMeshProUGUI targetNumberText;

    [SerializeField] private GameObject winnerAnnouncementPanel;
    [SerializeField] private GameObject winnerUIObj;
    [SerializeField] private TextMeshProUGUI rewardCoinsText;
    [SerializeField] private GameObject loserUIObj;

    [SerializeField] private Button startMatchButton;

    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private GameObject waitingUIObj;
    [SerializeField] private float winnerAnnouncementDelay = 2f;
    
    [SerializeField] private FriendRequestUIItem friendRequestUIItem;
    [SerializeField] private TMP_Dropdown quickMatchMaxPlayersCount;
    public int[] maxPlayerValues = { 4, 6, 8 };


    private void Awake()
    {
         if (quickMatchMaxPlayersCount != null)
        {
            quickMatchMaxPlayersCount.onValueChanged.AddListener(OnMaxPlayersDropdownChanged);
        }
    }

    public void OnMaxPlayersDropdownChanged(int index)
    {
        LobbyManager.Instance.SetMatchmakingMaxPlayers(maxPlayerValues[index]);
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
        StartCoroutine(AnnounceWinnerDelayed(winnerId));
    }

    private IEnumerator AnnounceWinnerDelayed(int winnerId)
    {
        yield return new WaitForSeconds(winnerAnnouncementDelay);

        winnerAnnouncementPanel.gameObject.SetActive(true);

        if (winnerId == (int)NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("I Win");
            winnerUIObj.SetActive(true);
            loserUIObj.SetActive(false);
            int coins = FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include).CurrentRoomData.coinAmount;
            rewardCoinsText.text = (coins*2).ToString();
        }
        else
        {
            Debug.Log("I Lose");
            winnerUIObj.SetActive(false);
            loserUIObj.SetActive(true);
        }

        EconomyManager.Instance.FetchWalletBalance();
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
        targetNumberText.text = Mathf.RoundToInt(targetNumber).ToString();
        Invoke(nameof(DisableAfterRoundUI),2.5f);
    }

    public void ShowFriendRequestUIItem(string Uid, string profilePic, string name)
    {
        friendRequestUIItem.gameObject.SetActive(true);
        friendRequestUIItem.SetupUIItem(Uid, profilePic, name);
    }
}
