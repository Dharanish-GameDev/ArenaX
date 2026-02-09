using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Arena.API.Models;

public class FacebookFriendItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image profileImage;
    [SerializeField] private Button inviteBtn;

    private FacebookFriend friend;

    public void Setup(FacebookFriend data)
    {
        friend = data;
        nameText.text = data.name;
        LoadAvatar(data.profileImage);
    }

    private void LoadAvatar(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        ImageLoader.Load(url, profileImage);
    }

    public void Invite()
    {
        Debug.Log("FB Invite: " + friend.id);
        // FB Invite Flow
    }
}