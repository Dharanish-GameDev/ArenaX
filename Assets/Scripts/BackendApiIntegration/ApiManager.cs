using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    public static ApiManager Instance { get; private set; }
    
    [Header("API Configuration")]
    [SerializeField] private string baseUrl = "https://your-api-url.com/api";
    [SerializeField] private float timeoutDuration = 10f;
    
    [Header("Authentication")]
    [SerializeField] private string authToken;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetAuthToken(string token)
    {
        authToken = token;
        PlayerPrefs.SetString("AuthToken", token);
    }
    
    public string GetAuthToken()
    {
        if (string.IsNullOrEmpty(authToken))
        {
            authToken = PlayerPrefs.GetString("AuthToken", "");
        }
        return authToken;
    }
    
    // Generic method with type parameter
    public void SendRequest<T>(string endpoint, RequestMethod method, Action<T> onSuccess, Action<string> onError, string jsonData = null)
    {
        StartCoroutine(SendRequestCoroutine<T>(endpoint, method, onSuccess, onError, jsonData));
    }
    
    // Non-generic method for string responses
    public void SendRequest(string endpoint, RequestMethod method, Action<string> onSuccess, Action<string> onError, string jsonData = null)
    {
        StartCoroutine(SendRequestCoroutine(endpoint, method, onSuccess, onError, jsonData));
    }
    
    private System.Collections.IEnumerator SendRequestCoroutine<T>(string endpoint, RequestMethod method, Action<T> onSuccess, Action<string> onError, string jsonData)
    {
        Task<string> requestTask = SendRequestInternal(endpoint, method, jsonData);
        
        while (!requestTask.IsCompleted)
        {
            yield return null;
        }
        
        if (requestTask.IsFaulted)
        {
            string errorMessage = requestTask.Exception?.GetBaseException().Message ?? "Unknown error";
            onError?.Invoke(errorMessage);
        }
        else
        {
            try
            {
                T result = JsonConvert.DeserializeObject<T>(requestTask.Result);
                onSuccess?.Invoke(result);
            }
            catch (Exception ex)
            {
                onError?.Invoke($"JSON Parse Error: {ex.Message}");
            }
        }
    }
    
    private System.Collections.IEnumerator SendRequestCoroutine(string endpoint, RequestMethod method, Action<string> onSuccess, Action<string> onError, string jsonData)
    {
        Task<string> requestTask = SendRequestInternal(endpoint, method, jsonData);
        
        while (!requestTask.IsCompleted)
        {
            yield return null;
        }
        
        if (requestTask.IsFaulted)
        {
            string errorMessage = requestTask.Exception?.GetBaseException().Message ?? "Unknown error";
            onError?.Invoke(errorMessage);
        }
        else
        {
            onSuccess?.Invoke(requestTask.Result);
        }
    }
    
    private async Task<string> SendRequestInternal(string endpoint, RequestMethod method, string jsonData = null)
    {
        string url = $"{baseUrl}/{endpoint}";
        
        using (UnityWebRequest request = CreateRequest(url, method, jsonData))
        {
            request.timeout = (int)timeoutDuration;
            
            var operation = request.SendWebRequest();
            
            float elapsedTime = 0f;
            while (!operation.isDone && elapsedTime < timeoutDuration)
            {
                elapsedTime += Time.deltaTime;
                await Task.Yield();
            }
            
            if (!operation.isDone)
            {
                throw new TimeoutException("Request timed out");
            }
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"API Error: {request.error} - Status: {request.responseCode}");
            }
            
            return request.downloadHandler.text;
        }
    }
    
    private UnityWebRequest CreateRequest(string url, RequestMethod method, string jsonData)
    {
        UnityWebRequest request;
        
        switch (method)
        {
            case RequestMethod.GET:
                request = UnityWebRequest.Get(url);
                break;
            case RequestMethod.POST:
                request = UnityWebRequest.PostWwwForm(url, "POST");
                if (!string.IsNullOrEmpty(jsonData))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                break;
            case RequestMethod.PUT:
                request = UnityWebRequest.Put(url, jsonData ?? "");
                request.SetRequestHeader("Content-Type", "application/json");
                break;
            case RequestMethod.DELETE:
                request = UnityWebRequest.Delete(url);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, null);
        }
        
        request.SetRequestHeader("Accept", "application/json");
        
        string token = GetAuthToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }
        
        return request;
    }

    public void ClearAuthToken()
    {
        authToken = null;
        PlayerPrefs.SetString("AuthToken", null);
    }
}

public enum RequestMethod
{
    GET,
    POST,
    PUT,
    DELETE
}