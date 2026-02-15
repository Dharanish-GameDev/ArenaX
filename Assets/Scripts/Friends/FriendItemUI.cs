using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Arena.API.Models;

public class FriendItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image profileImage;

    [SerializeField] private Button inviteBtn;

    private FriendUser friend;

    public void SetupFriend(FriendUser data)
    {
        friend = data;

        nameText.text = data.name;

        //inviteBtn.gameObject.SetActive(true);

        profileImage.sprite = UnifiedAuthManager.Instance.GetProfilePictureForId(friend.profileImage -1);
    }

    public void InviteFriend()
    {
        Debug.Log("Inviting Friend: " + friend.id);
        
    }
}