using System.Text;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using UnityEngine;

public class AppleAuthentication : MonoBehaviour
{
    private IAppleAuthManager appleAuthManager;

    void Start()
    {
        if (AppleAuthManager.IsCurrentPlatformSupported)
        {
            var deserializer = new PayloadDeserializer();
            this.appleAuthManager = new AppleAuthManager(deserializer);    
        }
    }

    void Update()
    {
        if (this.appleAuthManager != null)
        {
            this.appleAuthManager.Update();
        }
    }

    public void SignIn()
    {
        var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

        this.appleAuthManager.LoginWithAppleId(
            loginArgs,
            credential =>
            {
                var appleIdCredential = credential as IAppleIDCredential;
                if (appleIdCredential != null)
                {
                    var userId = appleIdCredential.User;
                    PlayerPrefs.SetString(AppleUserIdKey, userId);
                    var email = appleIdCredential.Email;
                    var fullName = appleIdCredential.FullName;
                    var identityToken = Encoding.UTF8.GetString(
                        appleIdCredential.IdentityToken,
                        0,
                        appleIdCredential.IdentityToken.Length);
                    var authorizationCode = Encoding.UTF8.GetString(
                        appleIdCredential.AuthorizationCode,
                        0,
                        appleIdCredential.AuthorizationCode.Length);
                }
            },
            error =>
            {
                var authorizationErrorCode = error.GetAuthorizationErrorCode();
            });
    }

    private const string AppleUserIdKey = "AppleUserId"; 
}
