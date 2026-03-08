using System;
using System.Collections.Generic;
using UnityEngine;
using Arena.API.Models;
using Newtonsoft.Json;
using UnityEngine.Purchasing;

public class StoreManager : MonoBehaviour
{
    private static StoreManager instance;
    public static StoreManager Instance => instance;

    private List<StoreItem> coinItems = new List<StoreItem>();
    private List<StoreItem> diamondItems = new List<StoreItem>();
    private List<StoreItem> silverItems = new List<StoreItem>();

    private bool isInitialized = false;
    private bool isIAPInitialized = false;

    public event Action OnStoreItemsLoaded;
    public event Action<StoreItem> OnItemPurchased;
    public event Action<string> OnPurchaseFailed;
    public event Action OnIAPInitialized;

    [Header("Debug")]
    [SerializeField] private bool logRawResponse = true;

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

    private void Start()
    {
// In builds, check if IAP is initialized
#if !UNITY_EDITOR
        try
        {
            if (CodelessIAPStoreListener.Instance != null)
            {
                isIAPInitialized = true;
                OnIAPInitialized?.Invoke();
                Debug.Log("IAP initialized in build");
            }
        }
        catch (Exception e)
        {
            Debug.Log($"IAP not available: {e.Message}");
        }
#endif
    }

    /// Initialize store
    public void InitializeStore(Action onComplete = null)
    {
        LoadStoreItems(onComplete);
    }

    public void RefreshStoreItems(Action onComplete = null)
    {
        Debug.Log("Refreshing store items...");
        LoadStoreItems(onComplete);
    }

    /// <summary>
    /// Called when IAP is initialized (for builds)
    /// </summary>
    public void OnIAPManagerInitialized()
    {
#if !UNITY_EDITOR
        isIAPInitialized = true;
        OnIAPInitialized?.Invoke();
        
        // Refresh displayed prices if store is already loaded
        if (isInitialized)
        {
            OnStoreItemsLoaded?.Invoke();
        }
#endif
    }

    /// Get items by category
    public List<StoreItem> GetStoreItemsByCategory(StoreCategory category)
    {
        var items = category switch
        {
            StoreCategory.Coin => new List<StoreItem>(coinItems),
            StoreCategory.Diamond => new List<StoreItem>(diamondItems),
            StoreCategory.Silver => new List<StoreItem>(silverItems),
            _ => new List<StoreItem>()
        };

        // In builds, update with IAP prices
#if !UNITY_EDITOR
        if (isIAPInitialized)
        {
            foreach (var item in items)
            {
                UpdateLocalizedPrice(item);
            }
        }
#endif

        return items;
    }

    /// Get all items
    public List<StoreItem> GetAllStoreItems()
    {
        var all = new List<StoreItem>();
        all.AddRange(coinItems);
        all.AddRange(diamondItems);
        all.AddRange(silverItems);

        // In builds, update with IAP prices
#if !UNITY_EDITOR
        if (isIAPInitialized)
        {
            foreach (var item in all)
            {
                UpdateLocalizedPrice(item);
            }
        }
#endif

        return all;
    }

    /// Find item by productId (IAP ID)
    public StoreItem GetItemByProductId(string productId)
    {
        var item = coinItems.Find(i => i.productId == productId);
        if (item != null) 
        {
#if !UNITY_EDITOR
            UpdateLocalizedPrice(item);
#endif
            return item;
        }

        item = diamondItems.Find(i => i.productId == productId);
        if (item != null) 
        {
#if !UNITY_EDITOR
            UpdateLocalizedPrice(item);
#endif
            return item;
        }

        item = silverItems.Find(i => i.productId == productId);
        if (item != null) 
        {
#if !UNITY_EDITOR
            UpdateLocalizedPrice(item);
#endif
            return item;
        }

        return null;
    }

