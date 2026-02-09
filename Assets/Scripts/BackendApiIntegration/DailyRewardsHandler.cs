using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arena.API.Models;
using Newtonsoft.Json;

public class DailyRewardsHandler : MonoBehaviour
{
    [SerializeField] private Transform[] sevenDaysRewards = new Transform[7];
    [SerializeField] private Button claimRewardButton;
    
    // Cache transforms to avoid repeated GetChild calls
    private Transform[][] dayRewardTransforms;
    private const int TOTAL_DAYS = 7;
    
    #if UNITY_EDITOR
    [SerializeField] private bool debug = false;
    [SerializeField] private string statusResponse;
    [SerializeField] private string claimRewardResponse;
    [SerializeField] private bool isClaimed;
    [SerializeField] private string afterClaimStatus;
    #endif
    
    private void Awake()
    {
        InitializeTransformsCache();
    }
    
    private void InitializeTransformsCache()
    {
        // Initialize cache for each day's transforms
        dayRewardTransforms = new Transform[sevenDaysRewards.Length][];
        for (int i = 0; i < sevenDaysRewards.Length; i++)
        {
            if (sevenDaysRewards[i] != null)
            {
                if (sevenDaysRewards[i].childCount >= 2)
                {
                    dayRewardTransforms[i] = new Transform[2];
                    dayRewardTransforms[i][0] = sevenDaysRewards[i].GetChild(0);
                    dayRewardTransforms[i][1] = sevenDaysRewards[i].GetChild(1);
                }
                else
                {
                    Debug.LogWarning($"Day {i} reward transform doesn't have enough children (needs at least 2)");
                }
            }
            else
            {
                Debug.LogError($"Day {i} reward transform is null!");
            }
        }
    }
    
