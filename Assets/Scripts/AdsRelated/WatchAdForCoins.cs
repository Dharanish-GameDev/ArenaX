using Arena.API.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class WatchAdForCoins : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] private GameObject gotCoinsDebugUI;
    
    #if UNITY_EDITOR
    
    [SerializeField] private string testRewardJson = string.Empty;
    
    [SerializeField] private bool debug = false;
    
    #endif
    void Start()
    {
        button.onClick.AddListener(ShowAdForCoins);
    }
    
    private void ShowAdForCoins()
    {
        AdsManager.Instance.RewardedAd.ShowAd(() =>
        {
            WatchAdRewardRequest request = new WatchAdRewardRequest();
            RewardedAdService rewardedAdService = AdsManager.Instance.RewardedAd as  RewardedAdService;
            if (rewardedAdService != null)
            {
                request.adId = rewardedAdService.lastShownAdId;
            }
            
            string json = JsonConvert.SerializeObject(request);
            
            
#if UNITY_EDITOR
            if (debug)
            {
                RewardResponse response = JsonConvert.DeserializeObject<RewardResponse>(testRewardJson);
                OnShownAdsResponse(response);
                return;
            }
#endif
            
            ApiManager.Instance.SendRequest<RewardResponse>(ApiEndPoints.Rewards.WatchAd, RequestMethod.POST,
                (response) =>
                {
                    OnShownAdsResponse(response);
                },
                (er) =>
                {
                    
                },json);
        });
    }

    private void OnShownAdsResponse(RewardResponse response)
    {
        Debug.Log("Got Reward :  " + response.reward.type + " Amount : " + response.reward.amount);
        EconomyManager.Instance.AddEconomy(response.reward.type, response.reward.amount);
        ShowGotCoinsDebugUI();
    }
    
    private void ShowGotCoinsDebugUI()
    {
        gotCoinsDebugUI?.SetActive(true);
        Invoke(nameof(HideGotCoinsDebugUI), 2f);
    }
    
    private void HideGotCoinsDebugUI()
    {
        gotCoinsDebugUI?.SetActive(false);
    }
}