    /// Find item by ID (searches by both id and productId)
    public StoreItem GetItemById(string id)
    {
        // First try to find by productId (IAP ID)
        var item = GetItemByProductId(id);
        if (item != null) return item;

        // Fallback to searching by internal id if needed
        item = coinItems.Find(i => i.id == id);
        if (item != null) 
        {
#if !UNITY_EDITOR
            UpdateLocalizedPrice(item);
#endif
            return item;
        }

        item = diamondItems.Find(i => i.id == id);
        if (item != null) 
        {
#if !UNITY_EDITOR
            UpdateLocalizedPrice(item);
#endif
            return item;
        }

        item = silverItems.Find(i => i.id == id);
        if (item != null) 
        {
#if !UNITY_EDITOR
            UpdateLocalizedPrice(item);
#endif
            return item;
        }

        return null;
    }

    /// Update item with localized price from IAP (Builds only)
    private void UpdateLocalizedPrice(StoreItem item)
    {
#if !UNITY_EDITOR
        try
        {
            if (CodelessIAPStoreListener.Instance == null) return;

            var product = CodelessIAPStoreListener.Instance.GetProduct(item.productId);
            
            if (product != null && product.metadata != null)
            {
                item.localizedPriceString = product.metadata.localizedPriceString;
                item.currencyCode = product.metadata.isoCurrencyCode;
                item.currencySymbol = GetCurrencySymbol(product.metadata.isoCurrencyCode);
                
                Debug.Log($"IAP price for {item.productId}: {item.localizedPriceString}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to update localized price for {item.productId}: {e.Message}");
        }
#endif
    }

    /// Get currency symbol from currency code
    private string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            "CNY" => "¥",
            "INR" => "₹",
            "CAD" => "C$",
            "AUD" => "A$",
            _ => "$"
        };
    }

    /// Determine category
    public StoreCategory GetCategoryFromType(string type)
    {
        if (type.Contains("coin", StringComparison.OrdinalIgnoreCase))
            return StoreCategory.Coin;

        if (type.Contains("diamond", StringComparison.OrdinalIgnoreCase))
            return StoreCategory.Diamond;

        if (type.Contains("silver", StringComparison.OrdinalIgnoreCase))
            return StoreCategory.Silver;

        return StoreCategory.Coin;
    }

    /// PURCHASE ITEM
    public void PurchaseItem(string productId, string receipt = "", Action<PurchaseResponse> onComplete = null)
    {
        var item = GetItemByProductId(productId);

        if (item == null)
        {
            Debug.LogError($"Item with productId {productId} not found in store");
            onComplete?.Invoke(new PurchaseResponse { success = false });
            OnPurchaseFailed?.Invoke("Item not found");
            return;
        }

        var request = new PurchaseRequest
        {
            productId = item.productId,
            receipt = receipt,
            platform = Application.platform == RuntimePlatform.Android ? "android" : "ios"
        };

        #if UNITY_EDITOR
        request.platform = "android";
        #endif

        string json = JsonConvert.SerializeObject(request);

        Debug.Log("Purchase Request : " + json);

        ApiManager.Instance.SendRequest<PurchaseResponse>(
            ApiEndPoints.Store.Purchase,
            RequestMethod.POST,
            (response) =>
            {
                if (response != null && response.success)
                {
                    Debug.Log($"Purchase successful: {item.name}");
                    OnItemPurchased?.Invoke(item);
                }
                else
                {
                    Debug.LogError("Purchase failed: Server returned error");
                    OnPurchaseFailed?.Invoke("Purchase failed");
                }

                onComplete?.Invoke(response);
            },
            (error) =>
            {
                Debug.LogError($"Purchase error: {error}");
                OnPurchaseFailed?.Invoke(error);
                onComplete?.Invoke(new PurchaseResponse { success = false });
            },
            json);
    }

    /// PRICE DISPLAY - Uses backend in Editor, IAP in builds
    public string FormatPrice(StoreItem item)
    {
        if (item == null)
        {
            Debug.LogError("StoreItem is null in FormatPrice");
            return "$0";
        }

// In builds, use IAP price if available
#if !UNITY_EDITOR
        if (!string.IsNullOrEmpty(item.localizedPriceString))
        {
            return item.localizedPriceString;
        }
#endif

        // In Editor (or fallback), use backend price
        return FormatPriceValue(item.price, "$");
    }

    /// Format price with currency code
    public string FormatPriceWithCurrency(StoreItem item)
    {
#if !UNITY_EDITOR
        if (!string.IsNullOrEmpty(item.localizedPriceString))
        {
            return item.localizedPriceString;
        }
#endif

        return FormatPriceValue(item.price, "$") + " USD";
    }

    /// Helper method to format price values (removes .00 for whole numbers)
    private string FormatPriceValue(float price, string currencySymbol)
    {
        // Check if it's a whole number (no decimal or .00)
        if (Math.Abs(price - Math.Round(price)) < 0.001f)
        {
            // It's a whole number, format without decimal places
            return $"{currencySymbol}{price:F0}";
        }
        else
        {
            // It has decimals, show 2 decimal places
            return $"{currencySymbol}{price:F2}";
        }
    }

    /// QUANTITY DISPLAY FOR ITEM NAME (with abbreviations)
    public string FormatItemName(StoreItem item)
    {
        string typeDisplay = GetTypeDisplayName(item.type);
        string formattedQuantity = FormatQuantityAbbreviated(item.quantity);
        
        return $"{formattedQuantity} {typeDisplay}";
    }

    /// QUANTITY DISPLAY (for description or other UI)
    public string FormatQuantity(StoreItem item)
    {
        string type = item.type.ToLower();
        string formattedQuantity = FormatQuantityAbbreviated(item.quantity);
        
        if (type.Contains("coin"))
            return $"{formattedQuantity} Coins";

        if (type.Contains("diamond"))
            return $"{formattedQuantity} Diamonds";

        if (type.Contains("silver"))
            return $"{formattedQuantity} Silver";

        return $"{formattedQuantity}";
    }

    /// Format quantity with abbreviations (K, M, B, T)
    public string FormatQuantityAbbreviated(float quantity)
    {
        if (quantity >= 1000000000000) // Trillion
        {
            float value = quantity / 1000000000000.0f;
            return (Math.Abs(value - Math.Round(value)) < 0.01f) ? $"{value:F0}T" : $"{value:F1}T";
        }
        else if (quantity >= 1000000000) // Billion
        {
            float value = quantity / 1000000000.0f;
            return (Math.Abs(value - Math.Round(value)) < 0.01f) ? $"{value:F0}B" : $"{value:F1}B";
        }
        else if (quantity >= 1000000) // Million
        {
            float value = quantity / 1000000.0f;
            return (Math.Abs(value - Math.Round(value)) < 0.01f) ? $"{value:F0}M" : $"{value:F1}M";
        }
        else if (quantity >= 1000) // Thousand
        {
            float value = quantity / 1000.0f;
            return (Math.Abs(value - Math.Round(value)) < 0.01f) ? $"{value:F0}K" : $"{value:F1}K";
        }
        else
        {
            return quantity.ToString("N0"); // Format with commas for numbers under 1000
        }
    }

    /// Format quantity with commas (no abbreviations)
    public string FormatQuantityWithCommas(float quantity)
    {
        return quantity.ToString("N0");
    }

    /// Get display name for type
    private string GetTypeDisplayName(string type)
    {
        string typeLower = type.ToLower();
        
        if (typeLower.Contains("coin"))
            return "Coins";
        else if (typeLower.Contains("diamond"))
            return "Diamonds";
        else if (typeLower.Contains("silver"))
            return "Silver";
        else
        {
            // Remove trailing 's' if present and capitalize
            string display = typeLower;
            if (display.EndsWith("s"))
                display = display.Substring(0, display.Length - 1);
            
            return char.ToUpper(display[0]) + display.Substring(1) + "s";
        }
    }

    /// LOAD ITEMS - Always from backend (both Editor and Builds)
    private void LoadStoreItems(Action onComplete)
    {
        ApiManager.Instance.SendRequest(
            ApiEndPoints.Store.Items,
            RequestMethod.GET,
            (string raw) =>
            {
                if (logRawResponse)
                {
                    Debug.Log("Store Response: " + raw);
                }
                
                try
                {
                    var response = JsonConvert.DeserializeObject<StorePaginatedResponse>(raw);

                    if (response != null && response.items != null)
                    {
                        var items = ConvertApiItems(response.items);
                        CategorizeItems(items);
                        isInitialized = true;

                        OnStoreItemsLoaded?.Invoke();
                    }
                    else
                    {
                        Debug.LogError("Failed to parse store items: null response");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing store items: {e.Message}");
                }

                onComplete?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"Network error: {error}");
                onComplete?.Invoke();
            });
    }

    /// Convert API format
    private List<StoreItem> ConvertApiItems(List<StoreApiItem> apiItems)
    {
        var list = new List<StoreItem>();

        foreach (var api in apiItems)
        {
            var item = new StoreItem();

            item.productId = api.productId;
            item.id = api.id;
            item.status = api.status;
            item.dailyLimit = api.dailyLimit;

            // Parse price with culture-invariant parsing
            if (!string.IsNullOrEmpty(api.price))
            {
                if (float.TryParse(api.price, 
                    System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    out float price))
                {
                    item.price = price;
                    Debug.Log($"Parsed price for {api.productId}: {price}");
                }
                else
                {
                    Debug.LogError($"Failed to parse price: '{api.price}' for {api.productId}");
                    item.price = 0f;
                }
            }
            else
            {
                Debug.LogError($"Price is null or empty for {api.productId}");
                item.price = 0f;
            }

            // Parse quantity
            if (!string.IsNullOrEmpty(api.value))
            {
                float.TryParse(api.value, 
                    System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    out float quantity);
                item.quantity = quantity;
            }

            item.type = api.type.ToLower();

            string formattedQuantity = FormatQuantityAbbreviated(item.quantity);
            string typeDisplay = GetTypeDisplayName(item.type);
            
            item.name = $"{formattedQuantity} {typeDisplay}";
            item.description = $"Purchase {formattedQuantity} {item.type}";

            list.Add(item);
        }

        return list;
    }

    /// Categorize items
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

        Debug.Log($"Categorized: Coins:{coinItems.Count} Diamonds:{diamondItems.Count} Silver:{silverItems.Count}");
    }

    /// Check if store is ready
    public bool IsStoreReady()
    {
        return isInitialized;
    }

    /// Check if IAP is ready (builds only)
    public bool IsIAPReady()
    {
#if !UNITY_EDITOR
        return isIAPInitialized;
#else
        return false;
#endif
    }

    /// Debug method to check store items
    public void DebugPrintStoreItems()
    {
        Debug.Log("=== STORE ITEMS DEBUG ===");
        
        var allItems = GetAllStoreItems();
        foreach (var item in allItems)
        {
            Debug.Log($"Product: {item.productId}");
            Debug.Log($"  - Internal ID: {item.id}");
            Debug.Log($"  - Name: {item.name}");
            Debug.Log($"  - Type: {item.type}");
            Debug.Log($"  - Raw Price: {item.price}");
            Debug.Log($"  - Localized String: {item.localizedPriceString ?? "null"}");
            Debug.Log($"  - Formatted Price: {FormatPrice(item)}");
            Debug.Log($"  - Quantity: {item.quantity}");
            Debug.Log($"  - Formatted Qty: {FormatQuantity(item)}");
        }
        Debug.Log("=========================");
    }

    /// API response
    [Serializable]
    private class StorePaginatedResponse
    {
        public int page;
        public int limit;
        public int total;
        public List<StoreApiItem> items;
    }

    /// API item
    [Serializable]
    private class StoreApiItem
    {
        public string id;
        public string productId;
        public string type;
        public string value;
        public string price;
        public int? dailyLimit;
        public bool status;
    }
}

public enum StoreCategory
{
    Coin,
    Diamond,
    Silver
}