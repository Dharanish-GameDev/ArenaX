using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using Newtonsoft.Json;

#region Data Models

[Serializable]
public class AuthRequest
{
    public string idToken;
    public string deviceId;
    public string platform;
    public string email;
    public string name;
    public string profileImage;
}

[Serializable]
public class RefreshTokenRequest
{
    public string refreshToken;
}

[Serializable]
public class AuthResponse
{
    public string accessToken;
    public string refreshToken;
    public bool isNewUser;
    public bool linked;
}

[Serializable]
public class UserProfile
{
    public string id;
    public string name;
    public string email;
    public string profileImage;
}

[Serializable]
public class UserData
{
    public string id;
    public string username;
    public string email;
    public int level;
    public int experience;
    public int coins;
    public int gems;
    public string profilePictureUrl;
    public string createdAt;
    public string lastLoginAt;
    public string countryCode;
    public string language;
    public bool isGuest;
    public bool isNewUser;

    public static UserData FromAuthResponse(AuthResponse response, string userId, string username, string email, string profileImage)
    {
        return new UserData
        {
            id = userId,
            username = username,
            email = email,
            profilePictureUrl = profileImage,
            level = 1,
            coins = 1000,
            gems = 50,
            experience = 0,
            isGuest = false,
            isNewUser = response.isNewUser
        };
    }
}

#endregion

public class UnifiedAuthManager : MonoBehaviour
{
    public static UnifiedAuthManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private string googleWebClientId;
    [SerializeField] private bool autoLoginOnStart = true;

    [Header("Editor Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private string editorTestToken = "";
    [SerializeField] private bool autoUseTestTokenInEditor = true;

    public event Action<UserData> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action OnLogoutComplete;

    private FirebaseAuth firebaseAuth;
    private FirebaseUser firebaseUser;
    private UserData currentUser;
    private string accessToken;
    private string refreshToken;
    private string currentProvider;
    private string deviceId;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        deviceId = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
        {
            deviceId = PlayerPrefs.GetString("DeviceId", Guid.NewGuid().ToString());
            PlayerPrefs.SetString("DeviceId", deviceId);
        }
    }

    private IEnumerator Start()
    {
        #if !UNITY_EDITOR
        yield return InitializeFacebook();
        InitializeFirebase();
        #endif

        if (autoLoginOnStart)
        {
            yield return new WaitForSeconds(0.5f);
            AttemptAutoLogin();
        }
        
        yield return new WaitForSeconds(0.5f);
        #if UNITY_EDITOR

        if (debugMode)
        {
            Debug.Log("On Login Success Called From Editor!!");
            OnLoginSuccess?.Invoke(new  UserData());
        }
        
        #endif
    }

    #endregion

    #region Initialization

    private IEnumerator InitializeFacebook()
    {
        if (!FB.IsInitialized)
        {
            FB.Init(() =>
            {
                if (FB.IsInitialized)
                    FB.ActivateApp();
            });
        }

        while (!FB.IsInitialized)
            yield return null;
    }

    private void InitializeFirebase()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;
    }

