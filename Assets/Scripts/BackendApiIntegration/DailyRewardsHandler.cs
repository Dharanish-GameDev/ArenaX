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
    
    #if UNITY_EDITOR
    [SerializeField] private bool debug = false;
    [SerializeField] private string statusResponse;
    [SerializeField] private string claimRewardResponse;
    [SerializeField] private bool isClaimed;
    [SerializeField] private string afterClaimStatus;
    #endif
    
    private void Awake()
    {
        // Initialize cache for each day's transforms
        dayRewardTransforms = new Transform[sevenDaysRewards.Length][];
        for (int i = 0; i < sevenDaysRewards.Length; i++)
        {
            if (sevenDaysRewards[i] != null && sevenDaysRewards[i].childCount >= 2)
            {
                dayRewardTransforms[i] = new Transform[2];
                dayRewardTransforms[i][0] = sevenDaysRewards[i].GetChild(0);
                dayRewardTransforms[i][1] = sevenDaysRewards[i].GetChild(1);
            }
        }
    }
    
    public void FetchRewardsFromServer(Action onComplete = null)
    {
        #if UNITY_EDITOR
        if (debug)
        {
            if (!string.IsNullOrEmpty(statusResponse) && !isClaimed)
            {
                DailyRewardStatusResponse response = JsonConvert.DeserializeObject<DailyRewardStatusResponse>(statusResponse);
                if (response != null)
                {
                    ApplyServerStatusResponse(response);
                    onComplete?.Invoke();
                }
                return;
            }
            else if (!string.IsNullOrEmpty(afterClaimStatus))
            {
                DailyRewardStatusResponse response = JsonConvert.DeserializeObject<DailyRewardStatusResponse>(afterClaimStatus);
                if (response != null)
                {
                    ApplyServerStatusResponse(response);
                    onComplete?.Invoke();
                }
            }
            return;
        }
        #endif
        
        ApiManager.Instance.SendRequest<DailyRewardStatusResponse>(
            ApiEndPoints.Rewards.DailyStatus,
            RequestMethod.GET,
            (response) =>
            {
                if (response != null)
                {
                    ApplyServerStatusResponse(response);
                }
                onComplete?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"Failed to fetch daily rewards status: {error}");
                onComplete?.Invoke();
            });
    }
    
    private void ApplyServerStatusResponse(DailyRewardStatusResponse response)
    {
        if (response == null) return;
        
        claimRewardButton.interactable = response.canClaim;
        
        claimRewardButton.onClick.RemoveAllListeners();
        
        claimRewardButton.onClick.AddListener(() =>
        {
            #if UNITY_EDITOR
            if (debug)
            {
                RewardResponse rewardResponse = JsonConvert.DeserializeObject<RewardResponse>(claimRewardResponse);
                OnClaimDailyReward(rewardResponse);
                return;
            }
            #endif
            
            ClaimDailyRewardRequest req = new ClaimDailyRewardRequest();
            req.day = response.currentDay;
            string json = JsonConvert.SerializeObject(req);
            
            claimRewardButton.interactable = false; // Prevent multiple clicks
            
            ApiManager.Instance.SendRequest<RewardResponse>(
                ApiEndPoints.Rewards.ClaimDaily,
                RequestMethod.POST,
                (rewardResponse) =>
                {
                    OnClaimDailyReward(rewardResponse);
                },
                (error) =>
                {
                    Debug.LogError($"Failed to claim daily reward: {error}");
                    claimRewardButton.interactable = true; // Re-enable on error
                },
                json);
        });
        
        // Apply visuals with correct day indexing
        ApplyVisuals(response.currentDay, response.claimedDays, response.canClaim);
    }
    
    private void OnClaimDailyReward(RewardResponse response)
    {
        if (response == null)
        {
            Debug.LogError("Received null reward response");
            claimRewardButton.interactable = true;
            return;
        }
        
        if (response.reward != null)
        {
            Debug.Log($"Claiming RewardType {response.reward.type} With Amount {response.reward.amount}");
            EconomyManager.Instance.AddEconomy(response.reward.type, response.reward.amount);
            
            #if UNITY_EDITOR
            isClaimed = true;
            #endif
            
            // Refresh the UI after claiming
            FetchRewardsFromServer();
        }
        else
        {
            Debug.LogWarning($"Can't Claim Reward: {response.message}");
            claimRewardButton.interactable = true; // Re-enable button if claim failed
        }
    }
    
    private void ApplyVisuals(int currentDay, List<int> alreadyClaimedDays, bool canClaim)
    {
        // Validate currentDay range
        if (currentDay < 1 || currentDay > 7)
        {
            Debug.LogError($"Invalid currentDay: {currentDay}. Expected 1-7.");
            return;
        }
        
        for (int i = 0; i < sevenDaysRewards.Length; i++)
        {
            // Convert array index (0-6) to day number (1-7)
            int dayNumber = i + 1;
            
            // Check if this day has been claimed
            bool isClaimed = alreadyClaimedDays.Contains(dayNumber);
            
            // Skip if this is the current claimable day (it will be handled separately)
            if (dayNumber == currentDay && canClaim && !isClaimed)
                continue;
                
            // Update visuals for each day
            if (dayRewardTransforms[i] != null && dayRewardTransforms[i].Length >= 2)
            {
                // Show claimed indicator or reward icon
                dayRewardTransforms[i][0].gameObject.SetActive(isClaimed);
                dayRewardTransforms[i][1].gameObject.SetActive(!isClaimed);
            }
        }
        
        // Handle current day specially if it can be claimed
        if (canClaim && currentDay >= 1 && currentDay <= 7)
        {
            int currentDayIndex = currentDay - 1; // Convert to 0-based index
            
            if (currentDayIndex < sevenDaysRewards.Length && 
                dayRewardTransforms[currentDayIndex] != null && 
                dayRewardTransforms[currentDayIndex].Length >= 2)
            {
                // Special visual state for today's claimable reward
                // For example: show both inactive and add a highlight effect
                dayRewardTransforms[currentDayIndex][0].gameObject.SetActive(false);
                dayRewardTransforms[currentDayIndex][1].gameObject.SetActive(false);
                
                // You might want to activate a third child for highlighting
                if (sevenDaysRewards[currentDayIndex].childCount > 2)
                {
                    sevenDaysRewards[currentDayIndex].GetChild(2).gameObject.SetActive(true);
                }
            }
        }
    }
    
    // Helper method to reset debug states
    #if UNITY_EDITOR
    public void ResetDebugState()
    {
        isClaimed = false;
    }
    #endif
}