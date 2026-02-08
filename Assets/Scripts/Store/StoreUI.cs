using System;
using System.Collections.Generic;
using UnityEngine;
using Arena.API.Models;

public class StoreUI : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private Transform coinContainer;
    [SerializeField] private Transform diamondContainer;
    [SerializeField] private Transform silverContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject coinItemPrefab;
    [SerializeField] private GameObject diamondItemPrefab;
    [SerializeField] private GameObject silverItemPrefab;

    [Header("UI Elements")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMPro.TextMeshProUGUI errorText;

    [Header("Category Tabs (Optional)")]
    [SerializeField] private UnityEngine.UI.Button coinTabButton;
    [SerializeField] private UnityEngine.UI.Button diamondTabButton;
    [SerializeField] private UnityEngine.UI.Button silverTabButton;
    [SerializeField] private GameObject coinTabContent;
    [SerializeField] private GameObject diamondTabContent;
    [SerializeField] private GameObject silverTabContent;

    private StoreCategory currentCategory = StoreCategory.Coin;

    private void Start()
    {
        InitializeStoreUI();
        SetupCategoryTabs();
    }

    private void InitializeStoreUI()
    {
        loadingPanel.SetActive(true);
        errorPanel.SetActive(false);

        // Subscribe to events
        StoreManager.Instance.OnAllItemsLoaded += OnAllItemsLoaded;
        StoreManager.Instance.OnItemPurchased += OnItemPurchased;
        StoreManager.Instance.OnPurchaseFailed += OnPurchaseFailed;

        // Subscribe to individual category events if needed
        StoreManager.Instance.OnCoinItemsLoaded += OnCoinItemsLoaded;
        StoreManager.Instance.OnDiamondItemsLoaded += OnDiamondItemsLoaded;
        StoreManager.Instance.OnSilverItemsLoaded += OnSilverItemsLoaded;

        StoreManager.Instance.InitializeStore();
    }

    private void SetupCategoryTabs()
    {
        if (coinTabButton != null)
        {
            coinTabButton.onClick.AddListener(() => SwitchCategory(StoreCategory.Coin));
            diamondTabButton.onClick.AddListener(() => SwitchCategory(StoreCategory.Diamond));
            silverTabButton.onClick.AddListener(() => SwitchCategory(StoreCategory.Silver));
            
            // Show coin category by default
            SwitchCategory(StoreCategory.Coin);
        }
    }

    private void OnAllItemsLoaded()
    {
        loadingPanel.SetActive(false);
        PopulateAllContainers();
    }

    private void OnCoinItemsLoaded()
    {
        PopulateCategoryContainer(StoreCategory.Coin);
    }

    private void OnDiamondItemsLoaded()
    {
        PopulateCategoryContainer(StoreCategory.Diamond);
    }

    private void OnSilverItemsLoaded()
    {
        PopulateCategoryContainer(StoreCategory.Silver);
    }

    private void PopulateAllContainers()
    {
        PopulateCategoryContainer(StoreCategory.Coin);
        PopulateCategoryContainer(StoreCategory.Diamond);
        PopulateCategoryContainer(StoreCategory.Silver);
    }

    private void PopulateCategoryContainer(StoreCategory category)
    {
        Transform container = null;
        GameObject prefab = null;
        UnityEngine.UI.ColorBlock tabColors = new UnityEngine.UI.ColorBlock();

        switch (category)
        {
            case StoreCategory.Coin:
                container = coinContainer;
                prefab = coinItemPrefab;
                if (coinTabButton != null)
                    tabColors = coinTabButton.colors;
                break;
            case StoreCategory.Diamond:
                container = diamondContainer;
                prefab = diamondItemPrefab;
                if (diamondTabButton != null)
                    tabColors = diamondTabButton.colors;
                break;
            case StoreCategory.Silver:
                container = silverContainer;
                prefab = silverItemPrefab;
                if (silverTabButton != null)
                    tabColors = silverTabButton.colors;
                break;
        }

        if (container == null || prefab == null) return;

        // Clear existing items
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Get items for this category
        var items = StoreManager.Instance.GetStoreItemsByCategory(category);
        
        if (items.Count == 0)
        {
            Debug.LogWarning($"No items found for category: {category}");
            // You could instantiate a "No items available" message here
            return;
        }

        // Instantiate items
        foreach (var item in items)
        {
            var itemGO = Instantiate(prefab, container);
            var storeItemUI = itemGO.GetComponent<StoreItemUI>();
            if (storeItemUI != null)
            {
                storeItemUI.Initialize(item, OnItemButtonClicked, category);
            }
        }

        Debug.Log($"Populated {category} container with {items.Count} items");
    }

    private void SwitchCategory(StoreCategory category)
    {
        currentCategory = category;

        // Update tab visuals
        if (coinTabContent != null)
        {
            coinTabContent.SetActive(category == StoreCategory.Coin);
            diamondTabContent.SetActive(category == StoreCategory.Diamond);
            silverTabContent.SetActive(category == StoreCategory.Silver);

            // Update button colors to show active state
            UpdateTabButtonColors(category);
        }
    }

    private void UpdateTabButtonColors(StoreCategory activeCategory)
    {
        if (coinTabButton == null) return;

        var normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        var selectedColor = new Color(1f, 1f, 1f, 1f);
        var disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        // Reset all buttons to normal
        var coinColors = coinTabButton.colors;
        coinColors.normalColor = normalColor;
        coinTabButton.colors = coinColors;

        var diamondColors = diamondTabButton.colors;
        diamondColors.normalColor = normalColor;
        diamondTabButton.colors = diamondColors;

        var silverColors = silverTabButton.colors;
        silverColors.normalColor = normalColor;
        silverTabButton.colors = silverColors;

        // Set active button color
        switch (activeCategory)
        {
            case StoreCategory.Coin:
                coinColors.normalColor = selectedColor;
                coinTabButton.colors = coinColors;
                break;
            case StoreCategory.Diamond:
                diamondColors.normalColor = selectedColor;
                diamondTabButton.colors = diamondColors;
                break;
            case StoreCategory.Silver:
                silverColors.normalColor = selectedColor;
                silverTabButton.colors = silverColors;
                break;
        }
    }

    private void OnItemButtonClicked(StoreItem item)
    {
        // Show confirmation dialog or directly purchase
        ShowPurchaseConfirmation(item);
    }

    private void ShowPurchaseConfirmation(StoreItem item)
    {
        // You could implement a confirmation dialog here
        // For now, purchase directly
        StoreManager.Instance.PurchaseItem(item.id, "", (response) =>
        {
            if (response.success)
            {
                Debug.Log($"Purchased {item.name} successfully!");
                ShowPurchaseSuccessMessage(item);
            }
        });
    }

    private void ShowPurchaseSuccessMessage(StoreItem item)
    {
        // Implement success message/effect
        Debug.Log($"Successfully purchased: {item.name}");
    }

    private void OnItemPurchased(StoreItem item)
    {
        // Refresh the specific category UI
        var category = StoreManager.Instance.GetCategoryFromType(item.type);
        PopulateCategoryContainer(category);
    }

    private void OnPurchaseFailed(string errorMessage)
    {
        errorPanel.SetActive(true);
        errorText.text = $"Purchase Failed: {errorMessage}";
        Debug.LogError($"Purchase failed: {errorMessage}");
    }

    public void OnRetryButtonClicked()
    {
        errorPanel.SetActive(false);
        loadingPanel.SetActive(true);
        StoreManager.Instance.RefreshStoreItems();
    }

    public void OnCloseErrorButtonClicked()
    {
        errorPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (StoreManager.Instance != null)
        {
            StoreManager.Instance.OnAllItemsLoaded -= OnAllItemsLoaded;
            StoreManager.Instance.OnItemPurchased -= OnItemPurchased;
            StoreManager.Instance.OnPurchaseFailed -= OnPurchaseFailed;
            StoreManager.Instance.OnCoinItemsLoaded -= OnCoinItemsLoaded;
            StoreManager.Instance.OnDiamondItemsLoaded -= OnDiamondItemsLoaded;
            StoreManager.Instance.OnSilverItemsLoaded -= OnSilverItemsLoaded;
        }
    }
}