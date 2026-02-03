using UnityEngine;
using UnityEngine.UI;

public class ProfileSelection : MonoBehaviour
{
    public Button[] Buttons; // Green rings
    private int selectedIndex = -1;
    
    [SerializeField] private Image _profileImage;

    void Start()
    {
        // Turn off all highlights at start
        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].transform.GetChild(1).gameObject.SetActive(false);
            int id = i;
            Buttons[i].onClick.AddListener(() => SelectIcon(id));
        }
        
        SelectIcon(1);
    }

    public void SelectIcon(int index)
    {
        // Disable previous
        if (selectedIndex >= 0)
            Buttons[selectedIndex].transform.GetChild(1).gameObject.SetActive(false);

        // Enable new
        Buttons[index].transform.GetChild(1).gameObject.SetActive(true);
        selectedIndex = index;

        Debug.Log("Selected Icon: " + index);
        
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
