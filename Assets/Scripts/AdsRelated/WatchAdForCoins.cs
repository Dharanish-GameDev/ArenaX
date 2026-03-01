using Arena.API.Models;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WatchAdForCoins : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] private GameObject gotCoinsDebugUI;
    [SerializeField] private TextMeshProUGUI countText;
    
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
                //OnShownAdsResponse(response);
                return;
            }
#endif
            ApiManager.Instance.SendRequest(ApiEndPoints.Rewards.WatchAd, RequestMethod.POST,
                (response) =>
                {
                    OnShownAdsResponse(response);
                },
                (er) =>
                {
                    
                },json);
        });
    }

    private void OnShownAdsResponse(string response)
    {
        EconomyManager.Instance.FetchWalletBalance(()=>
        {
            Debug.Log("Fecthing Wallet Balance After Ad");
        });

        ClaimRewardResponse rewardresponse = JsonConvert.DeserializeObject<ClaimRewardResponse>(response);
        
        Debug.Log(rewardresponse.ToString());
        FetchRemainingCountsToWatchAd();
        // EconomyManager.Instance.AddEconomy(response.reward.type, response.reward.amount);
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

    public void FetchRemainingCountsToWatchAd()
    {
        ApiManager.Instance.SendRequest<WatchAdsCount>(ApiEndPoints.Rewards.AdCount, RequestMethod.GET,(response =>
        {
            countText.SetText("x"+response.remainingToday.ToString());
            gameObject.SetActive(response.remainingToday > 0);
            Debug.Log(response.remainingToday);
            
        }), (er) =>
        {
            Debug.Log(er);
        });
    }
}
