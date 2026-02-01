using UnityEngine;
using UnityEngine.Advertisements;
using System;

public abstract class BaseAdService : MonoBehaviour, IAdService, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] protected string _androidAdUnitId;
    [SerializeField] protected string _iOSAdUnitId;
    [SerializeField] protected GameObject _loadingPanel;
    
    protected string _adUnitId;
    protected Action _onCompleteCallback;
    protected bool _showLoadingOnLoad = false; // Control flag
    
    public bool IsAdReady { get; protected set; }
    
    public void Initialize()
    {
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID || UNITY_EDITOR
        _adUnitId = _androidAdUnitId;
#endif
        
        LoadAd(); // Initial load - NO loading UI
    }
    
    public virtual void LoadAd()
    {
        if (!Advertisement.isInitialized || string.IsNullOrEmpty(_adUnitId))
            return;
            
        if (_showLoadingOnLoad) // Only show if flag is true
        {
            ShowLoading();
        }
        
        Advertisement.Load(_adUnitId, this);
    }
    
    public virtual void ShowAd(Action onComplete = null)
    {
        if (!IsAdReady)
        {
            _onCompleteCallback = onComplete;
            _showLoadingOnLoad = true; // Show loading when called from ShowAd
            LoadAd();
            return;
        }
        
        _onCompleteCallback = onComplete;
        ShowLoading(); // Show loading when ad is about to show
        Advertisement.Show(_adUnitId, this);
    }
    
    // Load callbacks
    public virtual void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId.Equals(_adUnitId))
        {
            IsAdReady = true;
            HideLoading();
            _showLoadingOnLoad = false; // Reset flag
        }
    }
    
    public virtual void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Failed to load ad: {error} - {message}");
        HideLoading();
        _showLoadingOnLoad = false; // Reset flag
        Invoke(nameof(LoadAd), 5f);
    }
    
    // Show callbacks
    public virtual void OnUnityAdsShowStart(string adUnitId) => HideLoading();
    public virtual void OnUnityAdsShowClick(string adUnitId) { }
    
    public abstract void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState);
    
    public virtual void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Failed to show ad: {error} - {message}");
        HideLoading();
        LoadAd(); // Auto reload without loading UI
    }
    
    // Loading UI
    protected void ShowLoading()
    {
        if (_loadingPanel != null)
            _loadingPanel.SetActive(true);
    }
    
    protected void HideLoading()
    {
        if (_loadingPanel != null)
            _loadingPanel.SetActive(false);
    }
}