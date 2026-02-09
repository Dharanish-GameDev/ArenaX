using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class RewardedAdService : BaseAdService
{
    public string lastShownAdId = string.Empty;
    public override void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            lastShownAdId = adUnitId;
            _onCompleteCallback?.Invoke();
        }
        
        _onCompleteCallback = null;
        LoadAd();
    }
}