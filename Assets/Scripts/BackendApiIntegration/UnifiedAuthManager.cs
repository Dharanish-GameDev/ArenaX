using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arena.API.Models;
using UnityEngine;
using Facebook.Unity;
using Firebase.Auth;
using Google;
using Newtonsoft.Json;
using System.Text;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;

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
    public int profileImage;

    public static string ToDebugString(AuthRequest r)
    {
        if (r == null) return "[AuthRequest] NULL";

        return
            "========== AUTH REQUEST ==========\n" +
            $"idToken     : [ {r.idToken } ]\n" +
            $"deviceId    : [ {r.deviceId} ]\n" +
            $"platform    : [ {r.platform} ]\n" +
            $"email       : [ {r.email} ]\n" +
            $"name        : [ {r.name} ]\n" +
            $"profileImage: [ {r.profileImage} ]\n" +
            "==================================";
    }

    private static string Mask(string value, int visible = 6)
    {
        if (string.IsNullOrEmpty(value)) return "NULL";
        if (value.Length <= visible) return value;

        return value.Substring(0, visible) + "********";
    }
}

[Serializable]
public class UpdatedProfile
{
    public string message;
    public BackendUser user;
}

[Serializable]
public class BackendUser
{
    public string id;
    public string email;
    public string name;
    public string contact;
    public int profileImage;
    public string createdAt;
}

[Serializable]
public class AuthResponse
{
    public string accessToken;
    public string refreshToken;
    public bool isNewUser;
    public bool linked;
    public BackendUser user;
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
    public int profilePictureIndex = 1;
    public bool isGuest;
    public bool isNewUser;
    public string contact;

    public static UserData FromAuthResponse(AuthResponse response)
    {
        return new UserData
        {
            id = response.user.id,
            username = response.user.name,
            email = response.user.email,
            profilePictureIndex = response.user.profileImage,
            level = 1,
            coins = 1000,
            gems = 50,
            experience = 0,
            isGuest = false,
            isNewUser = response.isNewUser,
            contact = response.user.contact
        };
    }
}

[Serializable]
public class UpdateUserProfileRequest
{
    public string name;
    public string contact;
    public int profileImage;
}

#endregion

public class UnifiedAuthManager : MonoBehaviour
{
    public static UnifiedAuthManager Instance { get; private set; }

    [Header("Google")]
    [SerializeField] private string googleWebClientId;

    [Header("Auto Login")]
    [SerializeField] private bool autoLoginOnStart = true;

    [Header("Editor Testing")]
    [SerializeField] private EditorTestMode editorTestMode = EditorTestMode.None;

    [Header("Default Editor Mode")]
    [SerializeField] private AuthRequest defaultEditorAuthRequest;

    [Header("Parallel Sync Mode")]
    [SerializeField] private AuthRequest parallelSyncAuthRequest;
    
    [Header("Avatars SO")]
    [SerializeField] private AvatarsSO avatarsSO;
    [SerializeField] private Sprite defaultAvatar;

    public event Action<UserData> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action OnLogoutComplete;

    private FirebaseAuth firebaseAuth;
    private UserData currentUser;
    private string accessToken;
    private string refreshToken;
    private string deviceId;
    private string currentProvider;

    private string socialMediaName = "User";
    
    // Apple Sign-In
    private IAppleAuthManager appleAuthManager;
    private const string AppleUserIdKey = "AppleUserId";

    public enum EditorTestMode
    {
        None,
        DefaultEditor,
        ParallelSync
    }
    
    private const string PROP_NAME = "NAME";

