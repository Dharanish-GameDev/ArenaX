using System;
using UnityEngine;
using Arena.API.Models;
using UnityEngine.UI;

public class StoreUI : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private Transform coinContainer;
    [SerializeField] private Transform diamondContainer;
    [SerializeField] private Transform silverContainer;

    [Header("Prefab")]
    [SerializeField] private GameObject storeItemPrefab;

    [Header("UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMPro.TextMeshProUGUI errorText;

    private void Start()
    {
        InitializeStoreUI();
    }

    private void InitializeStoreUI()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (errorPanel != null)
            errorPanel.SetActive(false);

        StoreManager.Instance.OnStoreItemsLoaded += OnStoreItemsLoaded;
        StoreManager.Instance.OnItemPurchased += OnItemPurchased;
        StoreManager.Instance.OnPurchaseFailed += OnPurchaseFailed;

        // IMPORTANT: Load store items
        StoreManager.Instance.InitializeStore();
    }

    private void OnStoreItemsLoaded()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        PopulateAllContainers();

        Debug.Log("Store Items Loaded!");
    }

    private void PopulateAllContainers()
    {
        PopulateContainer(StoreCategory.Coin, coinContainer);
        PopulateContainer(StoreCategory.Diamond, diamondContainer);
        PopulateContainer(StoreCategory.Silver, silverContainer);
    }

    private void PopulateContainer(StoreCategory category, Transform container)
    {
        if (container == null)
        {
            Debug.LogWarning($"Container missing for {category}");
            return;
        }

        if (storeItemPrefab == null)
        {
            Debug.LogError("Store Item Prefab not assigned!");
            return;
        }

        Transform parent = container;
        if(parent.TryGetComponent(out ScrollRect rect))
        {
            parent = rect.content;
        }

        // Clear old items
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }

        var items = StoreManager.Instance.GetStoreItemsByCategory(category);

        if (items == null || items.Count == 0)
        {
            Debug.Log($"No items found for category: {category}");
            return;
        }

        foreach (var item in items)
        {
            if(item.productId == "coins_1m") continue;
            GameObject itemGO = Instantiate(storeItemPrefab, parent);

            StoreItemUI storeItemUI = itemGO.GetComponent<StoreItemUI>();

            if (storeItemUI != null)
            {
                storeItemUI.Initialize(item);
            }
            else
            {
                Debug.LogError("StoreItemUI component missing on prefab!");
            }
        }
    }

    private void OnItemPurchased(StoreItem item)
    {
        Debug.Log($"Item purchased: {item.name}");

        var category = StoreManager.Instance.GetCategoryFromType(item.type);

        RefreshCategoryContainer(category);
    }

    private void RefreshCategoryContainer(StoreCategory category)
    {
        switch (category)
        {
            case StoreCategory.Coin:
                PopulateContainer(category, coinContainer);
                break;

            case StoreCategory.Diamond:
                PopulateContainer(category, diamondContainer);
                break;

            case StoreCategory.Silver:
                PopulateContainer(category, silverContainer);
                break;
        }
    }

    private void OnPurchaseFailed(string errorMessage)
    {
        Debug.LogError($"Purchase Failed: {errorMessage}");

        if (errorPanel != null)
            errorPanel.SetActive(true);

        if (errorText != null)
            errorText.text = $"Purchase Failed:\n{errorMessage}";
    }

    public void OnRetryButtonClicked()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        StoreManager.Instance.RefreshStoreItems();
    }

    public void OnCloseErrorButtonClicked()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (StoreManager.Instance != null)
        {
            StoreManager.Instance.OnStoreItemsLoaded -= OnStoreItemsLoaded;
            StoreManager.Instance.OnItemPurchased -= OnItemPurchased;
            StoreManager.Instance.OnPurchaseFailed -= OnPurchaseFailed;
        }
    }
}