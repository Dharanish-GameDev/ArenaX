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
    
    [SerializeField] private TextMeshProUGUI profileName;
    [SerializeField] private RawImage profileImage;
    
    
    public static bool isLoadedAlready = false;


    private void Awake()
    {
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
}
