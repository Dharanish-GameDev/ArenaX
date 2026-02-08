using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EconomyUIHolder : MonoBehaviour
{
    [SerializeField] private List<TextMeshProUGUI> coinTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> silverTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> diamondTexts = new List<TextMeshProUGUI>();


    private void Start()
    {
        foreach (TextMeshProUGUI text in coinTexts)
        {
            EconomyManager.Instance.RegisterCoins((coin) =>
            {
                text.SetText(coin.ToString());
            });
        }
        
        foreach (TextMeshProUGUI text in silverTexts)
        {
            EconomyManager.Instance.RegisterSilver((silver) =>
            {
                text.SetText(silver.ToString());
            });
        }
        
        foreach (TextMeshProUGUI text in diamondTexts)
        {
            EconomyManager.Instance.RegisterDiamonds((diamond) =>
            {
                text.SetText(diamond.ToString());
            });
        }
    }

    private void OnDestroy()
    {
        foreach (TextMeshProUGUI text in coinTexts)
        {
            EconomyManager.Instance.UnregisterCoins((coin) =>
            {
                text.SetText(coin.ToString());
            });
        }
        
        foreach (TextMeshProUGUI text in silverTexts)
        {
            EconomyManager.Instance.UnregisterSilver((silver) =>
            {
                text.SetText(silver.ToString());
            });
        }
        
        foreach (TextMeshProUGUI text in diamondTexts)
        {
            EconomyManager.Instance.UnregisterDiamonds((diamond) =>
            {
                text.SetText(diamond.ToString());
            });
        }
    }
}
