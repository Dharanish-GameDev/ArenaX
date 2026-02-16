using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arena.API.Models;
using UnityEngine.Purchasing;

public class StoreItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Image itemIcon;
    [SerializeField] private CodelessIAPButton iapButton;
    
    [Header("Sprites")]
    [SerializeField] private Sprite[] itemSprites;

    private StoreItem currentItem;

    public void Initialize(StoreItem item)
    {
        currentItem = item;

        itemNameText.text = item.name;
        descriptionText.text = item.description;
        priceText.text = StoreManager.Instance.FormatPrice(item);

        SetItemSprite(item);
        
        if (iapButton != null)
        {
            iapButton.productId =  item.id;
            iapButton.onOrderConfirmed.AddListener(OnOrderConfirmed);
        }
    }

    private void OnOrderConfirmed(ConfirmedOrder order)
    {
        string receipt = order.Info.Receipt;
        string productId = currentItem.id;
        
        StoreManager.Instance.PurchaseItem(productId, receipt , (response =>
        {
            if (response.success)
            {
                Debug.Log(productId +" Purchased");
                EconomyManager.Instance.FetchWalletBalance();
            }
        }));
    }

    private void SetItemSprite(StoreItem item)
    {
        if (itemIcon == null || itemSprites == null || itemSprites.Length == 0) return;

        int spriteIndex = 0;
        
        if (item.type.Contains("coin")) spriteIndex = 0;
        else if (item.type.Contains("diamond")) spriteIndex = 1;
        else if (item.type.Contains("silver")) spriteIndex = 2;
        
        spriteIndex = Mathf.Clamp(spriteIndex, 0, itemSprites.Length - 1);
        
        itemIcon.sprite = itemSprites[spriteIndex];
    }
}