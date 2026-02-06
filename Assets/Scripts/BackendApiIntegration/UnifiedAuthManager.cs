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
public class SocialAuthRequest
{
    public string token;
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
    public UserProfile user;
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
    
    public static UserData FromUserProfile(UserProfile profile)
    {
        return new UserData
        {
            id = profile.id,
            username = profile.name,
            email = profile.email,
            profilePictureUrl = profile.profileImage,
            level = 1,
            coins = 1000,
            gems = 50,
            experience = 0,
            isGuest = false
        };
    }
}

#endregion

public class UnifiedAuthManager : MonoBehaviour
{
    public static UnifiedAuthManager Instance { get; private set; }

    [Header("Configuration")]
    // [SerializeField] private string googleAuthEndpoint = "auth/google";
    // [SerializeField] private string facebookAuthEndpoint = "auth/facebook";
    // [SerializeField] private string refreshTokenEndpoint = "auth/refresh";
    [SerializeField] private string googleWebClientId = "982688911766-n723nhqprrrhm7et50mblmagqk3d0vue.apps.googleusercontent.com";
    [SerializeField] private bool autoLoginOnStart = true;
    
    [Header("Editor Debug Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private string editorTestToken = ""; // Paste your test JWT token here
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
    private bool isFirebaseReady;
    private bool isFacebookReady;

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
    }

    private IEnumerator Start()
    {
        DebugLog("Initializing Auth Manager...");
        
        #if !UNITY_EDITOR
        // Initialize real SDKs only in builds
        yield return InitializeFacebook();
        InitializeFirebase();
        #else
        // Skip SDK initialization in editor
        isFacebookReady = true;
        isFirebaseReady = true;
        DebugLog("Editor mode - Using test token for authentication");
        
        // Load test token from PlayerPrefs if exists
        if (string.IsNullOrEmpty(editorTestToken))
        {
            editorTestToken = PlayerPrefs.GetString("EditorTestToken", "");
        }
        #endif

        if (autoLoginOnStart)
        {
            yield return new WaitForSeconds(1f);
            AttemptAutoLogin();
        }
    }

    private void OnDestroy()
    {
        if (firebaseAuth != null)
            firebaseAuth.StateChanged -= OnFirebaseAuthStateChanged;
    }

    #endregion

    #region Initialization

    private IEnumerator InitializeFacebook()
    {
        if (!FB.IsInitialized)
        {
            FB.Init(() =>
            {
                isFacebookReady = FB.IsInitialized;
                if (isFacebookReady)
                    FB.ActivateApp();
            });
        }

        while (!FB.IsInitialized)
            yield return null;

        isFacebookReady = true;
    }

    private void InitializeFirebase()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;
        firebaseAuth.StateChanged += OnFirebaseAuthStateChanged;
        isFirebaseReady = true;
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

    private void OnFirebaseAuthStateChanged(object sender, EventArgs e)
    {
        firebaseUser = firebaseAuth.CurrentUser;
    }

    #endregion

    #region Auto Login

    private void AttemptAutoLogin()
    {
        DebugLog("Attempting auto login...");
        
        // Check for stored tokens
        accessToken = PlayerPrefs.GetString("AccessToken", "");
        refreshToken = PlayerPrefs.GetString("RefreshToken", "");

        if (!string.IsNullOrEmpty(accessToken))
        {
            DebugLog("Found stored access token");
            ValidateCurrentSession();
            return;
        }

        #if !UNITY_EDITOR
        // Real device auto-login
        if (firebaseAuth.CurrentUser != null)
        {
            HandleFirebaseAutoLogin();
            return;
        }

        if (FB.IsLoggedIn)
        {
            HandleFacebookAutoLogin();
            return;
        }
        #else
        // Editor auto-login with test token
        if (autoUseTestTokenInEditor && !string.IsNullOrEmpty(editorTestToken))
        {
            DebugLog("Auto-login with editor test token");
            LoginWithTestToken(editorTestToken);
        }
        #endif
    }

    private void ValidateCurrentSession()
    {
        ApiManager.Instance.SetAuthToken(accessToken);
        LoadCachedUserData();
    }

    private void LoadCachedUserData()
    {
        string userJson = PlayerPrefs.GetString("UserData", "");
        if (!string.IsNullOrEmpty(userJson))
        {
            try
            {
                currentUser = JsonConvert.DeserializeObject<UserData>(userJson);
                OnLoginSuccess?.Invoke(currentUser);
                DebugLog($"Welcome back {currentUser.username}");
            }
            catch (Exception e)
            {
                DebugLogError($"Failed to load cached user data: {e.Message}");
            }
        }
    }

    #endregion

    #region Public Login Methods

    public void LoginWithGoogle()
    {
        #if UNITY_EDITOR
        // In editor, use test token
        if (autoUseTestTokenInEditor && !string.IsNullOrEmpty(editorTestToken))
        {
            DebugLog("Editor: Using test token for Google login");
            LoginWithTestToken(editorTestToken);
            return;
        }
        #endif

        if (!isFirebaseReady)
        {
            OnLoginFailed?.Invoke("Firebase not ready");
            return;
        }

        DebugLog("Signing in with Google...");
        InitializeGoogle();

        #if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(GoogleLoginAndroid());
        #elif UNITY_IOS && !UNITY_EDITOR
        StartCoroutine(GoogleLoginIOS());
        #else
        // Fallback for editor or unsupported platforms
        OnLoginFailed?.Invoke("Google login not supported in editor. Use test token.");
        #endif
    }

    public void LoginWithFacebook()
    {
        #if UNITY_EDITOR
        // In editor, use test token
        if (autoUseTestTokenInEditor && !string.IsNullOrEmpty(editorTestToken))
        {
            DebugLog("Editor: Using test token for Facebook login");
            LoginWithTestToken(editorTestToken);
            return;
        }
        #endif

        if (!isFacebookReady)
        {
            OnLoginFailed?.Invoke("Facebook not ready");
            return;
        }

        DebugLog("Signing in with Facebook...");

        FB.LogInWithReadPermissions(
            new List<string> { "public_profile", "email" },
            result =>
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
        #if UNITY_EDITOR
        // In editor, use test token for guest too
        if (autoUseTestTokenInEditor && !string.IsNullOrEmpty(editorTestToken))
        {
            DebugLog("Editor: Using test token for guest login");
            LoginWithTestToken(editorTestToken);
            return;
        }
        #endif

        DebugLog("Creating guest account...");

        firebaseAuth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                OnLoginFailed?.Invoke("Guest login failed");
                return;
            }

            // Firebase SDK returns Task without Result property in some versions
            // We'll get the user from CurrentUser instead
            firebaseUser = firebaseAuth.CurrentUser;
            if (firebaseUser != null)
            {
                SyncFirebaseWithBackend("guest");
            }
            else
            {
                OnLoginFailed?.Invoke("Failed to get user after guest login");
            }
        });
    }

    #region Editor Test Token Login

    public void LoginWithTestToken(string token = null)
    {
        if (string.IsNullOrEmpty(token))
        {
            token = editorTestToken;
        }

        if (string.IsNullOrEmpty(token))
        {
            OnLoginFailed?.Invoke("No test token provided");
            return;
        }

        DebugLog($"Using test token: {token.Substring(0, Math.Min(30, token.Length))}...");
        
        // Set the token
        ApiManager.Instance.SetAuthToken(token);
        accessToken = token;
        PlayerPrefs.SetString("AccessToken", accessToken);
        
        // Create a test user
        currentUser = new UserData
        {
            id = "editor_test_user",
            username = "Editor Test User",
            email = "editor@test.com",
            level = 10,
            coins = 5000,
            gems = 250,
            experience = 15000,
            profilePictureUrl = "https://via.placeholder.com/150",
            createdAt = DateTime.Now.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            lastLoginAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            countryCode = "US",
            language = "en",
            isGuest = false
        };
        
        // Save user data
        SaveUserData();
        
        // Update LoginManager
        UpdateLoginManager();
        
        OnLoginSuccess?.Invoke(currentUser);
        DebugLog($"Logged in with test token as: {currentUser.username}");
    }

    public void SetTestToken(string token)
    {
        editorTestToken = token;
        PlayerPrefs.SetString("EditorTestToken", token);
        DebugLog("Test token saved");
    }

    #endregion

    #endregion

    #region Real Platform Login (Build Only)

    #if !UNITY_EDITOR
    
    private IEnumerator GoogleLoginAndroid()
    {
        var googleTask = GoogleSignIn.DefaultInstance.SignIn();

        yield return new WaitUntil(() => googleTask.IsCompleted);

        if (googleTask.IsFaulted)
        {
            DebugLogError($"Google Sign-In failed: {googleTask.Exception}");
            OnLoginFailed?.Invoke("Google Sign-In failed");
            yield break;
        }

        if (googleTask.IsCanceled)
        {
            OnLoginFailed?.Invoke("Google Sign-In cancelled");
            yield break;
        }

        var googleUser = googleTask.Result;
        var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

        // Sign in with Firebase using the credential
        firebaseAuth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(authTask =>
        {
            if (authTask.IsFaulted)
            {
                DebugLogError($"Firebase login failed: {authTask.Exception}");
                // Fallback to direct token
                SendSocialLoginToBackend("google", googleUser.IdToken, googleUser.Email, 
                    googleUser.DisplayName, googleUser.ImageUrl?.ToString());
                return;
            }

            if (authTask.IsCanceled)
            {
                OnLoginFailed?.Invoke("Firebase login cancelled");
                return;
            }

            // Get the current user from FirebaseAuth
            firebaseUser = firebaseAuth.CurrentUser;
            if (firebaseUser != null)
            {
                SyncFirebaseWithBackend("google");
            }
            else
            {
                OnLoginFailed?.Invoke("Failed to get Firebase user");
            }
        });
    }

    private IEnumerator GoogleLoginIOS()
    {
        var googleTask = GoogleSignIn.DefaultInstance.SignIn();

        yield return new WaitUntil(() => googleTask.IsCompleted);

        if (googleTask.IsFaulted)
        {
            DebugLogError($"Google Sign-In failed: {googleTask.Exception}");
            OnLoginFailed?.Invoke("Google Sign-In failed");
            yield break;
        }

        if (googleTask.IsCanceled)
        {
            OnLoginFailed?.Invoke("Google Sign-In cancelled");
            yield break;
        }

        var googleUser = googleTask.Result;
        SendSocialLoginToBackend("google", googleUser.IdToken, googleUser.Email,
            googleUser.DisplayName, googleUser.ImageUrl?.ToString());
    }

    #endif

    #endregion

    #region Firebase & Facebook Sync

    private void SyncFirebaseWithBackend(string provider)
    {
        if (firebaseUser == null)
        {
            DebugLogError("Firebase user is null, cannot sync with backend");
            OnLoginFailed?.Invoke("Firebase user not found");
            return;
        }

        firebaseUser.TokenAsync(false).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                DebugLogError($"Failed to get Firebase token: {task.Exception?.Message}");
                OnLoginFailed?.Invoke("Failed to get authentication token");
                return;
            }

            string idToken = task.Result;
            SendSocialLoginToBackend(provider, idToken, firebaseUser.Email,
                firebaseUser.DisplayName, firebaseUser.PhotoUrl?.ToString());
        });
    }

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
                : "";
            string photoUrl = $"https://graph.facebook.com/{id}/picture?type=large";
            string accessToken = AccessToken.CurrentAccessToken.TokenString;

            SendSocialLoginToBackend("facebook", accessToken, email, name, photoUrl);
        });
    }

    #endregion

    #region Backend API

    private void SendSocialLoginToBackend(string provider, string token, string email = null, string displayName = null, string photoUrl = null)
    {
        var request = new SocialAuthRequest
        {
            token = token
        };

        string json = JsonConvert.SerializeObject(request);
        string endpoint = provider == "google" ? ApiEndPoints.Auth.Google : ApiEndPoints.Auth.Facebook;

        ApiManager.Instance.SendRequest<AuthResponse>(
            endpoint,
            RequestMethod.POST,
            response => OnBackendAuthSuccess(response, provider, displayName, photoUrl),
            error => OnBackendAuthFailed(error, provider),
            json
        );
    }

    private void OnBackendAuthSuccess(AuthResponse response, string provider, string displayName = null, string photoUrl = null)
    {
        accessToken = response.accessToken;
        refreshToken = response.refreshToken;
        
        PlayerPrefs.SetString("AccessToken", accessToken);
        PlayerPrefs.SetString("RefreshToken", refreshToken);
        ApiManager.Instance.SetAuthToken(accessToken);

        currentUser = UserData.FromUserProfile(response.user);
        currentProvider = provider;
        
        // Update with additional info if not provided by backend
        if (string.IsNullOrEmpty(currentUser.username) && !string.IsNullOrEmpty(displayName))
            currentUser.username = displayName;
        
        if (string.IsNullOrEmpty(currentUser.profilePictureUrl) && !string.IsNullOrEmpty(photoUrl))
            currentUser.profilePictureUrl = photoUrl;
        
        SaveUserData();
        UpdateLoginManager();
        
        OnLoginSuccess?.Invoke(currentUser);
        DebugLog($"Welcome {currentUser.username}");
    }

    private void OnBackendAuthFailed(string error, string provider)
    {
        DebugLogError($"{provider} auth failed: {error}");
        OnLoginFailed?.Invoke($"Login failed: {error}");
    }

    #endregion

    #region Helper Methods

    private void HandleFirebaseAutoLogin()
    {
        if (firebaseUser == null)
        {
            DebugLog("No Firebase user found for auto login");
            return;
        }

        firebaseUser.TokenAsync(false).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                DebugLogError($"Failed to get Firebase token for auto login: {task.Exception?.Message}");
                return;
            }

            SendSocialLoginToBackend("google", task.Result, firebaseUser.Email,
                firebaseUser.DisplayName, firebaseUser.PhotoUrl?.ToString());
        });
    }

    private void HandleFacebookAutoLogin()
    {
        string accessToken = AccessToken.CurrentAccessToken?.TokenString;
        if (string.IsNullOrEmpty(accessToken))
        {
            DebugLog("No Facebook access token found");
            return;
        }

        FB.API("/me?fields=id,name,email", HttpMethod.GET, (IGraphResult result) =>
        {
            if (result.Error == null)
            {
                string id = result.ResultDictionary["id"].ToString();
                string name = result.ResultDictionary["name"].ToString();
                string email = result.ResultDictionary.ContainsKey("email")
                    ? result.ResultDictionary["email"].ToString()
                    : $"{id}@facebook.com";
                string photoUrl = $"https://graph.facebook.com/{id}/picture?type=large";

                SendSocialLoginToBackend("facebook", accessToken, email, name, photoUrl);
            }
        });
    }

    private void SaveUserData()
    {
        if (currentUser != null)
        {
            string userJson = JsonConvert.SerializeObject(currentUser);
            PlayerPrefs.SetString("UserData", userJson);
        }
    }

    private void UpdateLoginManager()
    {
        if (LoginManager.instance != null)
        {
            LoginManager.instance.SetUsername(currentUser.username);
            
            if (!string.IsNullOrEmpty(currentUser.profilePictureUrl))
            {
                StartCoroutine(LoadProfilePicture(currentUser.profilePictureUrl));
            }
            
            LoginManager.instance.TriggerLoginEvent();
        }
    }

    private IEnumerator LoadProfilePicture(string url)
    {
        using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
                LoginManager.instance.SetProfilePicture(texture);
            }
            else
            {
                DebugLogWarning($"Failed to load profile picture: {request.error}");
            }
        }
    }

    #endregion

    #region Public Methods

    public void Logout()
    {
        PlayerPrefs.DeleteKey("AccessToken");
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("UserData");
        
        #if !UNITY_EDITOR
        if (firebaseAuth != null) 
            firebaseAuth.SignOut();
        
        FB.LogOut();
        GoogleSignIn.DefaultInstance?.SignOut();
        #endif
        
        ApiManager.Instance.ClearAuthToken();
        currentUser = null;
        currentProvider = null;
        firebaseUser = null;
        
        OnLogoutComplete?.Invoke();
        DebugLog("Logged out successfully");
    }

    public UserData GetCurrentUser() => currentUser;
    public bool IsLoggedIn() => currentUser != null;
    public string GetAccessToken() => accessToken;
    public string GetCurrentProvider() => currentProvider;

    #endregion

    #region Debug Methods

    private void DebugLog(string message)
    {
        if (debugMode)
            Debug.Log($"[AuthManager] {message}");
    }

    private void DebugLogWarning(string message)
    {
        if (debugMode)
            Debug.LogWarning($"[AuthManager] {message}");
    }

    private void DebugLogError(string message)
    {
        Debug.LogError($"[AuthManager] {message}");
    }

    public void TestBackendAPI()
    {
        if (!IsLoggedIn())
        {
            DebugLogError("Not logged in. Login first.");
            return;
        }

        // Test wallet endpoint
        ApiManager.Instance.SendRequest(
            "wallet/balance",
            RequestMethod.GET,
            response =>
            {
                DebugLog($"Backend API works! Wallet response: {response}");
            },
            error =>
            {
                DebugLogError($"Backend API failed: {error}");
            }
        );
    }

    public void TestTokenRefresh()
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            DebugLogError("No refresh token available");
            return;
        }

        var request = new RefreshTokenRequest
        {
            refreshToken = refreshToken
        };

        string json = JsonConvert.SerializeObject(request);
        
        ApiManager.Instance.SendRequest<AuthResponse>(
            ApiEndPoints.Auth.Refresh,
            RequestMethod.POST,
            response =>
            {
                accessToken = response.accessToken;
                refreshToken = response.refreshToken;
                PlayerPrefs.SetString("AccessToken", accessToken);
                PlayerPrefs.SetString("RefreshToken", refreshToken);
                ApiManager.Instance.SetAuthToken(accessToken);
                DebugLog("Token refreshed successfully");
            },
            error =>
            {
                DebugLogError($"Token refresh failed: {error}");
            },
            json
        );
    }

    #endregion
}