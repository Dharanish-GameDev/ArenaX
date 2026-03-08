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
        if(item.dailyLimit.HasValue)
        {
            descriptionText.text =  $"Daily Limit {item.dailyLimit.Value} times";
            purchaseButton.interactable = item.dailyLimit.Value > 0;
        }
        else
        {
            descriptionText.text = "Unlimited times";
        }
        
        // Use the formatted price from StoreManager
        priceText.text = StoreManager.Instance.FormatPrice(item);

        SetItemSprite(item);
        
        if (iapButton != null)
        {
            // IMPORTANT: Use productId for IAP, not the internal id
            iapButton.productId = item.productId; // This should be "coins_100m", not a GUID
            iapButton.onOrderConfirmed.AddListener(OnOrderConfirmed);
        }
    }

    private void OnOrderConfirmed(ConfirmedOrder order)
    {
        string receipt = order.Info.Receipt;
        string productId = currentItem.productId; // Use productId for the purchase
        
        StoreManager.Instance.PurchaseItem(productId, receipt, (response) =>
        {
            if (response != null && response.success)
            {
                Debug.Log($"{productId} Purchased successfully");
                EconomyManager.Instance.FetchWalletBalance();
                StoreManager.Instance.RefreshStoreItems();
            }
            else
            {
                Debug.LogError($"Purchase failed for {productId}");
            }
        });
    }

    private void SetItemSprite(StoreItem item)
    {
        if (itemIcon == null || itemSprites == null || itemSprites.Length == 0) return;

        int spriteIndex = 0;
        string type = item.type.ToLower();
        
        if (type.Contains("coin")) spriteIndex = 0;
        else if (type.Contains("diamond")) spriteIndex = 1;
        else if (type.Contains("silver")) spriteIndex = 2;
        
        spriteIndex = Mathf.Clamp(spriteIndex, 0, itemSprites.Length - 1);
        
        itemIcon.sprite = itemSprites[spriteIndex];
    }

    private void OnDestroy()
    {
        if (iapButton != null)
        {
            iapButton.onOrderConfirmed.RemoveListener(OnOrderConfirmed);
        }
    }
}