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
            // profileImage = user.ImageUrl?.ToString()
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
                // profileImage = $"https://graph.facebook.com/{id}/picture?type=large"
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
                socialMediaName = request.name;
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

        Debug.Log($"[AUTH] Login Success → UID: {currentUser.id}");
        Debug.Log("Profile Index : " + currentUser.profilePictureIndex);

        if (response.user.name == "string")
        {
            UpdateUserName(socialMediaName,()=> {});
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
        // 0) Turn off auto-login for this session if you want
        // (Optional) PlayerPrefs.SetInt("ManualLogout", 1);

        // 1) Provider sign-out (IMPORTANT)
        try
        {
            // Google Sign-In: clears cached account so user must choose again
            GoogleSignIn.DefaultInstance.SignOut();
            GoogleSignIn.DefaultInstance.Disconnect();
        }
        catch (Exception e)
        {
            Debug.Log($"[AUTH] Google signout skipped/failed: {e.Message}");
        }

        try
        {
            // Facebook
            if (FB.IsInitialized && FB.IsLoggedIn)
                FB.LogOut();
        }
        catch (Exception e)
        {
            Debug.Log($"[AUTH] Facebook logout skipped/failed: {e.Message}");
        }

        try
        {
            // Firebase (even if you didn't use it directly here, safe)
            if (firebaseAuth == null) firebaseAuth = FirebaseAuth.DefaultInstance;
            firebaseAuth.SignOut();
        }
        catch (Exception e)
        {
            Debug.Log($"[AUTH] Firebase signout skipped/failed: {e.Message}");
        }

        // 2) Clear backend session tokens stored locally
        PlayerPrefs.DeleteKey("AccessToken");
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("UserData");
        PlayerPrefs.Save();

        // 3) Clear API auth header / cached user
        ApiManager.Instance.ClearAuthToken();
        accessToken = null;
        refreshToken = null;
        currentUser = null;
        currentProvider = null;

        // 4) Notify UI
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
       request.contact = "123456789";
       request.profileImage = index;
        
       string json = JsonConvert.SerializeObject(request);
        
       ApiManager.Instance.SendRequest<UpdatedProfile>(ApiEndPoints.User.PutUserProfile,RequestMethod.PUT,(profile =>
       {
           currentUser.profilePictureIndex = profile.user.profileImage;
           PlayerPrefs.SetString("UserData", JsonConvert.SerializeObject(currentUser));
       }), (er =>
       {
           Debug.Log(er);
       }),json);
    }

    public void UpdateUserName(string username, Action onComplete)
    {
        if(string.IsNullOrEmpty(username)) return;

        UpdateUserProfileRequest request = new UpdateUserProfileRequest();
        request.name = username;
        request.contact = "123456789";
        request.profileImage = currentUser.profilePictureIndex;
        
        string json = JsonConvert.SerializeObject(request);
        
        ApiManager.Instance.SendRequest<UpdatedProfile>(ApiEndPoints.User.PutUserProfile,RequestMethod.PUT,(profile =>
        {
            currentUser.username = profile.user.name;
            LoginManager.instance.SetUsername(username);
            PlayerPrefs.SetString("UserData", JsonConvert.SerializeObject(currentUser));
        }), (er =>
        {
            Debug.Log(er);
        }),json);
        
        
    }

    #endregion
}
