using System;
using System.Collections.Generic;
using UnityEngine;
using Arena.API.Models;

public class StoreItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMPro.TextMeshProUGUI itemNameText;
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
    [SerializeField] private TMPro.TextMeshProUGUI priceText;
    [SerializeField] private UnityEngine.UI.Button purchaseButton;
    [SerializeField] private UnityEngine.UI.Image backgroundImage;
    [SerializeField] private UnityEngine.UI.Image iconImage;

    [Header("Category Colors")]
    [SerializeField] private Color coinColor = new Color(1f, 0.84f, 0f, 1f); // Gold
    [SerializeField] private Color diamondColor = new Color(0.32f, 0.85f, 1f, 1f); // Blue
    [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f, 1f); // Silver

    [Header("Category Icons (Optional)")]
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private Sprite diamondIcon;
    [SerializeField] private Sprite silverIcon;

    private StoreItem currentItem;
    private StoreCategory itemCategory;
    private Action<StoreItem> onPurchaseCallback;

    public void Initialize(StoreItem item, Action<StoreItem> onPurchase, StoreCategory category)
    {
        currentItem = item;
        itemCategory = category;
        onPurchaseCallback = onPurchase;

        // Set UI elements
        itemNameText.text = item.name;
        descriptionText.text = item.description;
        priceText.text = StoreManager.Instance.FormatPrice(item);

        // Set category-specific visuals
        ApplyCategoryVisuals(category);

        // Setup button
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnPurchaseClicked);

        // Set button color based on category
        var buttonColors = purchaseButton.colors;
        buttonColors.normalColor = GetCategoryColor(category);
        purchaseButton.colors = buttonColors;
    }

    private void ApplyCategoryVisuals(StoreCategory category)
    {
        Color categoryColor = GetCategoryColor(category);
        
        // Set background color
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.2f);
        }

        // Set icon if available
        if (iconImage != null)
        {
            iconImage.color = categoryColor;
            
            // Set category-specific icon
            Sprite iconSprite = GetCategoryIcon(category);
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
            }
        }

        // Set price text color
        priceText.color = categoryColor;
    }

    private Color GetCategoryColor(StoreCategory category)
    {
        return category switch
        {
            StoreCategory.Coin => coinColor,
            StoreCategory.Diamond => diamondColor,
            StoreCategory.Silver => silverColor,
            _ => Color.white
        };
    }

    private Sprite GetCategoryIcon(StoreCategory category)
    {
        return category switch
        {
            StoreCategory.Coin => coinIcon,
            StoreCategory.Diamond => diamondIcon,
            StoreCategory.Silver => silverIcon,
            _ => null
        };
    }

    private void OnPurchaseClicked()
    {
        onPurchaseCallback?.Invoke(currentItem);
    }

    // Optional: Add hover effects or animations
    public void OnPointerEnter()
    {
        // Add hover effect
        if (backgroundImage != null)
        {
            var color = GetCategoryColor(itemCategory);
            backgroundImage.color = new Color(color.r, color.g, color.b, 0.3f);
        }
    }

    public void OnPointerExit()
    {
        // Reset hover effect
        if (backgroundImage != null)
        {
            var color = GetCategoryColor(itemCategory);
            backgroundImage.color = new Color(color.r, color.g, color.b, 0.2f);
        }
    }
}