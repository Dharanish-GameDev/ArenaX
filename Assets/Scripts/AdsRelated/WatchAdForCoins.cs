using UnityEngine;
using UnityEngine.UI;

public class WatchAdForCoins : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] private GameObject gotCoinsDebugUI;
    void Start()
    {
        button.onClick.AddListener(ShowAdForCoins);
    }
    
    private void ShowAdForCoins()
    {
        AdsManager.Instance.RewardedAd.ShowAd(() =>
        {
            Debug.Log("You got 100 coins!");
            ShowGotCoinsDebugUI();
        });
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
