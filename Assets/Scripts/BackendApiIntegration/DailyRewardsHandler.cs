using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arena.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class DailyRewardsHandler : MonoBehaviour
{
    [SerializeField] private Transform[] sevenDaysRewards = new Transform[7];
    [SerializeField] private Button claimRewardButton;
    
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
        if (sevenDaysRewards == null)
        {
            Debug.LogError("sevenDaysRewards array is not assigned!");
            return;
        }
        
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
            Debug.Log("[DEBUG] Using local JSON data");
            
            if (!string.IsNullOrEmpty(statusResponse) && !isClaimed)
            {
                ProcessDebugJson(statusResponse, "status");
            }
            else if (!string.IsNullOrEmpty(afterClaimStatus))
            {
                ProcessDebugJson(afterClaimStatus, "after claim");
            }
            onComplete?.Invoke();
            return;
        }
        #endif
        
        Debug.Log("[API] Fetching daily rewards from server...");
        ApiManager.Instance.SendRequest(
            ApiEndPoints.Rewards.DailyStatus,
            RequestMethod.GET,
            (res) =>
            {
                Debug.Log($"[API] Received response: {res}");
                DailyRewardStatusResponse response = JsonConvert.DeserializeObject<DailyRewardStatusResponse>(res);
                if (response != null)
                {
                    Debug.Log("[API] Successfully received and parsed response");
                    ApplyServerStatusResponse(response);
                }
                else
                {
                    Debug.LogError("[API] Received null response after parsing");
                }
                onComplete?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"[API] Request failed: {error}");
                onComplete?.Invoke();
            });
    }
    
    #if UNITY_EDITOR
    private void ProcessDebugJson(string json, string source)
    {
        Debug.Log($"[DEBUG {source}] Processing JSON: {json}");
        
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[DEBUG {source}] JSON is empty");
            return;
        }
        
        try
        {
            // First validate the JSON structure
            var jsonObject = JObject.Parse(json);
            Debug.Log($"[DEBUG {source}] JSON is valid, parsing as DailyRewardStatusResponse");
            
            DailyRewardStatusResponse response = JsonConvert.DeserializeObject<DailyRewardStatusResponse>(json);
            if (response != null)
            {
                Debug.Log($"[DEBUG {source}] Successfully parsed: currentDay={response.currentDay}, canClaim={response.canClaimToday}");
                ApplyServerStatusResponse(response);
            }
            else
            {
                Debug.LogError($"[DEBUG {source}] Parsed response is null");
            }
        }
        catch (JsonReaderException jex)
        {
            Debug.LogError($"[DEBUG {source}] JSON Reader Error: {jex.Message}\nLine: {jex.LineNumber}, Position: {jex.LinePosition}");
        }
        catch (JsonSerializationException sex)
        {
            Debug.LogError($"[DEBUG {source}] JSON Serialization Error: {sex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DEBUG {source}] Unexpected error: {ex.Message}\n{ex.StackTrace}");
        }
    }
    #endif
    
    private void ApplyServerStatusResponse(DailyRewardStatusResponse response)
    {
        if (response == null) 
        {
            Debug.LogError("ApplyServerStatusResponse: Response is null");
            return;
        }
        
        // Validate required fields
        if (response.claimedDays == null)
        {
            Debug.LogWarning("claimedDays is null, initializing empty list");
            response.claimedDays = new List<int>();
        }
        
        Debug.Log($"Applying response - currentDay: {response.currentDay}, canClaim: {response.canClaimToday}, claimedDays: [{string.Join(", ", response.claimedDays)}]");
        
        // Update button state
        claimRewardButton.interactable = response.canClaimToday;
        claimRewardButton.onClick.RemoveAllListeners();
        
        claimRewardButton.onClick.AddListener(() =>
        {
            OnClaimButtonClicked(response.currentDay);
        });
        
        // Apply visuals
        ApplyVisuals(response.currentDay, response.claimedDays, response.canClaimToday);
    }
    
    private void OnClaimButtonClicked(int currentDay)
    {
        #if UNITY_EDITOR
        if (debug)
        {
            if (!string.IsNullOrEmpty(claimRewardResponse))
            {
                try
                {
                    var rewardResponse = JsonConvert.DeserializeObject<RewardResponse>(claimRewardResponse);
                    if (rewardResponse != null)
                    {
                        OnClaimDailyReward(rewardResponse);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Debug claim JSON error: {ex.Message}");
                }
            }
            return;
        }
        #endif
        
        ClaimDailyRewardRequest req = new ClaimDailyRewardRequest();
        req.day = currentDay;
        
        string json = JsonConvert.SerializeObject(req);
        Debug.Log($"Sending claim request for day {currentDay}: {json}");
        
        claimRewardButton.interactable = false;
        
        ApiManager.Instance.SendRequest<RewardResponse>(
            ApiEndPoints.Rewards.ClaimDaily,
            RequestMethod.POST,
            (rewardResponse) =>
            {
                OnClaimDailyReward(rewardResponse);
            },
            (error) =>
            {
                Debug.LogError($"Claim request failed: {error}");
                claimRewardButton.interactable = true;
            },
            json);
    }
    
    private void OnClaimDailyReward(RewardResponse response)
    {
        if (response == null)
        {
            Debug.LogError("Claim response is null");
            claimRewardButton.interactable = true;
            return;
        }
        
        if (response.reward != null)
        {
            Debug.Log($"Successfully claimed {response.reward.amount} {response.reward.type}");
            
            // Add to economy
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddEconomy(response.reward.type, response.reward.amount);
            }
            
            #if UNITY_EDITOR
            isClaimed = true;
            #endif
            
            // Refresh status
            FetchRewardsFromServer();
        }
        else
        {
            Debug.LogWarning($"Claim failed: {response.message}");
            claimRewardButton.interactable = true;
            
            // Still refresh to update status
            FetchRewardsFromServer();
        }
    }
    
    private void ApplyVisuals(int currentDay, List<int> claimedDays, bool canClaim)
    {
        // Validate input
        if (currentDay < 0 || currentDay >= TOTAL_DAYS)
        {
            Debug.LogError($"Invalid currentDay: {currentDay}. Must be 0-{TOTAL_DAYS-1}");
            return;
        }
        
        if (claimedDays == null)
        {
            claimedDays = new List<int>();
        }
        
        Debug.Log($"Setting visuals - Day {currentDay}, Claimable: {canClaim}, Claimed: {claimedDays.Contains(currentDay)}");
        
        // Reset all visuals first
        for (int i = 0; i < TOTAL_DAYS; i++)
        {
            SetDayVisual(i, false, false);
        }
        
        // Mark claimed days
        foreach (int day in claimedDays)
        {
            if (day >= 0 && day < TOTAL_DAYS)
            {
                SetDayVisual(day, true, false);
            }
        }
        
        // Highlight current day if claimable and not claimed
        if (canClaim && !claimedDays.Contains(currentDay))
        {
            SetDayVisual(currentDay, false, true);
        }
    }
    
    private void SetDayVisual(int dayIndex, bool isClaimed, bool isHighlighted)
    {
        if (dayIndex < 0 || dayIndex >= TOTAL_DAYS)
            return;
            
        if (dayRewardTransforms == null || dayRewardTransforms.Length <= dayIndex)
            return;
            
        var transforms = dayRewardTransforms[dayIndex];
        if (transforms == null || transforms.Length < 2)
            return;
            
        // Child 0: Claimed indicator
        if (transforms[0] != null)
            transforms[0].gameObject.SetActive(isClaimed);
            
        // Child 1: Reward icon
        if (transforms[1] != null)
            transforms[1].gameObject.SetActive(!isClaimed);
            
        // Child 2: Highlight (optional)
        if (sevenDaysRewards[dayIndex].childCount > 2)
        {
            var highlight = sevenDaysRewards[dayIndex].GetChild(2);
            if (highlight != null)
                highlight.gameObject.SetActive(isHighlighted);
        }
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Test Minimal JSON")]
    public void TestMinimalJson()
    {
        // Minimal valid JSON that should work
        statusResponse = @"{
            ""currentDay"": 0,
            ""claimedDays"": [],
            ""canClaim"": true
        }";
        
        debug = true;
        isClaimed = false;
        FetchRewardsFromServer();
    }
    
    [ContextMenu("Test Full JSON")]
    public void TestFullJson()
    {
        statusResponse = @"{
            ""currentDay"": 2,
            ""claimedDays"": [0, 1],
            ""nextClaimTime"": ""2026-02-08T14:42:39.782Z"",
            ""canClaim"": true
        }";
        
        debug = true;
        isClaimed = false;
        FetchRewardsFromServer();
    }
    
    [ContextMenu("Test Invalid JSON")]
    public void TestInvalidJson()
    {
        statusResponse = @"{ invalid json }";
        debug = true;
        FetchRewardsFromServer();
    }
    #endif
    
    // Editor validation
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (sevenDaysRewards.Length != 7)
        {
            Debug.LogWarning("Daily rewards should have exactly 7 transforms");
            Array.Resize(ref sevenDaysRewards, 7);
        }
    }
    #endif
}