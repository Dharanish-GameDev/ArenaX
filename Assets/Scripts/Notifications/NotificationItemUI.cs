using UnityEngine;
using TMPro;
using Arena.API.Models;

public class NotificationItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private GameObject actionButtons;

    private NotificationItem data;
    private NotificationsUIController controller;

    public void Setup(NotificationItem item, NotificationsUIController parent)
    {
        data = item;
        controller = parent;

        messageText.text = item.message;
        timeText.text = FormatTime(item.createdAt);

        actionButtons.SetActive(item.type == "friend_request");
    }

    public void Accept()
    {
        Respond(true);
    }

    public void Decline()
    {
        Respond(false);
    }

    private void Respond(bool accept)
    {
        NotificationsManager.Instance.Respond(data.id, accept,
            (success, msg) =>
            {
                Debug.Log(msg);
                if (success)
                    controller.RemoveItem(this);
            });
    }

    private string FormatTime(string iso)
    {
        if (System.DateTime.TryParse(iso, out var dt))
            return dt.ToLocalTime().ToString("dd MMM, HH:mm");

        return "";
    }
}