using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arena.API.Models;

public class StoreItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Image itemIcon;
    
    [Header("Sprites")]
    [SerializeField] private Sprite[] itemSprites;

    private StoreItem currentItem;
    private Action<StoreItem> onPurchaseCallback;

    public void Initialize(StoreItem item, Action<StoreItem> onPurchase)
    {
        currentItem = item;
        onPurchaseCallback = onPurchase;

        itemNameText.text = item.name;
        descriptionText.text = item.description;
        priceText.text = StoreManager.Instance.FormatPrice(item);

        SetItemSprite(item);

        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnPurchaseClicked);
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

    private void OnPurchaseClicked()
    {
        onPurchaseCallback?.Invoke(currentItem);
    }
}