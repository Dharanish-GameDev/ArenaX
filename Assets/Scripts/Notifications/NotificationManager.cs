using UnityEngine;
using Newtonsoft.Json;
using System;
using Arena.API.Models;

public class NotificationsManager : MonoBehaviour
{
    private static NotificationsManager instance;
    public static NotificationsManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("NotificationsManager");
                instance = go.AddComponent<NotificationsManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public void GetNotifications(Action<NotificationsResponse> onSuccess, Action<string> onError)
    {
        ApiManager.Instance.SendRequest<NotificationsResponse>(
            ApiEndPoints.Notifications.GetNotifications,
            RequestMethod.GET,
            (res) =>
            {
                Debug.Log($"Notifications received: {res?.notifications?.Count ?? 0}");
                onSuccess?.Invoke(res);
            },
            (err) =>
            {
                Debug.LogError("Notifications fetch failed: " + err);
                onError?.Invoke(err);
            }
        );
    }

    public void Respond(string notificationId, bool accept, Action<bool, string> onComplete)
    {
        var req = new RespondNotificationRequest
        {
            notificationId = notificationId,
            accept = accept
        };

        string json = JsonConvert.SerializeObject(req);

        ApiManager.Instance.SendRequest<BaseResponse>(
            ApiEndPoints.Notifications.Respond,
            RequestMethod.POST,
            (res) =>
            {
                if (res != null && res.success)
                {
                    onComplete?.Invoke(true, res.message);
                }
                else
                {
                    onComplete?.Invoke(false, res?.message ?? "Failed");
                }
            },
            (err) =>
            {
                Debug.LogError(err);
                onComplete?.Invoke(false, err);
            },
            json
        );
    }
}