    public void FetchRewardsFromServer(Action onComplete = null)
    {
        #if UNITY_EDITOR
        if (debug)
        {
            Debug.Log("Debug mode: Using local JSON data");
            
            // First try the debug flow
            if (!string.IsNullOrEmpty(statusResponse) && !isClaimed)
            {
                try
                {
                    Debug.Log($"Debug Status Response JSON: {statusResponse}");
                    DailyRewardStatusResponse response = JsonConvert.DeserializeObject<DailyRewardStatusResponse>(statusResponse);
                    if (response != null)
                    {
                        Debug.Log($"Debug: Successfully parsed response. currentDay: {response.currentDay}, canClaim: {response.canClaim}");
                        ApplyServerStatusResponse(response);
                        onComplete?.Invoke();
                    }
                    else
                    {
                        Debug.LogError("Debug: Failed to parse statusResponse - response is null");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Debug: JSON parse error: {ex.Message}\nStack Trace: {ex.StackTrace}");
                }
                return;
            }
            else if (!string.IsNullOrEmpty(afterClaimStatus))
            {
                try
                {
                    Debug.Log($"Debug After Claim JSON: {afterClaimStatus}");
                    DailyRewardStatusResponse response = JsonConvert.DeserializeObject<DailyRewardStatusResponse>(afterClaimStatus);
                    if (response != null)
                    {
                        ApplyServerStatusResponse(response);
                        onComplete?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Debug: After claim JSON parse error: {ex.Message}");
                }
            }
            return;
        }
        #endif
        
        Debug.Log("Fetching daily rewards from server...");
        ApiManager.Instance.SendRequest<DailyRewardStatusResponse>(
            ApiEndPoints.Rewards.DailyStatus,
            RequestMethod.GET,
            (response) =>
            {
                Debug.Log($"Server response received. Success: {response != null}");
                if (response != null)
                {
                    ApplyServerStatusResponse(response);
                }
                else
                {
                    Debug.LogError("Received null response from server");
                }
                onComplete?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"Failed to fetch daily rewards: {error}");
                onComplete?.Invoke();
            });
    }
    
    private void ApplyServerStatusResponse(DailyRewardStatusResponse response)
    {
        if (response == null) 
        {
            Debug.LogError("ApplyServerStatusResponse: response is null");
            return;
        }
        
        Debug.Log($"Applying server response: currentDay={response.currentDay}, canClaim={response.canClaim}, claimedDays count={response.claimedDays?.Count}");
        
        claimRewardButton.interactable = response.canClaim;
        
        claimRewardButton.onClick.RemoveAllListeners();
        
        claimRewardButton.onClick.AddListener(() =>
        {
            #if UNITY_EDITOR
            if (debug)
            {
                try
                {
                    Debug.Log($"Debug Claim Response JSON: {claimRewardResponse}");
                    RewardResponse rewardResponse = JsonConvert.DeserializeObject<RewardResponse>(claimRewardResponse);
                    OnClaimDailyReward(rewardResponse);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Debug: Claim response JSON parse error: {ex.Message}");
                }
                return;
            }
            #endif
            
            ClaimDailyRewardRequest req = new ClaimDailyRewardRequest();
            // Server uses 0-indexed days
            req.day = response.currentDay;
            Debug.Log($"Claiming reward for day: {req.day}");
            
            string json = "";
            try
            {
                json = JsonConvert.SerializeObject(req);
                Debug.Log($"Claim request JSON: {json}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to serialize claim request: {ex.Message}");
                return;
            }
            
            claimRewardButton.interactable = false; // Prevent multiple clicks
            
            ApiManager.Instance.SendRequest<RewardResponse>(
                ApiEndPoints.Rewards.ClaimDaily,
                RequestMethod.POST,
                (rewardResponse) =>
                {
                    Debug.Log($"Claim response received. Success: {rewardResponse != null}");
                    OnClaimDailyReward(rewardResponse);
                },
                (error) =>
                {
                    Debug.LogError($"Failed to claim daily reward: {error}");
                    claimRewardButton.interactable = true; // Re-enable on error
                },
                json);
        });
        
        // Apply visuals with 0-indexed days from server
        ApplyVisuals(response.currentDay, response.claimedDays, response.canClaim);
    }
    
    private void OnClaimDailyReward(RewardResponse response)
    {
        if (response == null)
        {
            Debug.LogError("OnClaimDailyReward: Received null reward response");
            claimRewardButton.interactable = true;
            return;
        }
        
        if (response.reward != null)
        {
            Debug.Log($"Successfully claimed RewardType {response.reward.type} With Amount {response.reward.amount}");
            EconomyManager.Instance.AddEconomy(response.reward.type, response.reward.amount);
            
            #if UNITY_EDITOR
            isClaimed = true;
            Debug.Log($"Debug: isClaimed set to {isClaimed}");
            #endif
            
            // Refresh the UI after claiming
            FetchRewardsFromServer();
        }
        else
        {
            Debug.LogWarning($"Claim failed: {response.message}");
            claimRewardButton.interactable = true; // Re-enable button if claim failed
            
            // Still refresh to get updated status
            FetchRewardsFromServer();
        }
    }
    
    private void ApplyVisuals(int currentDay, List<int> claimedDays, bool canClaim)
    {
        Debug.Log($"ApplyVisuals called: currentDay={currentDay}, canClaim={canClaim}, claimedDays count={claimedDays?.Count}");
        
        // Validate currentDay range (0-6 from server)
        if (currentDay < 0 || currentDay >= TOTAL_DAYS)
        {
            Debug.LogError($"Invalid currentDay: {currentDay}. Expected 0-{TOTAL_DAYS - 1} from server.");
            return;
        }
        
        // Reset all days first
        ResetAllDayVisuals();
        
        // Mark claimed days
        if (claimedDays != null)
        {
            foreach (int claimedDay in claimedDays)
            {
                if (claimedDay >= 0 && claimedDay < TOTAL_DAYS)
                {
                    SetDayVisuals(claimedDay, true, false);
                }
                else
                {
                    Debug.LogWarning($"Invalid claimedDay in server response: {claimedDay}");
                }
            }
        }
        
        // Handle current day
        if (canClaim && !IsDayClaimed(currentDay, claimedDays))
        {
            // Current day can be claimed
            SetDayVisuals(currentDay, false, true);
        }
        else if (canClaim && IsDayClaimed(currentDay, claimedDays))
        {
            Debug.LogWarning($"Day {currentDay} is marked as claimable but also in claimedDays list");
            SetDayVisuals(currentDay, true, false);
        }
        else if (!canClaim && !IsDayClaimed(currentDay, claimedDays))
        {
            // Current day cannot be claimed yet
            SetDayVisuals(currentDay, false, false);
        }
    }
    
    private void ResetAllDayVisuals()
    {
        for (int i = 0; i < sevenDaysRewards.Length; i++)
        {
            SetDayVisuals(i, false, false);
        }
    }
    
    private void SetDayVisuals(int dayIndex, bool isClaimed, bool isHighlighted)
    {
        if (dayIndex < 0 || dayIndex >= sevenDaysRewards.Length)
        {
            Debug.LogError($"Invalid dayIndex in SetDayVisuals: {dayIndex}");
            return;
        }
        
        if (dayRewardTransforms[dayIndex] != null && dayRewardTransforms[dayIndex].Length >= 2)
        {
            // Claimed indicator (child 0)
            dayRewardTransforms[dayIndex][0].gameObject.SetActive(isClaimed);
            
            // Reward icon (child 1)
            dayRewardTransforms[dayIndex][1].gameObject.SetActive(!isClaimed);
            
            // Highlight effect (child 2 if exists)
            if (sevenDaysRewards[dayIndex].childCount > 2)
            {
                sevenDaysRewards[dayIndex].GetChild(2).gameObject.SetActive(isHighlighted);
            }
        }
        else
        {
            Debug.LogWarning($"Day {dayIndex} transforms cache is not properly initialized");
        }
    }
    
    private bool IsDayClaimed(int day, List<int> claimedDays)
    {
        return claimedDays != null && claimedDays.Contains(day);
    }
    
    #if UNITY_EDITOR
    public void ResetDebugState()
    {
        isClaimed = false;
        Debug.Log("Debug state reset");
    }
    
    [ContextMenu("Test Valid Example JSON")]
    public void TestWithValidJson()
    {
        string exampleJson = @"{
            ""currentDay"": 1,
            ""claimedDays"": [0],
            ""nextClaimTime"": ""2026-02-08T14:42:39.782Z"",
            ""canClaim"": true
        }";
        
        statusResponse = exampleJson;
        debug = true;
        isClaimed = false;
        FetchRewardsFromServer();
        Debug.Log("Test with valid JSON triggered");
    }
    
    [ContextMenu("Test Already Claimed JSON")]
    public void TestWithAlreadyClaimedJson()
    {
        string exampleJson = @"{
            ""currentDay"": 1,
            ""claimedDays"": [0, 1],
            ""nextClaimTime"": ""2026-02-09T14:42:39.782Z"",
            ""canClaim"": false
        }";
        
        statusResponse = exampleJson;
        debug = true;
        isClaimed = true;
        FetchRewardsFromServer();
        Debug.Log("Test with already claimed JSON triggered");
    }
    
    [ContextMenu("Print Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log($"Debug mode: {debug}");
        Debug.Log($"isClaimed: {isClaimed}");
        Debug.Log($"Status Response: {statusResponse}");
        Debug.Log($"Claim Response: {claimRewardResponse}");
    }
    #endif
}