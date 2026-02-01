using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour, IUnityAdsInitializationListener
{
    public static AdsManager Instance { get; private set; }
    
    [Header("Game IDs")]
    [SerializeField] string _androidGameId;
    [SerializeField] string _iOSGameId;
    [SerializeField] bool _testMode = true;
    
    [Header("Ad Services")]
    [SerializeField] RewardedAdService _rewardedAdService;
    [SerializeField] InterstitialAdService _interstitialAdService;
    
    private string _gameId;
    
    // Access services through interface
    public IAdService RewardedAd => _rewardedAdService;
    public IAdService InterstitialAd => _interstitialAdService;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAds();
    }
    
    void InitializeAds()
    {
#if UNITY_IOS
        _gameId = _iOSGameId;
#elif UNITY_ANDROID || UNITY_EDITOR
        _gameId = _androidGameId;
#endif

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(_gameId, _testMode, this);
        }
        else if (Advertisement.isInitialized)
        {
            OnInitializationComplete();
        }
    }
    
    public void OnInitializationComplete()
    {
        Debug.Log("Ads initialized");
        
        // Initialize services
        RewardedAd?.Initialize();
        InterstitialAd?.Initialize();
    }
    
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Ads init failed: {error} - {message}");
    }
}