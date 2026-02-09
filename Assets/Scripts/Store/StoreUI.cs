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

    [Header("Single Prefab")]
    [SerializeField] private GameObject storeItemPrefab;

    [Header("UI Elements")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMPro.TextMeshProUGUI errorText;

    private void Start()
    {
        InitializeStoreUI();
    }

    private void InitializeStoreUI()
    {
        errorPanel.SetActive(false);

        StoreManager.Instance.OnStoreItemsLoaded += OnStoreItemsLoaded;
        StoreManager.Instance.OnItemPurchased += OnItemPurchased;
        StoreManager.Instance.OnPurchaseFailed += OnPurchaseFailed;

        // StoreManager.Instance.InitializeStore();
    }

    private void OnStoreItemsLoaded()
    {
        loadingPanel.SetActive(false);
        PopulateAllContainers();
        Debug.Log("On Store Items Loaded!!!");
    }

    private void PopulateAllContainers()
    {
        PopulateContainer(StoreCategory.Coin, coinContainer);
        PopulateContainer(StoreCategory.Diamond, diamondContainer);
        PopulateContainer(StoreCategory.Silver, silverContainer);
    }

    private void PopulateContainer(StoreCategory category, Transform container)
    {
        if (container == null || storeItemPrefab == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        var items = StoreManager.Instance.GetStoreItemsByCategory(category);
        
        if (items.Count == 0)
        {
            Debug.Log($"No items found for category: {category}");
            return;
        }

        foreach (var item in items)
        {
            var itemGO = Instantiate(storeItemPrefab, container);
            var storeItemUI = itemGO.GetComponent<StoreItemUI>();
            storeItemUI.name = item.name;
            if (storeItemUI != null)
            {
                storeItemUI.Initialize(item, OnItemButtonClicked);
            }
        }
    }

    private void OnItemButtonClicked(StoreItem item)
    {
        StoreManager.Instance.PurchaseItem(item.id, "", (response) =>
        {
            if (response.success)
            {
                Debug.Log($"Purchased {item.name} successfully!");
            }
        });
    }

    private void OnItemPurchased(StoreItem item)
    {
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
        errorPanel.SetActive(true);
        errorText.text = $"Purchase Failed:\n{errorMessage}";
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
            StoreManager.Instance.OnStoreItemsLoaded -= OnStoreItemsLoaded;
            StoreManager.Instance.OnItemPurchased -= OnItemPurchased;
            StoreManager.Instance.OnPurchaseFailed -= OnPurchaseFailed;
        }
    }
}