    private void SetPhotonPlayerIdentity(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            username = "Player";

        Photon.Pun.PhotonNetwork.NickName = username;

        if (Photon.Pun.PhotonNetwork.LocalPlayer != null)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props[PROP_NAME] = username;
            Photon.Pun.PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
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

        // Initialize Apple Sign-In
        InitializeAppleSignIn();

#if UNITY_EDITOR
        editorTestMode = ClonesManager.IsClone()
            ? EditorTestMode.ParallelSync
            : EditorTestMode.DefaultEditor;
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

    private void Update()
    {
        // Update Apple Auth Manager
        if (appleAuthManager != null)
        {
            appleAuthManager.Update();
        }
    }

    #endregion

    #region Initialization

    private void InitializeAppleSignIn()
    {
        if (AppleAuthManager.IsCurrentPlatformSupported)
        {
            var deserializer = new PayloadDeserializer();
            appleAuthManager = new AppleAuthManager(deserializer);
        }
    }

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
            AuthRequest request = editorTestMode == EditorTestMode.DefaultEditor
                ? defaultEditorAuthRequest
                : parallelSyncAuthRequest;

            if (request != null && !string.IsNullOrEmpty(request.idToken))
            {
                LoginWithEditorRequest(request);
                return;
            }
        }
#endif

        accessToken = PlayerPrefs.GetString("AccessToken", "");
        if (!string.IsNullOrEmpty(accessToken))
        {
            ApiManager.Instance.SetAuthToken(accessToken);
            LoadCachedUserData();
        }
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

    #region Editor Login

    private void LoginWithEditorRequest(AuthRequest authRequest)
    {
        var request = new AuthRequest
        {
            idToken = authRequest.idToken,
            deviceId = string.IsNullOrEmpty(authRequest.deviceId) ? deviceId : authRequest.deviceId,
            platform = authRequest.platform,
            email = authRequest.email,
            name = authRequest.name,
            profileImage = authRequest.profileImage
        };

        SendAuthToBackend(request, "google");
    }

    #endregion

    #region Public Login API

    public void LoginWithGoogle()
    {
#if UNITY_EDITOR
        if (editorTestMode != EditorTestMode.None)
        {
            AuthRequest request = editorTestMode == EditorTestMode.DefaultEditor
                ? defaultEditorAuthRequest
                : parallelSyncAuthRequest;

            if (request != null && !string.IsNullOrEmpty(request.idToken))
            {
                LoginWithEditorRequest(request);
                return;
            }
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

   public void LoginWithApple()
{
#if UNITY_EDITOR
    if (editorTestMode != EditorTestMode.None)
    {
        AuthRequest request = editorTestMode == EditorTestMode.DefaultEditor
            ? defaultEditorAuthRequest
            : parallelSyncAuthRequest;

        if (request != null && !string.IsNullOrEmpty(request.idToken))
        {
            LoginWithEditorRequest(request);
            return;
        }
    }
#endif

    if (appleAuthManager == null)
    {
        OnLoginFailed?.Invoke("Apple Sign-In not supported on this platform");
        return;
    }

    var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

    appleAuthManager.LoginWithAppleId(
        loginArgs,
        credential =>
        {
            var appleIdCredential = credential as IAppleIDCredential;
            if (appleIdCredential != null)
            {
                HandleAppleSignInSuccess(appleIdCredential);
            }
        },
        error =>
        {
            // GetAuthorizationErrorCode returns the enum value directly, not nullable
            var authorizationErrorCode = error.GetAuthorizationErrorCode();
            
            string errorMessage;
            
            // Check if it's a user cancellation
            if (authorizationErrorCode == AuthorizationErrorCode.Canceled)
            {
                errorMessage = "Apple Sign-In was cancelled";
                Debug.Log($"[APPLE] {errorMessage}");
                // Don't trigger failure callback for user cancellation
                return;
            }
            
            // Handle other error codes
            switch (authorizationErrorCode)
            {
                case AuthorizationErrorCode.Unknown:
                    errorMessage = "Apple Sign-In failed with unknown error";
                    break;
                case AuthorizationErrorCode.InvalidResponse:
                    errorMessage = "Apple Sign-In received invalid response";
                    break;
                case AuthorizationErrorCode.NotHandled:
                    errorMessage = "Apple Sign-In was not handled";
                    break;
                case AuthorizationErrorCode.Failed:
                    errorMessage = "Apple Sign-In failed";
                    break;
                case AuthorizationErrorCode.Canceled:
                    errorMessage = "Apple Sign-In was cancelled";
                    break; // Already handled above, but keeping for completeness
                default:
                    errorMessage = $"Apple Sign-In failed with error code: {authorizationErrorCode}";
                    break;
            }
            
            Debug.LogError($"[APPLE] {errorMessage}");
            OnLoginFailed?.Invoke(errorMessage);
        });
}

    #endregion

    #region Apple Login Handlers

    private void HandleAppleSignInSuccess(IAppleIDCredential appleIdCredential)
    {
        // Save user ID for potential quick login
        var userId = appleIdCredential.User;
        PlayerPrefs.SetString(AppleUserIdKey, userId);
    
        // Get user info
        string email = appleIdCredential.Email;
        string name = "";
    
        if (appleIdCredential.FullName != null)
        {
            var fullName = appleIdCredential.FullName;
            name = $"{fullName.GivenName} {fullName.FamilyName}".Trim();
        
            if (string.IsNullOrEmpty(name))
                name = fullName.GivenName ?? fullName.FamilyName ?? "Apple User";
        }
    
        if (string.IsNullOrEmpty(name))
            name = "Apple User";
        
        if (string.IsNullOrEmpty(email))
            email = $"{userId}@apple.private.com"; // Fallback email for private relay

        // Get identity token
        string identityToken = appleIdCredential.IdentityToken != null
            ? Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0, appleIdCredential.IdentityToken.Length)
            : null;

        if (string.IsNullOrEmpty(identityToken))
        {
            OnLoginFailed?.Invoke("Failed to get Apple identity token");
            return;
        }

        var request = new AuthRequest
        {
            idToken = identityToken,
            deviceId = deviceId,
            platform = GetPlatformString(),
            email = email,
            name = name,
            profileImage = 1
        };

        SendAuthToBackend(request, "apple");
        socialMediaName = name;
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

        var request = new AuthRequest
        {
            idToken = user.IdToken,
            deviceId = deviceId,
            platform = GetPlatformString(),
            email = user.Email,
            name = user.DisplayName,
            profileImage = 1
        };

        SendAuthToBackend(request, "google");
    }

    #endregion

    #region Facebook Login

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
                profileImage = 1
            };

