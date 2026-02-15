using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileSelection : MonoBehaviour
{
    public Button[] Buttons;
    
    [SerializeField] private TMP_InputField _profileName;
    private int selectedIndex = -1;
    
    [SerializeField] private Image _profileImage;

    private void Awake()
    {
        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].transform.GetChild(1).gameObject.SetActive(false);
            int id = i;
            Buttons[i].onClick.AddListener(() => SelectIcon(id));
        }
    }

    private void OnEnable()
    {
        if (UnifiedAuthManager.Instance.GetCurrentUser() != null)
        {
            _profileName.SetTextWithoutNotify(UnifiedAuthManager.Instance.GetCurrentUser()?.username);
            SelectIcon(UnifiedAuthManager.Instance.GetCurrentUser().profilePictureIndex - 1, true);
        }
    }

    void Start()
    {
        _profileName.onValueChanged.AddListener(OnUserNameValueChanged);
    }

    private void OnUserNameValueChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _profileName.text = value;
            UnifiedAuthManager.Instance.UpdateUserName(value, () =>
            {
                Debug.Log("UpdateUserName called");
            });
        }
    }

    public void SelectIcon(int index, bool isInitial = false)
    {
        // Disable previous
        if (selectedIndex >= 0)
            Buttons[selectedIndex].transform.GetChild(1).gameObject.SetActive(false);

        // Enable new
        Buttons[index].transform.GetChild(1).gameObject.SetActive(true);
        selectedIndex = index;

        Debug.Log("Selected Icon: " + index);

        if (!isInitial)
        {
            UnifiedAuthManager.Instance.UpdateProfilePicture(index + 1, () =>
            {
                Debug.Log("Update Profile Pic called");
            });   
        }
        
        SetIcon(index);
    }

    private void SetIcon(int index)
    {
        _profileImage.sprite = GetSpriteOfIndex(selectedIndex);
    }

    private Sprite GetSpriteOfIndex(int index)
    {
        return Buttons[index].transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite;
    }
}
