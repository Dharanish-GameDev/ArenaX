using System;
using UnityEngine;
using UnityEngine.Events;

public class LoginManager : MonoBehaviour
{
   public static LoginManager instance;
   
   [SerializeField] private UnityEvent OnLoginEvent;
   public event Action OnUserLogin;
   
   [SerializeField] private string username;
   [SerializeField] private Texture profilePicture;
   


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
   public void TriggerLoginEvent()
   {
      OnUserLogin?.Invoke();
   }
   
   public void SetUsername(string username)
   {
      this.username = username;
      FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include)?.SetProfileName(username);
   }
   
   public void SetProfilePicture(Texture profilePicture)
   {
      this.profilePicture = profilePicture;
      FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include) ?.SetProfileImage(profilePicture);
   }

   public Texture GetProfilePicture()
   {
      return profilePicture;
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
}
