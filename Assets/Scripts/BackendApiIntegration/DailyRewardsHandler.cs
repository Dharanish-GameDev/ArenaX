using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Arena.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;

public class DailyRewardsHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform[] sevenDaysRewards = new RectTransform[7];
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private TextMeshProUGUI nextClaimText;
    
    private Dictionary<int,RectTransform> sevenDaysRewardsDict = new Dictionary<int, RectTransform>();
    
    private const int TOTAL_DAYS = 7;

#if UNITY_EDITOR
    [Header("Editor Debug")]
    [SerializeField] private bool debug = false;
    [SerializeField] private string statusResponse;
#endif

    private void Awake()
    {
        InitializeTransformsCache();
    }

    private void InitializeTransformsCache()
    {
        for (int i = 1; i < TOTAL_DAYS + 1; i++)
        {
            sevenDaysRewardsDict[i] = sevenDaysRewards[i - 1];
        }

        foreach (var keyValuePair in sevenDaysRewardsDict)
        {
            Debug.Log($"{keyValuePair.Key}: {keyValuePair.Value.name}");
        }
    }

    public void FetchRewardsFromServer(Action onComplete = null)
    {
#if UNITY_EDITOR
        if (debug && !string.IsNullOrEmpty(statusResponse))
        {
            ProcessDebugJson(statusResponse);
            onComplete?.Invoke();
            return;
        }
#endif

        ApiManager.Instance.SendRequest(
            ApiEndPoints.Rewards.DailyStatus,
            RequestMethod.GET,
            (res) =>
            {
                DailyRewardStatusResponse response =
                    JsonConvert.DeserializeObject<DailyRewardStatusResponse>(res);

                if (response == null)
                {
                    Debug.LogError("[DailyRewards] Failed to parse response");
                    return;
                }

                Debug.Log($"[DailyRewards] Day={response.currentDay}, CanClaim={response.canClaimToday}, Claimed=[{string.Join(",", response.claimedDays)}]");

                ApplyServerStatusResponse(response);
                onComplete?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"[DailyRewards] Status API Failed: {error}");
                onComplete?.Invoke();
            });
    }

#if UNITY_EDITOR
    private void ProcessDebugJson(string json)
    {
        try
        {
            JObject.Parse(json);

            DailyRewardStatusResponse response =
                JsonConvert.DeserializeObject<DailyRewardStatusResponse>(json);

            ApplyServerStatusResponse(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DailyRewards] Invalid Debug JSON → {e.Message}");
        }
    }
