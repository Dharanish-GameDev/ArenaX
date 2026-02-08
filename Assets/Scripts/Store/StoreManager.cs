using System;
using System.Collections.Generic;
using UnityEngine;
using Arena.API.Models;
using Newtonsoft.Json;

public class StoreManager : MonoBehaviour
{
    private static StoreManager instance;
    public static StoreManager Instance => instance;

    // Separate lists for different item types
    private List<StoreItem> coinItems = new List<StoreItem>();
    private List<StoreItem> diamondItems = new List<StoreItem>();
    private List<StoreItem> silverItems = new List<StoreItem>();
    
    private bool isInitialized = false;

    // Separate events for each category
    public event Action OnCoinItemsLoaded;
    public event Action OnDiamondItemsLoaded;
    public event Action OnSilverItemsLoaded;
    public event Action OnAllItemsLoaded;
    
    public event Action<StoreItem> OnItemPurchased;
    public event Action<string> OnPurchaseFailed;

    // Property to track loading status
    private int loadedCategories = 0;
    private const int TOTAL_CATEGORIES = 3;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Public Methods

    /// <summary>
    /// Initialize the store by loading available items
    /// </summary>
    public void InitializeStore(Action onComplete = null)
    {
        if (isInitialized)
        {
            onComplete?.Invoke();
            return;
        }

        loadedCategories = 0;
        LoadAllStoreItems(onComplete);
    }

    /// <summary>
    /// Get all store items by category
    /// </summary>
    public List<StoreItem> GetStoreItemsByCategory(StoreCategory category)
    {
        return category switch
        {
            StoreCategory.Coin => new List<StoreItem>(coinItems),
            StoreCategory.Diamond => new List<StoreItem>(diamondItems),
            StoreCategory.Silver => new List<StoreItem>(silverItems),
            _ => new List<StoreItem>()
        };
    }

    /// <summary>
    /// Get all items from all categories
    /// </summary>
    public List<StoreItem> GetAllStoreItems()
    {
        var allItems = new List<StoreItem>();
        allItems.AddRange(coinItems);
        allItems.AddRange(diamondItems);
        allItems.AddRange(silverItems);
        return allItems;
    }

    /// <summary>
    /// Get a specific store item by ID from any category
    /// </summary>
    public StoreItem GetItemById(string itemId)
    {
        // Check coin items
        var item = coinItems.Find(i => i.id == itemId);
        if (item != null) return item;
        
        // Check diamond items
        item = diamondItems.Find(i => i.id == itemId);
        if (item != null) return item;
        
        // Check silver items
        item = silverItems.Find(i => i.id == itemId);
        return item;
    }

    /// <summary>
    /// Get category of an item by its type
    /// </summary>
    public StoreCategory GetCategoryFromType(string type)
    {
        if (type.Contains("coin", StringComparison.OrdinalIgnoreCase)) 
            return StoreCategory.Coin;
        if (type.Contains("diamond", StringComparison.OrdinalIgnoreCase)) 
            return StoreCategory.Diamond;
        if (type.Contains("silver", StringComparison.OrdinalIgnoreCase)) 
            return StoreCategory.Silver;
        
        return StoreCategory.Coin; // Default
    }

    /// <summary>
    /// Purchase an item from the store
    /// </summary>
    public void PurchaseItem(string itemId, string receipt = "", Action<PurchaseResponse> onComplete = null)
    {
        var item = GetItemById(itemId);
        if (item == null)
        {
            Debug.LogError($"Item with ID {itemId} not found in store");
            onComplete?.Invoke(new PurchaseResponse 
            { 
                success = false, 
                message = "Item not found" 
            });
            return;
        }

        // Create purchase request
        var purchaseRequest = new PurchaseRequest
        {
            itemId = itemId,
            receipt = receipt
        };
        
        string json = JsonConvert.SerializeObject(purchaseRequest);

        // Send purchase request to server
        ApiManager.Instance.SendRequest<PurchaseResponse>(
            ApiEndPoints.Store.Purchase,
            RequestMethod.POST,
            (response) =>
            {
                if (response != null)
                {
                    if (response.success)
                    {
                        Debug.Log($"Purchase successful: {response.message}");
                        OnItemPurchased?.Invoke(item);
                        
                        // Refresh the specific category after purchase
                        RefreshCategory(GetCategoryFromType(item.type));
                    }
                    else
                    {
                        Debug.LogError($"Purchase failed: {response.message}");
                        OnPurchaseFailed?.Invoke(response.message);
                    }
                }
                onComplete?.Invoke(response);
            },
            (error) =>
            {
                Debug.LogError($"Failed to process purchase: {error}");
                var errorResponse = new PurchaseResponse
                {
                    success = false,
                    message = $"Network error: {error}"
                };
                OnPurchaseFailed?.Invoke(error);
                onComplete?.Invoke(errorResponse);
            },
            json);
    }

