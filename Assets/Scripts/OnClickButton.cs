using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    [SerializeField] private GameObject landingPanel;     // Main Menu Screen
    [SerializeField] private GameObject coinStorePanel;   // Coin Store Panel
    [SerializeField] private GameObject settingpanel;
    [SerializeField] private GameObject FriendPanel;

    public void OpenCoinStore()
    {
        coinStorePanel.SetActive(true);
        landingPanel.SetActive(false);
    }
    public void OpenMainMenu()
    {
        coinStorePanel.SetActive(false);
        landingPanel.SetActive(true);
        settingpanel.SetActive(false);
        FriendPanel.SetActive(false);
    }
    public void OpenSettings()
    {
        settingpanel.SetActive(true);
        landingPanel.SetActive(false);
    }
    public void friendPanel()
    {
        FriendPanel.SetActive(true);
        landingPanel.SetActive(false);
    }

}
