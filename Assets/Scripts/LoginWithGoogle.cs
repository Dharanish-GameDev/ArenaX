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
    public TextMeshProUGUI Username;
    public TextMeshProUGUI UserEmail;
    public RawImage UserProfilePic;

    private bool isGoogleInitialized = false;

    private void Start()
    {
        InitFirebase();
        InitGoogleSignIn();
    }

    // ---------------- INIT ----------------

    void InitFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    void InitGoogleSignIn()
    {
        if (isGoogleInitialized) return;

        configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            RequestEmail = true,
            WebClientId = GoogleAPI
        };

        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestProfile = true;

        isGoogleInitialized = true;
    }

    // ---------------- LOGIN ----------------

    public void Login()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        GoogleSignIn.DefaultInstance.SignIn()
            .ContinueWith(OnGoogleSignInResult);
#else
        // Editor / non-Android fallback
        LoginManager.instance.TriggerLoginEvent();
#endif
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
        if (Username != null)
            Username.text = user.DisplayName;

        if (UserEmail != null)
            UserEmail.text = user.Email;

        if (user.PhotoUrl != null && UserProfilePic != null)
        {
            StartCoroutine(LoadProfileImage(user.PhotoUrl.ToString()));
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
            UserProfilePic.texture = tex;
        }
        else
        {
            Debug.LogWarning("Image Load Failed: " + request.error);
        }
    }
}
