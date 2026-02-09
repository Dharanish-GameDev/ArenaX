using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Arena.API.Models;

public class FriendItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image profileImage;

    [SerializeField] private Button inviteBtn;
    [SerializeField] private Button sendRequestBtn;

    private Friend friend;

    public void SetupFriend(Friend data)
    {
        friend = data;

        nameText.text = data.name;
        statusText.text = data.status;

        inviteBtn.gameObject.SetActive(data.status == "friend");
        sendRequestBtn.gameObject.SetActive(data.status != "friend");

        LoadAvatar(data.profileImage);
    }

    private void LoadAvatar(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        //StartCoroutine(ImageLoader.Load(url, profileImage));
        
        ImageLoader.Load(url, profileImage);
    }

    public void InviteFriend()
    {
        Debug.Log("Inviting Friend: " + friend.id);

        //RoomManager.Instance.InviteFriend(friend.id);
    }

    public void SendFriendRequest()
    {
        FriendsManager.Instance.SendFriendRequest(friend.id,
            (success, msg) =>
            {
                Debug.Log(msg);
                if (success)
                    sendRequestBtn.interactable = false;
            });
    }
}