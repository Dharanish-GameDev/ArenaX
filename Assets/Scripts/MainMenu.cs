using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject loginScreen;
    [SerializeField] private GameObject landingPage;
    [SerializeField] private GameObject profilePage;
    [SerializeField] private GameObject dailyRewardsPage;
    [SerializeField] private GameObject coinsStorePage;
    [SerializeField] private GameObject notificationsPage;
    [SerializeField] private GameObject friendListPage;
    [SerializeField] private GameObject settingsPage;

    [Header("Popup")] [Space(5)] 
    [SerializeField]
    private GameObject logoutPopup;
    
    [Space(10)]
    
    [SerializeField] private TextMeshProUGUI profileName;
    [SerializeField] private Image profileImage;
    
    
    
    public static bool isLoadedAlready = false;

    private List<GameObject> screens = new List<GameObject>();
    
    private GameObject lastScreen;
    
    [SerializeField] private DailyRewardsHandler dailyRewardsHandler;


    private void Awake()
    {
        screens.Add(landingPage);
        screens.Add(profilePage);
        screens.Add(dailyRewardsPage);
        screens.Add(coinsStorePage);
        
        
        if (!isLoadedAlready)
        {
            loadingScreen.SetActive(true);
            if (loadingScreen.TryGetComponent<SmoothLoadingBar>(out SmoothLoadingBar loadingBar))
            {
                loadingBar.StartLoading();
                isLoadedAlready = true;
            }
            loginScreen.SetActive(false);
            landingPage.SetActive(false);
        }
        else
        {
            loadingScreen.SetActive(false);
            landingPage.SetActive(true);
            loginScreen.SetActive(false);
        }
    }

    private void Start()
    {
        LoginManager.instance.OnUserLogin += () =>
        {
            loginScreen?.SetActive(false);
            landingPage?.SetActive(true);
            
            StoreManager.Instance.InitializeStore(() =>
            {
                Debug.Log("Store Initialized After Login");
            });
        };
        UnifiedAuthManager.Instance.OnLogoutComplete += () =>
        {
            loginScreen.SetActive(true);
            landingPage.SetActive(false);
        };
        
        if(!isLoadedAlready) return;
        SetProfileImage(LoginManager.instance.GetProfilePicture());
        SetProfileName(LoginManager.instance.GetUsername());
    }
    

    public void SetProfileName(string profileName)
    {
        if(!string.IsNullOrEmpty(profileName))
            this.profileName.text = profileName;
    }

    public void SetProfileImage(Sprite profileImage)
    {
        if(this.profileImage == null) return;
        this.profileImage.sprite = profileImage;
    }

    private GameObject GetCurrentEnabledScreen()
    {
        GameObject currentScreen = screens[0];

        foreach (GameObject screen in screens)
        {
            if(screen.activeInHierarchy)
            {
                currentScreen = screen;
                break;
            }
        }
        
        return currentScreen;
    }

    public void ShowProfilePage()
    {
        lastScreen = GetCurrentEnabledScreen();
        landingPage.SetActive(false);
        profilePage.SetActive(true);
        // Here I can Make the Api Request
    }

    public void HideProfilePage()
    {
        profilePage.SetActive(false);
        if (lastScreen != null)
        {
            lastScreen.SetActive(true);
        }
        else
        {
            landingPage.SetActive(true);
        }
        lastScreen = null;
    }

    public void ShowDailyRewardsPage()
    {
        // Here I can Make The Daily Rewards Request
        lastScreen = GetCurrentEnabledScreen();
        landingPage.SetActive(false);
        profilePage.SetActive(false);
        dailyRewardsPage.SetActive(true);
        dailyRewardsHandler.FetchRewardsFromServer();
    }

    public void HideDailyRewardsPage()
    {
        dailyRewardsPage.SetActive(false);
        if (lastScreen != null)
        {
            lastScreen.SetActive(true);
        }
        else
        {
            landingPage.SetActive(true);
        }
        lastScreen = null;
    }

    public void ShowCoinsStorePage()
    {
        lastScreen = GetCurrentEnabledScreen();
        profilePage.gameObject.SetActive(false);
        landingPage.SetActive(false);
        coinsStorePage.SetActive(true);
        Invoke(nameof(LoadStoreDelay),0.5f);
    }

    private void LoadStoreDelay()
    {
        StoreManager.Instance.InitializeStore(() =>
        {
            Debug.Log("Store Initialized After Opening Store");
        });
    }

    public void HideCoinsStorePage()
    {
        coinsStorePage.SetActive(false);
        if (lastScreen != null)
        {
            lastScreen.SetActive(true);
        }
        else
        {
            landingPage.SetActive(true);
        }
        lastScreen = null;
    }

    public void ShowNotificationsPage()
    {
        lastScreen = GetCurrentEnabledScreen();
        lastScreen.gameObject.SetActive(false);
        notificationsPage.SetActive(true);
    }

    public void HideNotificationsPage()
    {
        notificationsPage.SetActive(false);
        
        if (lastScreen != null)
        {
            lastScreen.SetActive(true);
        }
        else
        {
            landingPage.SetActive(true);
        }
        lastScreen = null;
    }
    
    public void ShowFriendListPage()
    {
        lastScreen = GetCurrentEnabledScreen();
        // lastScreen.gameObject.SetActive(false);
        friendListPage.SetActive(true);
    }

    public void HideFriendListPage()
    {
        friendListPage.SetActive(false);
        if (lastScreen != null)
        {
            lastScreen.SetActive(true);
        }
        else
        {
            landingPage.SetActive(true);
        }
        lastScreen = null;
    }

    public void ShowSettingsPage()
    {
        lastScreen = GetCurrentEnabledScreen();
        lastScreen.gameObject.SetActive(false);
        settingsPage.SetActive(true);
    }

    public void HideSettingsPage()
    {
        settingsPage.SetActive(false);
        if (lastScreen != null)
        {
            lastScreen.SetActive(true);
        }
        else
        {
            landingPage.SetActive(true);
        }
        lastScreen = null;
    }

    public void HideAndShowLogOut()
    {
        HideSettingsPage();
        logoutPopup.gameObject.SetActive(true);
    }
}
