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
    
    [SerializeField] private TextMeshProUGUI profileName;
    [SerializeField] private RawImage profileImage;
    
    
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
        
        
        if(Application.isEditor) return;
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
        if(!isLoadedAlready) return;
        SetProfileImage(LoginManager.instance.GetProfilePicture());
        SetProfileName(LoginManager.instance.GetUsername());
    }

    public void SetProfileName(string profileName)
    {
        if(!string.IsNullOrEmpty(profileName))
            this.profileName.text = profileName;
    }

    public void SetProfileImage(Texture profileImage)
    {
        this.profileImage.texture = profileImage;
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
        dailyRewardsHandler.FetchRewardsFromServer(() =>
        {
            lastScreen = GetCurrentEnabledScreen();
            landingPage.SetActive(false);
            profilePage.SetActive(false);
            dailyRewardsPage.SetActive(true);
        });
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
}
