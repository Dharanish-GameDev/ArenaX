using System;
using UnityEngine;
using UnityEngine.Events;

public class LoginManager : MonoBehaviour
{
   public static LoginManager instance;
   
   [SerializeField] private UnityEvent OnLoginEvent;
   public event Action OnUserLogin;


   private void Awake()
   {
      instance = this;
      OnUserLogin += () =>
      {
         OnLoginEvent.Invoke();
      };
   }

   [ContextMenu("Login")]
   public void TriggerLoginEvent()
   {
      OnUserLogin?.Invoke();
   }
}
