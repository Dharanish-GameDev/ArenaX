using System;
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
    public void GetFriendsList(
        int page,
        int limit,
        FriendshipStatus status,
        Action<FriendListResponse> onSuccess,
        Action<string> onError)
    {
        string url =
            $"{ApiEndPoints.User.GetUserList}?page={page}&limit={limit}&status={status}";

        ApiManager.Instance.SendRequest<FriendListResponse>(
            url,
            RequestMethod.GET,
            (response) =>
            {
                Debug.Log($"Friends list => Page:{response.page}  Count:{response.users?.Count ?? 0}  Total:{response.total}");
                onSuccess?.Invoke(response);
            },
            (error) =>
            {
                Debug.LogError("Friends list fetch failed => " + error);
                onError?.Invoke(error);
            }
        );
    }


    public void GetIncomingFriendRequests(System.Action<FriendRequestsResponse> onSuccess, System.Action<string> onError)
    {
        ApiManager.Instance.SendRequest<FriendRequestsResponse>(
            ApiEndPoints.Friends.IncomingRequest,
            RequestMethod.GET,
            (response) =>
            {
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
    /// Send friend request to another user
    /// </summary>
    public void SendFriendRequest(string friendId, System.Action<string> onComplete)
    {
        var requestData = new SendFriendRequest
        {
            toUserId = friendId
        };

        string json = JsonConvert.SerializeObject(requestData);

        ApiManager.Instance.SendRequest(
            ApiEndPoints.Friends.SendRequest,
            RequestMethod.POST,
            (response) =>
            {
                Debug.Log("Friend request sent Response : " + response);
                // if (response != null && response.success)
                // {
                //     Debug.Log($"Friend request sent to {friendId}");
                //     onComplete?.Invoke(true, response.message ?? "Request sent successfully");
                // }
                // else
                // {
                //     Debug.LogWarning($"Friend request failed for {friendId}");
                //     onComplete?.Invoke(false, response?.message ?? "Failed to send request");
                // }
            },
            (error) =>
            {
                Debug.LogError($"Failed to send friend request: {error}");
                //onComplete?.Invoke(false, error);
            },
            json
        );
    }

    /// <summary>
    /// Accept or decline a friend request
    /// </summary>
    public void RespondToFriendRequest(
        string requestId,
        bool accept,
        System.Action<bool, string> onComplete)
    {
        var requestData = new RespondFriendRequest
        {
            requestId = requestId,
            action = accept ? "ACCEPTED" : "REJECTED"
        };

        string json = JsonConvert.SerializeObject(requestData);
        Debug.Log("Respond Payload => " + json);

        ApiManager.Instance.SendRequest(
            ApiEndPoints.Friends.RespondRequest,
            RequestMethod.POST,
            (response) =>
            {
                Debug.Log("Friend request respond success: " + response);
                onComplete?.Invoke(true, response);
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