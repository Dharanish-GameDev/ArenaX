using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class RewardedAdService : BaseAdService
{
    // public static string adPlacement = string.Empty;
    //
    // public static string placement = string.Empty;
    // public static string adToken = string.Empty;
    public override void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            _onCompleteCallback?.Invoke();
        }
        
        _onCompleteCallback = null;
        LoadAd();
    }
}