#endif

    private void ApplyServerStatusResponse(DailyRewardStatusResponse response)
    {
        if (response.claimedDays == null)
            response.claimedDays = new List<int>();

        claimRewardButton.onClick.RemoveAllListeners();
        claimRewardButton.interactable = response.canClaimToday;
        
        

        if (response.canClaimToday)
        {
            // Keep backend day (1-7) for the API call
            int backendCurrentDay = response.currentDay;
            claimRewardButton.onClick.AddListener(() =>
            {
                OnClaimButtonClicked(backendCurrentDay);
            });
            nextClaimText.gameObject.SetActive(false);
        }
        else
        {
            nextClaimText.text = $"Your Next Claim is in {response.nextResetInHours}  hours";
            nextClaimText.gameObject.SetActive(true);
        }
        List<int> uiClaimedDays = new List<int>();
        foreach (int day in response.claimedDays)
        {
            uiClaimedDays.Add(day);
        }

        int uiCurrentDay = response.currentDay;

        ApplyVisuals(uiCurrentDay, uiClaimedDays, response.canClaimToday);
    }

    private void OnClaimButtonClicked(int backendDay)
    {
        ClaimDailyRewardRequest req = new ClaimDailyRewardRequest { day = backendDay };
        string json = JsonConvert.SerializeObject(req);

        claimRewardButton.interactable = false;

        ApiManager.Instance.SendRequest<ClaimRewardResponse>(
            ApiEndPoints.Rewards.ClaimDaily,
            RequestMethod.POST,
            (rewardResponse) =>
            {
                OnClaimDailyReward(rewardResponse);
            },
            (error) =>
            {
                Debug.LogError($"[DailyRewards] Claim Failed: {error}");
                claimRewardButton.interactable = true;
            },
            json);
    }

    private void OnClaimDailyReward(ClaimRewardResponse response)
    {
        if (response == null)
        {
            claimRewardButton.interactable = true;
            return;
        }

        if (response.reward != null)
        {
            Debug.Log($"[DailyRewards] Claimed Reward: {response.reward}");
            EconomyManager.Instance.FetchWalletBalance();
        }

        FetchRewardsFromServer();
    }

    private void ApplyVisuals(int currentDayIndex, List<int> claimedDaysIndices, bool canClaim)
    {
        Debug.Log($"Applying Visuals - Current UI Index: {currentDayIndex}, CanClaim: {canClaim}");

        for (int i = 0; i < claimedDaysIndices.Count; i++)
        {
            Debug.Log("Claimed Days : " + claimedDaysIndices[i]);
        }
        
        for (int i = 1; i <= TOTAL_DAYS; i++)
        {
            bool isClaimed = claimedDaysIndices.Contains(i);
            bool isCurrent = (i == currentDayIndex);
            bool isCurrentAndClaimable = (isCurrent && canClaim && !isClaimed);

            if (isClaimed)
            {
                SetDayVisual(i, true, false);
            }
            else if (isCurrentAndClaimable)
            {
                SetDayVisual(i, false, false);
            }
            else
            {
                // UNCLAIMED (not current or current but can't claim): Show locked visual, hide claimed visual
                SetDayVisual(i, false, true);
            }
        }
    }

    private void SetDayVisual(int dayIndex, bool showClaimed, bool showLocked)
    {
        // if (sevenDaysRewards == null ||
        //     dayIndex < 0 || dayIndex >= sevenDaysRewards.Length)
        //     return;
        if (sevenDaysRewardsDict.ContainsKey(dayIndex))
        {
            var t = sevenDaysRewardsDict[dayIndex];
            // if (t == null) return;
            
            // Child 0 = Claimed Visual
           t.GetChild(0).gameObject.SetActive(showClaimed);
            
            // Child 1 = Locked Visual
            t.GetChild(1).gameObject.SetActive(showLocked);
        }
        else
        {
            Debug.LogError($"[DailyRewards] Day {dayIndex} not found");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test - Day 2 Current & Claimable")]
    public void TestDay2CurrentAndClaimable()
    {
        statusResponse = @"{
            ""canClaimToday"": true,
            ""currentDay"": 2,
            ""claimedDays"": [1]
        }";
        debug = true;
        FetchRewardsFromServer();
    }

    [ContextMenu("Test - Day 3 Current & Not Claimable")]
    public void TestDay3CurrentNotClaimable()
    {
        statusResponse = @"{
            ""canClaimToday"": false,
            ""currentDay"": 3,
            ""claimedDays"": [1, 2]
        }";
        debug = true;
        FetchRewardsFromServer();
    }

    [ContextMenu("Test - Day 1 Claimed")]
    public void TestDay1Claimed()
    {
        statusResponse = @"{
            ""canClaimToday"": false,
            ""currentDay"": 2,
            ""claimedDays"": [1]
        }";
        debug = true;
        FetchRewardsFromServer();
    }

    [ContextMenu("Test - All Claimed")]
    public void TestAllClaimed()
    {
        statusResponse = @"{
            ""canClaimToday"": false,
            ""currentDay"": 7,
            ""claimedDays"": [1, 2, 3, 4, 5, 6, 7]
        }";
        debug = true;
        FetchRewardsFromServer();
    }

    [ContextMenu("Test - No Claims")]
    public void TestNoClaims()
    {
        statusResponse = @"{
            ""canClaimToday"": true,
            ""currentDay"": 1,
            ""claimedDays"": []
        }";
        debug = true;
        FetchRewardsFromServer();
    }
#endif
}