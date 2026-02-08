using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using System.Threading.Tasks;

public class UnityServiceInitializer : MonoBehaviour
{
    async void Awake()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            string profile = "p_" + Random.Range(100000, 999999).ToString(); // valid + short

            var options = new InitializationOptions()
                .SetProfile(profile);

            await UnityServices.InitializeAsync(options);

            Debug.Log("Unity Profile: " + profile);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Signed In Player ID: " + AuthenticationService.Instance.PlayerId);
    }
}