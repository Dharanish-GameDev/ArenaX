using UnityEngine;
using Arena.API.Models;
using Newtonsoft.Json;

public class FriendsManager : MonoBehaviour
{
    private static FriendsManager instance;
    public static FriendsManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("FriendsManager");
                instance = go.AddComponent<FriendsManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    /// <summary>
    /// Get list of friends from server
    /// </summary>
    public void GetFriendsList(System.Action<FriendListResponse> onSuccess, System.Action<string> onError)
    {
        ApiManager.Instance.SendRequest<FriendListResponse>(
            ApiEndPoints.Friends.List,
            RequestMethod.GET,
            (response) =>
            {
                Debug.Log($"Friends list received. Count: {response?.friends?.Count ?? 0}");
                onSuccess?.Invoke(response);
            },
            (error) =>
            {
                Debug.LogError($"Failed to get friends list: {error}");
                onError?.Invoke(error);
            }
        );
    }

    /// <summary>
    /// Get Facebook friends who are using the app
    /// </summary>
    public void GetFacebookFriends(System.Action<FacebookFriendsResponse> onSuccess, System.Action<string> onError)
    {
        ApiManager.Instance.SendRequest<FacebookFriendsResponse>(
            ApiEndPoints.Friends.FacebookFriends,
            RequestMethod.GET,
            (response) =>
            {
                Debug.Log($"Facebook friends received. Count: {response?.friends?.Count ?? 0}");
                onSuccess?.Invoke(response);
            },
            (error) =>
            {
                Debug.LogError($"Failed to get Facebook friends: {error}");
                onError?.Invoke(error);
            }
        );
    }

    /// <summary>
    /// Send friend request to another user
    /// </summary>
    public void SendFriendRequest(string friendId, System.Action<bool, string> onComplete)
    {
        var requestData = new SendFriendRequest
        {
            friendId = friendId
        };

        string json = JsonConvert.SerializeObject(requestData);

        ApiManager.Instance.SendRequest<BaseResponse>(
            ApiEndPoints.Friends.SendRequest,
            RequestMethod.POST,
            (response) =>
            {
                if (response != null && response.success)
                {
                    Debug.Log($"Friend request sent to {friendId}");
                    onComplete?.Invoke(true, response.message ?? "Request sent successfully");
                }
                else
                {
                    Debug.LogWarning($"Friend request failed for {friendId}");
                    onComplete?.Invoke(false, response?.message ?? "Failed to send request");
                }
            },
            (error) =>
            {
                Debug.LogError($"Failed to send friend request: {error}");
                onComplete?.Invoke(false, error);
            },
            json
        );
    }

    /// <summary>
    /// Accept or decline a friend request
    /// </summary>
    public void RespondToFriendRequest(string requestId, bool accept, System.Action<bool, string> onComplete)
    {
        var requestData = new RespondFriendRequest
        {
            requestId = requestId,
            accept = accept
        };

        string json = JsonConvert.SerializeObject(requestData);

        ApiManager.Instance.SendRequest<BaseResponse>(
            ApiEndPoints.Friends.RespondRequest,
            RequestMethod.POST,
            (response) =>
            {
                if (response != null && response.success)
                {
                    string action = accept ? "accepted" : "declined";
                    Debug.Log($"Friend request {action}: {requestId}");
                    onComplete?.Invoke(true, response.message ?? $"Request {action}");
                }
                else
                {
                    Debug.LogWarning($"Failed to respond to friend request: {requestId}");
                    onComplete?.Invoke(false, response?.message ?? "Failed to respond to request");
                }
            },
            (error) =>
            {
                Debug.LogError($"Failed to respond to friend request: {error}");
                onComplete?.Invoke(false, error);
            },
            json
        );
    }
}