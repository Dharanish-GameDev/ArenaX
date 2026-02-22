using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectPlayerUI : MonoBehaviour
{
    [SerializeField] private Button friendRequestButton;
    
    [SerializeField] private Image profileImage;
    
    [SerializeField] private TextMeshProUGUI nameText;
    
    private string uid;
    private string profileIndex;
    private string playerName;

    public void SetPlayerUI(string uid, string profileIndex, string playerName, bool isInteractable)
    {
        if (int.TryParse(profileIndex, out int profileIndexInt))
        {
            profileImage.sprite = UnifiedAuthManager.Instance.GetProfilePictureForId(profileIndexInt - 1);
        }
        
        nameText.text = playerName;

        if (isInteractable)
        {
            friendRequestButton.interactable = true;
            friendRequestButton.onClick.RemoveAllListeners();
            friendRequestButton.onClick.AddListener(() =>
                {
                    GameManager.instance.ShowFriendRequestUIItem(uid, profileIndex, playerName);
                }
            );   
        }
        else
        {
            DisableUIEvent();
        }

        this.playerName = playerName;
        this.uid = uid;
        this.profileIndex = profileIndex;
    }

    public void DisableUIEvent()
    {
        friendRequestButton.interactable = false;
        friendRequestButton.onClick.RemoveAllListeners();
    }
   
}
