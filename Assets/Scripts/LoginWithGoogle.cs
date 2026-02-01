using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

using Firebase.Auth;
using Firebase.Extensions;
using Google;

public class LoginWithGoogle : MonoBehaviour
{
    [Header("Google OAuth")]
    [Tooltip("WEB CLIENT ID from Google Cloud Console")]
    public string GoogleAPI =
        "773319435632-bp3t6vdb82ju0u4segtpftkq40pcu3o7.apps.googleusercontent.com";

    private GoogleSignInConfiguration configuration;

    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("UI")]
    public TextMeshProUGUI UserEmail;

    private static bool isGoogleInitialized = false;
    private static bool configurationSet = false;

    private void Awake()
    {
        // Handle multiple instances gracefully
        var existingInstances = FindObjectsOfType<LoginWithGoogle>();
        if (existingInstances.Length > 1)
        {
            // If another instance exists, destroy this one
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitFirebase();
        InitGoogleSignIn();
        
        // Check if user is already authenticated with Firebase
        CheckExistingFirebaseAuth();
    }

    // ---------------- INIT ----------------

    void InitFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    void InitGoogleSignIn()
    {
        if (isGoogleInitialized) return;

        // Check if GoogleSignIn has already been configured
        if (!configurationSet)
        {
            try
            {
                configuration = new GoogleSignInConfiguration
                {
                    RequestIdToken = true,
                    RequestEmail = true,
                    WebClientId = GoogleAPI
                };

                GoogleSignIn.Configuration = configuration;
                GoogleSignIn.Configuration.UseGameSignIn = false;
                GoogleSignIn.Configuration.RequestProfile = true;
                
                configurationSet = true;
                Debug.Log("Google Sign-In configuration set successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to set Google Sign-In configuration: " + ex.Message);
                // Configuration may already be set, continue anyway
                configurationSet = true;
            }
        }

        isGoogleInitialized = true;
    }
    
    void CheckExistingFirebaseAuth()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            user = auth.CurrentUser;
            Debug.Log("User already authenticated with Firebase: " + user.Email);
            UpdateUI();
        }
    }

    // ---------------- LOGIN ----------------

    public void Login()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Check if user is already authenticated with Firebase
        if (auth.CurrentUser != null)
        {
            Debug.Log("User already authenticated with Firebase");
            UpdateUI();
            LoginManager.instance.TriggerLoginEvent();
            return;
        }

        // Start Google Sign-In
        GoogleSignIn.DefaultInstance.SignIn()
            .ContinueWith(OnGoogleSignInResult);
#else
        // Editor / non-Android fallback
        LoginManager.instance.TriggerLoginEvent();
#endif
    }

    // ---------------- LOGOUT ----------------
    
    public void Logout()
    {
        // Sign out from Google if available
        try
        {
            if (GoogleSignIn.DefaultInstance != null)
            {
                GoogleSignIn.DefaultInstance.SignOut();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Error signing out from Google: " + ex.Message);
        }
        
        // Sign out from Firebase
        if (auth != null)
        {
            auth.SignOut();
        }
        
        user = null;
        
        // Reset UI if available
        if (UserEmail != null)
            UserEmail.text = "";
            
        Debug.Log("Logged out from Google and Firebase");
    }

    // ---------------- GOOGLE RESULT ----------------

    void OnGoogleSignInResult(Task<GoogleSignInUser> task)
    {
        if (task.IsCanceled)
        {
            Debug.LogWarning("Google Sign-In Canceled");
            return;
        }

        if (task.IsFaulted)
        {
            foreach (var e in task.Exception.InnerExceptions)
            {
                Debug.LogError("Google Sign-In Error: " + e);
            }
            return;
        }

        GoogleSignInUser googleUser = task.Result;
        FirebaseAuthWithGoogle(googleUser.IdToken);
    }

    // ---------------- FIREBASE AUTH ----------------

    void FirebaseAuthWithGoogle(string idToken)
    {
        Credential credential =
            GoogleAuthProvider.GetCredential(idToken, null);

        auth.SignInWithCredentialAsync(credential)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogWarning("Firebase Auth Canceled");
                    return;
                }

                if (task.IsFaulted)
                {
                    Debug.LogError("Firebase Auth Error: " + task.Exception);
                    return;
                }

                user = task.Result;
                Debug.Log("Firebase Login Success");

                UpdateUI();
                LoginManager.instance.TriggerLoginEvent();
            });
    }

    // ---------------- UI ----------------

    void UpdateUI()
    {
        if (user != null)
        {
            LoginManager.instance.SetUsername(user.DisplayName);

            if (UserEmail != null)
                UserEmail.text = user.Email;

            if (user.PhotoUrl != null)
            {
                StartCoroutine(LoadProfileImage(user.PhotoUrl.ToString()));
            }
        }
    }

    // ---------------- IMAGE LOADER ----------------

    IEnumerator LoadProfileImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(request);
            LoginManager.instance.SetProfilePicture(tex);
        }
        else
        {
            Debug.LogWarning("Image Load Failed: " + request.error);
        }
    }

    // ---------------- GETTERS ----------------
    
    public bool IsAuthenticated()
    {
        return user != null;
    }
    
    public string GetUserEmail()
    {
        return user != null ? user.Email : "";
    }
    
    public string GetUserName()
    {
        return user != null ? user.DisplayName : "";
    }

    // ---------------- CLEANUP ----------------
    
    private void OnApplicationQuit()
    {
        // Reset static variables when application quits
        isGoogleInitialized = false;
        configurationSet = false;
    }
    
    private void OnDestroy()
    {
        // Only reset if this is being destroyed and not because of scene change
        // Since we use DontDestroyOnLoad, this should only happen when quitting
        if (!gameObject.scene.isLoaded)
        {
            isGoogleInitialized = false;
            configurationSet = false;
        }
    }
}