    /// <summary>
    /// Refresh all store items from server
    /// </summary>
    public void RefreshStoreItems(Action onComplete = null)
    {
        loadedCategories = 0;
        LoadAllStoreItems(onComplete);
    }

    /// <summary>
    /// Refresh specific category
    /// </summary>
    public void RefreshCategory(StoreCategory category, Action onComplete = null)
    {
        LoadCategoryItems(category, onComplete);
    }

    /// <summary>
    /// Check if store has been initialized
    /// </summary>
    public bool IsStoreInitialized()
    {
        return isInitialized;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Load all store items from server
    /// </summary>
    private void LoadAllStoreItems(Action onComplete = null)
    {
        // Load all items first, then categorize them
        ApiManager.Instance.SendRequest<StoreItemsResponse>(
            ApiEndPoints.Store.Items,
            RequestMethod.GET,
            (response) =>
            {
                if (response != null && response.items != null)
                {
                    CategorizeItems(response.items);
                    isInitialized = true;
                    Debug.Log($"Loaded {response.items.Count} store items");
                    OnAllItemsLoaded?.Invoke();
                }
                else
                {
                    Debug.LogError("Failed to load store items: Invalid response");
                }
                onComplete?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"Failed to load store items: {error}");
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Categorize items into Coin, Diamond, and Silver
    /// </summary>
    private void CategorizeItems(List<StoreItem> items)
    {
        coinItems.Clear();
        diamondItems.Clear();
        silverItems.Clear();

        foreach (var item in items)
        {
            var category = GetCategoryFromType(item.type);
            
            switch (category)
            {
                case StoreCategory.Coin:
                    coinItems.Add(item);
                    break;
                case StoreCategory.Diamond:
                    diamondItems.Add(item);
                    break;
                case StoreCategory.Silver:
                    silverItems.Add(item);
                    break;
            }
        }

        Debug.Log($"Categorized items: {coinItems.Count} coins, {diamondItems.Count} diamonds, {silverItems.Count} silver");
        
        // Trigger individual category events
        OnCoinItemsLoaded?.Invoke();
        OnDiamondItemsLoaded?.Invoke();
        OnSilverItemsLoaded?.Invoke();
    }

    /// <summary>
    /// Load specific category items
    /// </summary>
    private void LoadCategoryItems(StoreCategory category, Action onComplete = null)
    {
        // For now, reload all items and recategorize
        // In a more advanced system, you might have separate endpoints for each category
        RefreshStoreItems(onComplete);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Format price for display with category-specific formatting
    /// </summary>
    public string FormatPrice(StoreItem item)
    {
        var category = GetCategoryFromType(item.type);
        
        return category switch
        {
            StoreCategory.Coin => $"{item.price:F0} Coins",
            StoreCategory.Diamond => $"{item.price:F0} Diamonds",
            StoreCategory.Silver => $"{item.price:F2} Silver",
            _ => $"{item.price:F2}"
        };
    }

    /// <summary>
    /// Get category display name
    /// </summary>
    public string GetCategoryName(StoreCategory category)
    {
        return category.ToString();
    }

    #endregion
}

// Enum for store categories
public enum StoreCategory
{
    Coin,
    Diamond,
    Silver
}