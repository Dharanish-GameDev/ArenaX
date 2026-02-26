using System;
using Arena.API.Models;
using UnityEngine;
using UnityEngine.Events;

public class LoginManager : MonoBehaviour
{
   public static LoginManager instance;
   
   [SerializeField] private UnityEvent OnLoginEvent;
   public event Action OnUserLogin;
   
   [SerializeField] private string username;
   [SerializeField] private int avatarIndex = 0;

   private void Awake()
   {
      if (instance == null)
      {
         instance = this;
         DontDestroyOnLoad(gameObject);
      }
      else
      {
         Destroy(gameObject);
      }
      OnUserLogin += () =>
      {
         OnLoginEvent.Invoke();
      };
   }

   private void Start()
   {
      UnifiedAuthManager.Instance.OnLoginSuccess += data =>
      {
         Debug.Log("<color=green> Login successful!! </color>");
         TriggerLoginEvent();
         SetUsername(data.username);
         avatarIndex = data.profilePictureIndex - 1;
         SetProfilePicture();
         EconomyManager.Instance.FetchWalletBalance(() =>
         {
            Debug.Log("<color=green>Successfully retrieved wallet balance</color>");
         });
      };
   }

   public void TriggerLoginEvent()
   {
      OnUserLogin?.Invoke();
   }
   
   public void SetUsername(string username)
   {
      this.username = username;
      FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include)?.SetProfileName(username);
   }
   
   public void SetProfilePicture()
   {
      FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include) ?.SetProfileImage(GetProfilePicture());
   }

   public Sprite GetProfilePicture()
   {
      return UnifiedAuthManager.Instance.GetProfilePictureForId(avatarIndex);
   }

   public string GetUsername()
   {
      return username;
   }

   public void LoginWithGoogle()
   {
      UnifiedAuthManager.Instance.LoginWithGoogle();
   }

   public void LoginWithFacebook()
   {
      UnifiedAuthManager.Instance.LoginWithFacebook();
   }

   public void LoginWithApple()
   {
      UnifiedAuthManager.Instance.LoginWithApple();
   }

   public void CopyLoginWithGoogle()
   {
     //UnifiedAuthManager.Instance.CopyPayLoadBuffer();
   }
   
   public void LogOut()
   {
      UnifiedAuthManager.Instance.Logout();
   }
}