    private void InitializeGoogle()
    {
        if (GoogleSignIn.Configuration == null)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = googleWebClientId,
                RequestEmail = true,
                RequestIdToken = true,
                RequestProfile = true
            };
        }
    }

    #endregion

    #region Auto Login

    private void AttemptAutoLogin()
    {
        accessToken = PlayerPrefs.GetString("AccessToken", "");
        refreshToken = PlayerPrefs.GetString("RefreshToken", "");

        if (!string.IsNullOrEmpty(accessToken))
        {
            ApiManager.Instance.SetAuthToken(accessToken);
            LoadCachedUserData();
            return;
        }

        #if UNITY_EDITOR
        if (autoUseTestTokenInEditor && !string.IsNullOrEmpty(editorTestToken))
            LoginWithTestToken(editorTestToken);
        #endif
    }

    private void LoadCachedUserData()
    {
        string json = PlayerPrefs.GetString("UserData", "");
        if (!string.IsNullOrEmpty(json))
        {
            currentUser = JsonConvert.DeserializeObject<UserData>(json);
            OnLoginSuccess?.Invoke(currentUser);
        }
    }

    #endregion

    #region Login Methods

    public void LoginWithGoogle()
    {
        #if UNITY_EDITOR
        if (autoUseTestTokenInEditor && !string.IsNullOrEmpty(editorTestToken))
        {
            LoginWithTestToken(editorTestToken);
            return;
        }
        #endif

        InitializeGoogle();
        StartCoroutine(GoogleLoginAndroid());
    }

    public void LoginWithFacebook()
    {
        FB.LogInWithReadPermissions(new List<string> { "public_profile", "email" }, result =>
        {
            if (!FB.IsLoggedIn)
            {
                OnLoginFailed?.Invoke("Facebook login cancelled");
                return;
            }

            SyncFacebookWithBackend();
        });
    }

    public void LoginAsGuest()
    {
        var request = new AuthRequest
        {
            idToken = "guest",
            deviceId = deviceId,
            platform = GetPlatformString(),
            email = "",
            name = "Guest",
            profileImage = ""
        };

        SendAuthToBackend(request, "guest");
    }

    #endregion

    #region Google Login

    private IEnumerator GoogleLoginAndroid()
    {
        var task = GoogleSignIn.DefaultInstance.SignIn();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            OnLoginFailed?.Invoke("Google Sign-in failed");
            yield break;
        }

        var user = task.Result;
        SendGoogleAuthToBackend(user.IdToken, user.Email, user.DisplayName, user.ImageUrl?.ToString());
    }
    string PayLoadBuffer = string.Empty;
    private void SendGoogleAuthToBackend(string token, string email, string name, string profile)
    {
        var request = new AuthRequest
        {
            idToken = token,
            deviceId = deviceId,
            platform = GetPlatformString(),
            email = email,
            name = name,
            profileImage = profile
        };
        
        PayLoadBuffer = $"Google Auth PayLoad - Id Token : [ {request.idToken} ], DeviceId : [ {request.deviceId} ] , Platform : [ {request.platform} ], Email : [ {request.email} ] , Name : [ {request.name} ], ProfileImage : [ {request.profileImage} ]";
        Debug.Log(PayLoadBuffer);
        SendAuthToBackend(request, "google");
    }
    
    public void CopyPayLoadBuffer()
    {
        GUIUtility.systemCopyBuffer = PayLoadBuffer;
    }


    #endregion

    #region Facebook Sync

    private void SyncFacebookWithBackend()
    {
        FB.API("/me?fields=id,name,email", HttpMethod.GET, result =>
        {
            if (result.Error != null)
            {
                OnLoginFailed?.Invoke("Facebook data error");
                return;
            }

            string id = result.ResultDictionary["id"].ToString();
            string name = result.ResultDictionary["name"].ToString();
            string email = result.ResultDictionary.ContainsKey("email")
                ? result.ResultDictionary["email"].ToString()
                : $"{id}@facebook.com";

            var request = new AuthRequest
            {
                idToken = AccessToken.CurrentAccessToken.TokenString,
                deviceId = deviceId,
                platform = GetPlatformString(),
                email = email,
                name = name,
                profileImage = $"https://graph.facebook.com/{id}/picture?type=large"
            };

            SendAuthToBackend(request, "facebook");
        });
    }

    #endregion

    #region Editor Test Token

    public void LoginWithTestToken(string token)
    {
        var request = new AuthRequest
        {
            idToken = token,
            deviceId = deviceId,
            platform = GetPlatformString(),
            email = "editor@test.com",
            name = "Editor",
            profileImage = ""
        };

        SendAuthToBackend(request, "google");
    }

    #endregion

    #region Backend

    private void SendAuthToBackend(AuthRequest request, string provider)
    {
        string endpoint = provider switch
        {
            "google" => ApiEndPoints.Auth.Google,
            "facebook" => ApiEndPoints.Auth.Facebook,
            // "guest" => ApiEndPoints.Auth.Guest,
            _ => ApiEndPoints.Auth.Google
        };

        string json = JsonConvert.SerializeObject(request);

        ApiManager.Instance.SendRequest<AuthResponse>(
            endpoint,
            RequestMethod.POST,
            res => OnBackendAuthSuccess(res, provider, request.name, request.profileImage),
            err => OnBackendAuthFailed(err, provider),
            json
        );
    }

    private void OnBackendAuthSuccess(AuthResponse response, string provider, string name, string photo)
    {
        accessToken = response.accessToken;
        refreshToken = response.refreshToken;

        PlayerPrefs.SetString("AccessToken", accessToken);
        PlayerPrefs.SetString("RefreshToken", refreshToken);

        ApiManager.Instance.SetAuthToken(accessToken);

        currentUser = UserData.FromAuthResponse(
            response,
            $"{provider}_{Guid.NewGuid()}",
            name,
            name,
            photo
        );

        currentProvider = provider;

        PlayerPrefs.SetString("UserData", JsonConvert.SerializeObject(currentUser));
        
        PayLoadBuffer = PayLoadBuffer + ", AccessToken : [ " + accessToken + " ] & RefreshToken : [ " + refreshToken + " ]";
        CopyPayLoadBuffer();
        Debug.Log(PayLoadBuffer);
        OnLoginSuccess?.Invoke(currentUser);
    }

    private void OnBackendAuthFailed(string error, string provider)
    {
        Debug.LogError($"{provider} auth failed → {error}");
        OnLoginFailed?.Invoke(error);
    }

    #endregion

    #region Utilities

    private string GetPlatformString()
    {
        #if UNITY_ANDROID
        return "android";
        #elif UNITY_IOS
        return "ios";
        #elif UNITY_EDITOR
        return "editor";
        #else
        return "other";
        #endif
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("AccessToken");
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("UserData");

        ApiManager.Instance.ClearAuthToken();
        currentUser = null;

        OnLogoutComplete?.Invoke();
    }

    public UserData GetCurrentUser() => currentUser;
    public bool IsLoggedIn() => currentUser != null;

    #endregion
}
