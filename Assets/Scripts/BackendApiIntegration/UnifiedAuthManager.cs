using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using Newtonsoft.Json;
#if UNITY_EDITOR
using ParrelSync;
#endif


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

    [Header("Editor Testing")]
    [SerializeField] private EditorTestMode editorTestMode = EditorTestMode.None;
    
    [Header("Default Editor Mode")]
    [SerializeField] private AuthRequest defaultEditorAuthRequest;
    
    [Header("Parallel Sync Mode")]
    [SerializeField] private AuthRequest parallelSyncAuthRequest;

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

    public enum EditorTestMode
    {
        None,
        DefaultEditor,
        ParallelSync
    }

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
        
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
            editorTestMode = EditorTestMode.ParallelSync;
        else
            editorTestMode = EditorTestMode.DefaultEditor;
#else
    editorTestMode = EditorTestMode.None;
#endif
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
        #if UNITY_EDITOR
        if (editorTestMode != EditorTestMode.None)
        {
            switch (editorTestMode)
            {
                case EditorTestMode.DefaultEditor:
                    if (defaultEditorAuthRequest != null && !string.IsNullOrEmpty(defaultEditorAuthRequest.idToken))
                    {
                        LoginWithEditorRequest(defaultEditorAuthRequest, "Default Editor");
                        return;
                    }
                    break;
                    
                case EditorTestMode.ParallelSync:
                    if (parallelSyncAuthRequest != null && !string.IsNullOrEmpty(parallelSyncAuthRequest.idToken))
                    {
                        LoginWithEditorRequest(parallelSyncAuthRequest, "Parallel Sync");
                        return;
                    }
                    break;
            }
        }
        #endif

        // accessToken = PlayerPrefs.GetString("AccessToken", "");
        // refreshToken = PlayerPrefs.GetString("RefreshToken", "");
        //
        // if (!string.IsNullOrEmpty(accessToken))
        // {
        //     ApiManager.Instance.SetAuthToken(accessToken);
        //     LoadCachedUserData();
        // }
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

    #region Editor Test Modes

    private void LoginWithEditorRequest(AuthRequest authRequest, string modeName)
    {
        Debug.Log($"[EDITOR {modeName}] Using AuthRequest for authentication");
        
        // Clone the request to avoid modifying the serialized object
        var request = new AuthRequest
        {
            idToken = authRequest.idToken,
            deviceId = string.IsNullOrEmpty(authRequest.deviceId) ? deviceId : authRequest.deviceId,
            platform = string.IsNullOrEmpty(authRequest.platform) ? GetPlatformString() : authRequest.platform,
            email = authRequest.email,
            name = authRequest.name,
            profileImage = authRequest.profileImage
        };

        SendAuthToBackend(request, "google");
    }

    #endregion

    #region Login Methods

    public void LoginWithGoogle()
    {
        #if UNITY_EDITOR
        if (editorTestMode != EditorTestMode.None)
        {
            switch (editorTestMode)
            {
                case EditorTestMode.DefaultEditor:
                    if (defaultEditorAuthRequest != null && !string.IsNullOrEmpty(defaultEditorAuthRequest.idToken))
                    {
                        LoginWithEditorRequest(defaultEditorAuthRequest, "Default Editor");
                        return;
                    }
                    break;
                    
                case EditorTestMode.ParallelSync:
                    if (parallelSyncAuthRequest != null && !string.IsNullOrEmpty(parallelSyncAuthRequest.idToken))
                    {
                        LoginWithEditorRequest(parallelSyncAuthRequest, "Parallel Sync");
                        return;
                    }
                    break;
            }
        }
        #endif

        InitializeGoogle();
        StartCoroutine(GoogleLoginAndroid());
    }

    public void LoginWithFacebook()
    {
        #if UNITY_EDITOR
        if (editorTestMode != EditorTestMode.None)
        {
            // For Facebook in editor, use whichever mode is active
            AuthRequest request = editorTestMode == EditorTestMode.DefaultEditor 
                ? defaultEditorAuthRequest 
                : parallelSyncAuthRequest;
                
            if (request != null && !string.IsNullOrEmpty(request.idToken))
            {
                LoginWithEditorRequest(request, editorTestMode.ToString());
                return;
            }
        }
        #endif

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

    #region Google Login (Build Only)

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

        SendAuthToBackend(request, "google");
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

    #region Backend Communication

    private void SendAuthToBackend(AuthRequest request, string provider)
    {
        string endpoint = provider switch
        {
            "google" => ApiEndPoints.Auth.Google,
            "facebook" => ApiEndPoints.Auth.Facebook,
            //"guest" => ApiEndPoints.Auth.Guest,
            _ => ApiEndPoints.Auth.Google
        };

        string json = JsonConvert.SerializeObject(request);
        Debug.Log($"[{provider.ToUpper()}] Sending auth request");

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
        OnLoginSuccess?.Invoke(currentUser);
    }

    private void OnBackendAuthFailed(string error, string provider)
    {
        Debug.LogError($"[{provider.ToUpper()}] Auth failed → {error}");
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

    #if UNITY_EDITOR
    
    [ContextMenu("Test Login with Default Editor Mode")]
    public void TestLoginWithDefaultEditor()
    {
        editorTestMode = EditorTestMode.DefaultEditor;
        LoginWithGoogle();
    }

    [ContextMenu("Test Login with Parallel Sync Mode")]
    public void TestLoginWithParallelSync()
    {
        editorTestMode = EditorTestMode.ParallelSync;
        LoginWithGoogle();
    }

    [ContextMenu("Fill Default Editor Example")]
    public void FillDefaultEditorExample()
    {
        defaultEditorAuthRequest = new AuthRequest
        {
            idToken = "test_token_123",
            deviceId = deviceId,
            platform = "editor",
            email = "test@example.com",
            name = "Test User",
            profileImage = ""
        };
        
        editorTestMode = EditorTestMode.DefaultEditor;
        Debug.Log("Default Editor request filled");
    }

    [ContextMenu("Copy Token from Mobile to Parallel Sync")]
    public void CopyTokenToParallelSync()
    {
        // This simulates copying token from mobile build logs
        // In reality, you'd paste the token manually in Inspector
        if (parallelSyncAuthRequest == null)
        {
            parallelSyncAuthRequest = new AuthRequest();
        }
        
        parallelSyncAuthRequest.deviceId = deviceId;
        parallelSyncAuthRequest.platform = "editor";
        
        // Token, email, name would be pasted manually
        Debug.Log("Ready to paste mobile token into Parallel Sync fields");
    }

    [ContextMenu("Switch to Default Editor Mode")]
    public void SwitchToDefaultEditorMode()
    {
        editorTestMode = EditorTestMode.DefaultEditor;
        Debug.Log("Switched to Default Editor Mode");
    }

    [ContextMenu("Switch to Parallel Sync Mode")]
    public void SwitchToParallelSyncMode()
    {
        editorTestMode = EditorTestMode.ParallelSync;
        Debug.Log("Switched to Parallel Sync Mode");
    }

    [ContextMenu("Disable Editor Test Mode")]
    public void DisableEditorTestMode()
    {
        editorTestMode = EditorTestMode.None;
        Debug.Log("Editor Test Mode disabled");
    }

    #endif
}