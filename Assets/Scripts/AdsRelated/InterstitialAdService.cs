using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class InterstitialAdService : BaseAdService
{
    public override void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId))
        {
            _onCompleteCallback?.Invoke();
        }
        
        _onCompleteCallback = null;
        LoadAd();
    }
}