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
    [SerializeField] private string baseUrl = "http://142.93.216.133:3000";
    [SerializeField] private float timeoutDuration = 15f;

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

    #region Auth Token

    public void SetAuthToken(string token)
    {
        authToken = token;
        PlayerPrefs.SetString("AuthToken", token);
        PlayerPrefs.Save();
    }

    public string GetAuthToken()
    {
        if (string.IsNullOrEmpty(authToken))
            authToken = PlayerPrefs.GetString("AuthToken", "");

        return authToken;
    }

    public void ClearAuthToken()
    {
        authToken = null;
        PlayerPrefs.DeleteKey("AuthToken");
    }

    #endregion

    #region Public API

    public void SendRequest<T>(string endpoint, RequestMethod method, Action<T> onSuccess, Action<string> onError, string jsonData = null)
    {
        StartCoroutine(SendRequestCoroutine(endpoint, method, onSuccess, onError, jsonData));
    }

    public void SendRequest(string endpoint, RequestMethod method, Action<string> onSuccess, Action<string> onError, string jsonData = null)
    {
        StartCoroutine(SendRequestCoroutine(endpoint, method, onSuccess, onError, jsonData));
    }

    #endregion

    #region Coroutines

    private System.Collections.IEnumerator SendRequestCoroutine<T>(string endpoint, RequestMethod method,
        Action<T> onSuccess, Action<string> onError, string jsonData)
    {
        Task<string> task = SendRequestInternal(endpoint, method, jsonData);

        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
        {
            onError?.Invoke(task.Exception?.GetBaseException().Message ?? "Unknown Error");
        }
        else
        {
            try
            {
                T result = JsonConvert.DeserializeObject<T>(task.Result);
                onSuccess?.Invoke(result);
            }
            catch (Exception ex)
            {
                onError?.Invoke($"JSON Parse Error: {ex.Message}");
            }
        }
    }

    private System.Collections.IEnumerator SendRequestCoroutine(string endpoint, RequestMethod method,
        Action<string> onSuccess, Action<string> onError, string jsonData)
    {
        Task<string> task = SendRequestInternal(endpoint, method, jsonData);

        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
        {
            onError?.Invoke(task.Exception?.GetBaseException().Message ?? "Unknown Error");
        }
        else
        {
            onSuccess?.Invoke(task.Result);
        }
    }

    #endregion

    #region Internal HTTP

    private async Task<string> SendRequestInternal(string endpoint, RequestMethod method, string jsonData = null)
    {
        string url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        Debug.Log($"[API] {method} → {url}");

        using (UnityWebRequest request = CreateRequest(url, method, jsonData))
        {
            request.timeout = Mathf.CeilToInt(timeoutDuration);

            var op = request.SendWebRequest();

            float elapsed = 0f;
            while (!op.isDone && elapsed < timeoutDuration)
            {
                elapsed += Time.deltaTime;
                await Task.Yield();
            }

            if (!op.isDone)
                throw new TimeoutException("API Request Timed Out");

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"API Error: {request.error} | Status: {request.responseCode} | Body: {request.downloadHandler.text}");

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
                request = new UnityWebRequest(url, "POST");
                request.downloadHandler = new DownloadHandlerBuffer();

                if (!string.IsNullOrEmpty(jsonData))
                {
                    byte[] body = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                break;

            case RequestMethod.PUT:
                request = new UnityWebRequest(url, "PUT");
                request.downloadHandler = new DownloadHandlerBuffer();

                if (!string.IsNullOrEmpty(jsonData))
                {
                    byte[] body = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.SetRequestHeader("Content-Type", "application/json");
                }
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
            request.SetRequestHeader("Authorization", $"Bearer {token}");

        return request;
    }

    #endregion
}

public enum RequestMethod
{
    GET,
    POST,
    PUT,
    DELETE
}
