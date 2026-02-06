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
   
   
   
   #if UNITY_EDITOR

   [SerializeField] private bool debug = false;
   [SerializeField] private string statusResponse;
   [SerializeField] private string claimRewardResponse;
   
   #endif
   
   public void FetchRewardsFromServer(Action onComplete = null)
   {
      #if UNITY_EDITOR

      if (debug)
      {
         if(!string.IsNullOrEmpty(statusResponse))
         {
            DailyRewardStatusResponse response = JsonConvert.DeserializeObject<DailyRewardStatusResponse>(statusResponse);
            if (response != null)
            {
               ApplyServerStausResponse(response);
               onComplete?.Invoke();
            } 
         }
         return;
      }
      
      #endif
      ApiManager.Instance.SendRequest<DailyRewardStatusResponse>(ApiEndPoints.Rewards.DailyStatus,RequestMethod.GET, (response) =>
      {
         ApplyServerStausResponse(response);
         onComplete?.Invoke();
      }, (er) =>
      {
         onComplete?.Invoke();
      });
   }

   private void ApplyServerStausResponse(DailyRewardStatusResponse response)
   {
      if(response == null) return;

      claimRewardButton.interactable = response.canClaim;
      
      claimRewardButton.onClick.RemoveAllListeners();
      
      claimRewardButton.onClick.AddListener(() =>
      {
         #if UNITY_EDITOR

         if (debug)
         {
            RewardResponse rewardResponse = JsonConvert.DeserializeObject<RewardResponse>(claimRewardResponse);
            OnClaimDailyReward(rewardResponse);
         }
         
         #endif
         
         
         ClaimDailyRewardRequest req = new ClaimDailyRewardRequest();
         req.day = response.currentDay;
         string json = JsonConvert.SerializeObject(req);
         // Claiming Reward from the Server
         ApiManager.Instance.SendRequest<RewardResponse>(ApiEndPoints.Rewards.ClaimDaily,RequestMethod.POST, (rewardResponse) =>
         {
            OnClaimDailyReward(rewardResponse);
         },
         (er) =>
         {
            
         },json);
      });

      int day = response.currentDay;
      
      // Need to Apply Visuals
      
      ApplyVisuals(response.currentDay,response.claimedDays, response.canClaim);
   }

   private void OnClaimDailyReward(RewardResponse response)
   {
      if (response.reward != null)
      {
         Debug.Log("Claiming RewardType " + response.reward.type + " With Amount " + response.reward.amount);
      }
      else
      {
         Debug.Log("Can't Claim Reward : " + response.message);
      }
   }

   private void ApplyVisuals(int currentDay, List<int> alreadyClaimedDays,bool canClaim)
   {
      for (int i = 0; i < sevenDaysRewards.Length; i++)
      {
         if (alreadyClaimedDays.Contains(i))
         {
            sevenDaysRewards[i].GetChild(0).gameObject.SetActive(true);
            sevenDaysRewards[i].GetChild(1).gameObject.SetActive(false);
         }
         else
         {
            sevenDaysRewards[i].GetChild(0).gameObject.SetActive(false);
            sevenDaysRewards[i].GetChild(1).gameObject.SetActive(true);
         }
      }
      
      if (canClaim)
      {
         sevenDaysRewards[currentDay].GetChild(0).gameObject.SetActive(false);
         sevenDaysRewards[currentDay].GetChild(1).gameObject.SetActive(false);
      }
   }
}
