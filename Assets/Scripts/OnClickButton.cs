using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    [SerializeField] private GameObject landingPanel;     // Main Menu Screen
    [SerializeField] private GameObject coinStorePanel;   // Coin Store Panel

    public void OpenCoinStore()
    {
        coinStorePanel.SetActive(true);
        landingPanel.SetActive(false);
    }
    public void OpenMainMenu()
    {
        coinStorePanel.SetActive(false);
        landingPanel.SetActive(true);
    }
}