            SendAuthToBackend(request, "facebook");
        });
    }

    #endregion

    #region Backend

    private void SendAuthToBackend(AuthRequest request, string provider)
    {
        string endpoint = provider switch
        {
            "google" => ApiEndPoints.Auth.Google,
            "facebook" => ApiEndPoints.Auth.Facebook,
            "apple" => ApiEndPoints.Auth.Apple, // Make sure this endpoint exists
            _ => ApiEndPoints.Auth.Google
        };

        ApiManager.Instance.SendRequest<AuthResponse>(
            endpoint,
            RequestMethod.POST,
            res =>
            {
                string buffer = AuthRequest.ToDebugString(request);
                GUIUtility.systemCopyBuffer = buffer;
                OnBackendAuthSuccess(res, provider);
            },
            err => OnBackendAuthFailed(err, provider),
            JsonConvert.SerializeObject(request)
        );
    }

    private void OnBackendAuthSuccess(AuthResponse response, string provider)
    {
        accessToken = response.accessToken;
        refreshToken = response.refreshToken;

        PlayerPrefs.SetString("AccessToken", accessToken);
        PlayerPrefs.SetString("RefreshToken", refreshToken);

        ApiManager.Instance.SetAuthToken(accessToken);

        currentUser = UserData.FromAuthResponse(response);
        SetPhotonPlayerIdentity(currentUser.username);

        currentProvider = provider;

        PlayerPrefs.SetString("UserData", JsonConvert.SerializeObject(currentUser));

        Debug.Log($"[AUTH] Login Success → UID: {currentUser.id} (Provider: {provider})");
        Debug.Log("Profile Index : " + currentUser.profilePictureIndex);

        if (response.user.name == "string" || string.IsNullOrEmpty(response.user.name))
        {
            UpdateUserName(socialMediaName, () => {});
        }
        
        OnLoginSuccess?.Invoke(currentUser);
    }

    private void OnBackendAuthFailed(string error, string provider)
    {
        Debug.LogError($"[{provider.ToUpper()}] Auth Failed → {error}");
        OnLoginFailed?.Invoke(error);
    }

    #endregion

    #region Utilities

    private string GetPlatformString()
    {
#if UNITY_ANDROID
        return "ANDROID";
#elif UNITY_IOS
        return "IOS";
#elif UNITY_EDITOR
        return "EDITOR";
#else
        return "OTHER";
#endif
    }

    public void Logout()
    {
        // Provider sign-out
        try
        {
            GoogleSignIn.DefaultInstance.SignOut();
            GoogleSignIn.DefaultInstance.Disconnect();
        }
        catch (Exception e)
        {
            Debug.Log($"[AUTH] Google signout skipped/failed: {e.Message}");
        }

        try
        {
            if (FB.IsInitialized && FB.IsLoggedIn)
                FB.LogOut();
        }
        catch (Exception e)
        {
            Debug.Log($"[AUTH] Facebook logout skipped/failed: {e.Message}");
        }

        try
        {
            if (firebaseAuth == null) firebaseAuth = FirebaseAuth.DefaultInstance;
            firebaseAuth.SignOut();
        }
        catch (Exception e)
        {
            Debug.Log($"[AUTH] Firebase signout skipped/failed: {e.Message}");
        }

        // Note: Apple Sign-In doesn't have a sign-out method, just clear local data

        // Clear backend session tokens
        PlayerPrefs.DeleteKey("AccessToken");
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("UserData");
        PlayerPrefs.Save();

        // Clear API auth header
        ApiManager.Instance.ClearAuthToken();
        accessToken = null;
        refreshToken = null;
        currentUser = null;
        currentProvider = null;

        // Notify UI
        OnLogoutComplete?.Invoke();
    }

    public UserData GetCurrentUser() => currentUser;
    public bool IsLoggedIn() => currentUser != null;

    public Sprite GetProfilePictureForId(int id)
    {
        if (avatarsSO == null || avatarsSO.avatars == null || avatarsSO.avatars.Count == 0)
            return defaultAvatar;

        if (id < 0 || id >= avatarsSO.avatars.Count)
            return defaultAvatar;

        return avatarsSO.avatars[id] ?? defaultAvatar;
    }

    public void UpdateProfilePicture(int index, Action onComplete)
    {
        UpdateUserProfileRequest request = new UpdateUserProfileRequest();
        request.name = currentUser.username;
        request.contact = currentUser.contact ?? "123456789";
        request.profileImage = index;
        
        string json = JsonConvert.SerializeObject(request);
        
        ApiManager.Instance.SendRequest<UpdatedProfile>(ApiEndPoints.User.PutUserProfile, RequestMethod.PUT, (profile) =>
        {
            currentUser.profilePictureIndex = profile.user.profileImage;
            PlayerPrefs.SetString("UserData", JsonConvert.SerializeObject(currentUser));
            onComplete?.Invoke();
        }, (er) =>
        {
            Debug.Log(er);
            onComplete?.Invoke();
        }, json);
    }

    public void UpdateUserName(string username, Action onComplete)
    {
        if (string.IsNullOrEmpty(username)) 
        {
            onComplete?.Invoke();
            return;
        }

        UpdateUserProfileRequest request = new UpdateUserProfileRequest();
        request.name = username;
        request.contact = currentUser.contact ?? "123456789";
        request.profileImage = currentUser.profilePictureIndex;
        
        string json = JsonConvert.SerializeObject(request);
        
        ApiManager.Instance.SendRequest<UpdatedProfile>(ApiEndPoints.User.PutUserProfile, RequestMethod.PUT, (profile) =>
        {
            currentUser.username = profile.user.name;
            // Assuming LoginManager exists
            if (LoginManager.instance != null)
                LoginManager.instance.SetUsername(username);
            PlayerPrefs.SetString("UserData", JsonConvert.SerializeObject(currentUser));
            onComplete?.Invoke();
        }, (er) =>
        {
            Debug.Log(er);
            onComplete?.Invoke();
        }, json);
    }

    #endregion
}