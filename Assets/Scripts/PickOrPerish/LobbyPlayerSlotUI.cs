using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerSlotUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject placeHolder;

    public void Set(string playerName, string avatarUrl)
    {
        if(nameText != null)
            nameText.text = playerName;

        if (!string.IsNullOrEmpty(avatarUrl))
            ImageLoader.Load(avatarUrl, avatarImage);

        if (placeHolder != null)
        {
            placeHolder.SetActive(false);
        }
    }
}