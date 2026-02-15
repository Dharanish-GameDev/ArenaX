using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerSlotUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject placeHolder;

    public void Set(string playerName, string avatarIndex)
    {
        if(nameText != null)
            nameText.text = playerName;
        
        if (int.TryParse(avatarIndex, out int index))
        {
            avatarImage.sprite = UnifiedAuthManager.Instance.GetProfilePictureForId(index - 1);
        }

        if (placeHolder != null)
        {
            placeHolder.SetActive(false);
        }
    }
}