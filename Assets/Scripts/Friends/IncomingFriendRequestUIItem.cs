using UnityEngine;
using TMPro;
using Arena.API.Models;
using UnityEngine.UI;

public class IncomingFriendRequestUIItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    private IncomingFriendRequestItem data;
    private IncomingFriendRequestsUIController controller;

    public void Setup(IncomingFriendRequestItem item, IncomingFriendRequestsUIController parent)
    {
        gameObject.SetActive(true);
        data = item;
        controller = parent;
        nameText.text = data.sender.name;
        acceptButton.onClick.AddListener(() => Respond(true));
        declineButton.onClick.AddListener(() => Respond(false));
    }
    
    private void Respond(bool accept)
    {
        FriendsManager.Instance.RespondToFriendRequest(data.requestId, accept, (success, s) =>
        {
            Debug.Log(s);
            if (success)
                controller.RemoveItem(this);
        });
    }
}