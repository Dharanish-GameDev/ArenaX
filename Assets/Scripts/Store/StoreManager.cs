using System;
using System.Collections.Generic;
using UnityEngine;
using Arena.API.Models;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StoreManager : MonoBehaviour
{
    private static StoreManager instance;
    public static StoreManager Instance => instance;

    private List<StoreItem> coinItems = new List<StoreItem>();
    private List<StoreItem> diamondItems = new List<StoreItem>();
    private List<StoreItem> silverItems = new List<StoreItem>();
    
    private bool isInitialized = false;

    public event Action OnStoreItemsLoaded;
    public event Action<StoreItem> OnItemPurchased;
    public event Action<string> OnPurchaseFailed;

#if UNITY_EDITOR
    [Header("Editor Simulation")]
    [SerializeField] private bool useSimulatedData = false;
    [TextArea(3, 10)]
    [SerializeField] private string simulatedStoreItemsJson = @"[
        {
            ""productId"": ""coins_30m"",
            ""coins"": 30000000,
            ""price"": 99
        },
        {
            ""productId"": ""coins_10m"",
            ""coins"": 10000000,
            ""price"": 49
        },
        {
            ""productId"": ""diamonds_100"",
            ""diamonds"": 100,
            ""price"": 9.99
        },
        {
            ""productId"": ""diamonds_500"",
            ""diamonds"": 500,
            ""price"": 39.99
        },
        {
            ""productId"": ""silver_1000"",
            ""silver"": 1000,
            ""price"": 4.99
        }
    ]";

    [SerializeField] private bool simulatePurchaseSuccess = true;
    [SerializeField] private float simulatedPurchaseDelay = 0.5f;
#endif

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

    /// <summary>
    /// Initialize the store by loading available items
    /// </summary>
    public void InitializeStore(Action onComplete = null)
    {
        if (isInitialized)
        {
            onComplete?.Invoke();
            OnStoreItemsLoaded?.Invoke();
            return;
        }

        LoadStoreItems(onComplete);
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
        var item = coinItems.Find(i => i.id == itemId);
        if (item != null) return item;
        
        item = diamondItems.Find(i => i.id == itemId);
        if (item != null) return item;
        
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
        
        return StoreCategory.Coin;
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
                success = false
            });
            return;
        }

#if UNITY_EDITOR
        if (useSimulatedData)
        {
            // Simulate purchase in editor
            StartCoroutine(SimulatePurchase(item, onComplete));
            return;
        }
#endif

        var purchaseRequest = new PurchaseRequest
        {
            productId = itemId,
            receipt = receipt,
            platform = "android"
        };
        
        string json = JsonConvert.SerializeObject(purchaseRequest);

        ApiManager.Instance.SendRequest<PurchaseResponse>(
            ApiEndPoints.Store.Purchase,
            RequestMethod.POST,
            (response) =>
            {
                if (response != null)
                {
                    if (response.success)
                    {
                        OnItemPurchased?.Invoke(item);
                    }
                    else
                    {
                        OnPurchaseFailed?.Invoke("Failed to Complete Purchase");
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
                };
                OnPurchaseFailed?.Invoke(error);
                onComplete?.Invoke(errorResponse);
            },
            json);
    }

#if UNITY_EDITOR
    private System.Collections.IEnumerator SimulatePurchase(StoreItem item, Action<PurchaseResponse> onComplete)
    {
        Debug.Log($"<color=yellow>[SIMULATED]</color> Processing purchase for: {item.name}");
        
        // Simulate network delay
        yield return new WaitForSeconds(simulatedPurchaseDelay);

        if (simulatePurchaseSuccess)
        {
            var response = new PurchaseResponse
            {
                success = true,
            };
            
            Debug.Log($"<color=green>[SIMULATED]</color> Purchase successful: {item.name}");
            OnItemPurchased?.Invoke(item);
            onComplete?.Invoke(response);
        }
        else
        {
            var response = new PurchaseResponse
            {
                success = false,
            };
            
            Debug.Log($"<color=red>[SIMULATED]</color> Purchase failed: {item.name}");
            OnPurchaseFailed?.Invoke("Purchase failed");
            onComplete?.Invoke(response);
        }
    }
#endif

    /// <summary>
    /// Refresh all store items from server
    /// </summary>
    public void RefreshStoreItems(Action onComplete = null)
    {
        LoadStoreItems(onComplete);
    }

    /// <summary>
    /// Check if store has been initialized
    /// </summary>
    public bool IsStoreInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// Format price for display with category-specific formatting
    /// </summary>
    public string FormatPrice(StoreItem item)
    {
        return $"${item.price:F2}";
    }

    /// <summary>
    /// Format quantity for display
    /// </summary>
    public string FormatQuantity(StoreItem item)
    {
        if (item.type == "coin")
            return $"{item.quantity:N0} Coins";
        else if (item.type == "diamond")
            return $"{item.quantity:N0} Diamonds";
        else if (item.type == "silver")
            return $"{item.quantity:N0} Silver";
        else
            return $"{item.quantity:N0}";
    }

    /// <summary>
    /// Load store items from server
    /// </summary>
    private void LoadStoreItems(Action onComplete = null)
    {
#if UNITY_EDITOR
        if (useSimulatedData)
        {
            // Use simulated data in editor
            LoadSimulatedStoreItems(onComplete);
            return;
        }
#endif

        // Use real API
        TryLoadStoreItems(onComplete);
    }

    /// <summary>
    /// Try loading store items with better error handling
    /// </summary>
    private void TryLoadStoreItems(Action onComplete)
    {
        ApiManager.Instance.SendRequest(
            ApiEndPoints.Store.Items,
            RequestMethod.GET,
            (string rawResponse) =>
            {
                Debug.Log($"Raw JSON Response: {rawResponse}");
                
                try
                {
                    // Parse the paginated API response
                    var paginatedResponse = JsonConvert.DeserializeObject<StorePaginatedResponse>(rawResponse);
                    
                    if (paginatedResponse != null && paginatedResponse.items != null && paginatedResponse.items.Count > 0)
                    {
                        // Convert API response items to StoreItem objects
                        var storeItems = ConvertApiResponseToStoreItems(paginatedResponse.items);
                        
                        CategorizeItems(storeItems);
                        isInitialized = true;
                        Debug.Log($"Successfully loaded {storeItems.Count} store items (page {paginatedResponse.page} of {paginatedResponse.total / paginatedResponse.limit + 1})");
                        OnStoreItemsLoaded?.Invoke();
                    }
                    else if (paginatedResponse != null && paginatedResponse.items != null && paginatedResponse.items.Count == 0)
                    {
                        Debug.LogWarning("Store items list is empty (0 items)");
                        OnStoreItemsLoaded?.Invoke();
                    }
                    else
                    {
                        Debug.LogError("Failed to deserialize store items: null response");
                    }
                }
                catch (JsonException je)
                {
                    Debug.LogError($"JSON Parse Error: {je.Message}");
                    Debug.LogError($"Raw response was: {rawResponse}");
                    
                    // Try alternate parsing approach - maybe it's a direct array
                    try
                    {
                        Debug.Log("Attempting to parse as direct array...");
                        var directItems = JsonConvert.DeserializeObject<List<StoreApiItem>>(rawResponse);
                        if (directItems != null)
                        {
                            var storeItems = ConvertApiResponseToStoreItems(directItems);
                            CategorizeItems(storeItems);
                            isInitialized = true;
                            Debug.Log($"Successfully loaded {storeItems.Count} store items (direct array format)");
                            OnStoreItemsLoaded?.Invoke();
                        }
                    }
                    catch (Exception e2)
                    {
                        Debug.LogError($"Alternate parsing also failed: {e2.Message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading store items: {e.Message}");
                }
                
                onComplete?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"Network error loading store items: {error}");
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Convert API response format to StoreItem format
    /// </summary>
    private List<StoreItem> ConvertApiResponseToStoreItems(List<StoreApiItem> apiItems)
    {
        var storeItems = new List<StoreItem>();
        
        foreach (var apiItem in apiItems)
        {
            var storeItem = new StoreItem();
            storeItem.id = apiItem.productId; // Using productId as the item ID
            
            // Parse price from string to float
            if (float.TryParse(apiItem.price, out float price))
            {
                storeItem.price = price;
            }
            else
            {
                storeItem.price = 0f;
                Debug.LogWarning($"Could not parse price for item {apiItem.productId}: {apiItem.price}");
            }
            
            // Parse quantity from string to float
            if (float.TryParse(apiItem.value, out float quantity))
            {
                storeItem.quantity = quantity;
            }
            else
            {
                storeItem.quantity = 0f;
                Debug.LogWarning($"Could not parse value for item {apiItem.productId}: {apiItem.value}");
            }
            
            // Set type based on the type field
            string itemType = apiItem.type.ToLower();
            storeItem.type = itemType;
            
            // Create display name
            if (itemType == "coins" || itemType == "coin")
            {
                storeItem.name = $"{FormatNumber(quantity)} Coins";
                storeItem.description = $"Purchase {FormatNumber(quantity)} coins";
            }
            else if (itemType == "diamonds" || itemType == "diamond")
            {
                storeItem.name = $"{FormatNumber(quantity)} Diamonds";
                storeItem.description = $"Purchase {FormatNumber(quantity)} diamonds";
            }
            else if (itemType == "silver")
            {
                storeItem.name = $"{FormatNumber(quantity)} Silver";
                storeItem.description = $"Purchase {FormatNumber(quantity)} silver";
            }
            else
            {
                // Default fallback
                storeItem.name = $"{FormatNumber(quantity)} {apiItem.type}";
                storeItem.description = $"Purchase {FormatNumber(quantity)} {apiItem.type}";
            }
            
            // // Add additional info
            // storeItem.dailyLimit = apiItem.dailyLimit;
            
            storeItems.Add(storeItem);
        }
        
        return storeItems;
    }

    /// <summary>
    /// Format large numbers with commas
    /// </summary>
    private string FormatNumber(float number)
    {
        if (number >= 1000000)
            return $"{(number / 1000000):0.#}M";
        else if (number >= 1000)
            return $"{(number / 1000):0.#}K";
        else
            return number.ToString("N0");
    }

    /// <summary>
    /// Paginated response model matching the actual API
    /// </summary>
    [Serializable]
    private class StorePaginatedResponse
    {
        public int page;
        public int limit;
        public int total;
        public List<StoreApiItem> items;
    }

    /// <summary>
    /// API Item model matching the actual API structure
    /// </summary>
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

#if UNITY_EDITOR
    /// <summary>
    /// Load simulated store items for editor testing
    /// </summary>
    private void LoadSimulatedStoreItems(Action onComplete = null)
    {
        try
        {
            Debug.Log("<color=yellow>[SIMULATED]</color> Loading simulated store items...");
            
            // Parse the simulated JSON using the API response model
            var apiItems = JsonConvert.DeserializeObject<List<StoreApiItem>>(simulatedStoreItemsJson);
            
            if (apiItems != null)
            {
                // Convert to StoreItem objects
                var storeItems = ConvertApiResponseToStoreItems(apiItems);
                CategorizeItems(storeItems);
                isInitialized = true;
                Debug.Log($"<color=green>[SIMULATED]</color> Loaded {storeItems.Count} store items");
                OnStoreItemsLoaded?.Invoke();
            }
            else
            {
                Debug.LogError("<color=red>[SIMULATED]</color> Failed to parse simulated store items");
            }
        }
        catch (JsonException je)
        {
            Debug.LogError($"<color=red>[SIMULATED]</color> JSON Error: {je.Message}");
            Debug.LogError($"<color=red>[SIMULATED]</color> Problem JSON: {simulatedStoreItemsJson}");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>[SIMULATED]</color> Error loading simulated store items: {e.Message}");
        }
        
        onComplete?.Invoke();
    }
#endif

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
    }

    /// <summary>
    /// Debug method to test JSON parsing
    /// </summary>
    public void TestJsonParsing(string testJson)
    {
        try
        {
            Debug.Log("Testing JSON parsing...");
            Debug.Log($"Test JSON: {testJson}");
            
            // Try parsing as paginated response first
            var paginatedResponse = JsonConvert.DeserializeObject<StorePaginatedResponse>(testJson);
            if (paginatedResponse != null && paginatedResponse.items != null)
            {
                Debug.Log($"Test successful! Parsed paginated response with {paginatedResponse.items.Count} items");
                foreach (var apiItem in paginatedResponse.items)
                {
                    Debug.Log($"Product: {apiItem.productId}, Type: {apiItem.type}, Value: {apiItem.value}, Price: ${apiItem.price}");
                }
                return;
            }
            
            // Try parsing as array
            var apiItems = JsonConvert.DeserializeObject<List<StoreApiItem>>(testJson);
            if (apiItems != null)
            {
                Debug.Log($"Test successful! Parsed {apiItems.Count} API items directly");
                foreach (var apiItem in apiItems)
                {
                    Debug.Log($"Product: {apiItem.productId}, Type: {apiItem.type}, Value: {apiItem.value}, Price: ${apiItem.price}");
                }
            }
            else
            {
                Debug.LogError("Test failed: could not parse JSON");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
        }
    }
}

public enum StoreCategory
{
    Coin,
    Diamond,
    Silver
}

#if UNITY_EDITOR
[CustomEditor(typeof(StoreManager))]
public class StoreManagerEditor : Editor
{
    private string testJson = @"{
  ""page"": 1,
  ""limit"": 20,
  ""total"": 4,
  ""items"": [
    {
      ""id"": ""2eb2a1aa-0fde-11f1-be50-767828ab6c2f"",
      ""productId"": ""diamond_500"",
      ""type"": ""DIAMOND"",
      ""value"": ""500"",
      ""price"": ""14.99"",
      ""dailyLimit"": 2,
      ""status"": true
    },
    {
      ""id"": ""2eb348d2-0fde-11f1-be50-767828ab6c2f"",
      ""productId"": ""silver_10k"",
      ""type"": ""SILVER"",
      ""value"": ""10000"",
      ""price"": ""4.99"",
      ""dailyLimit"": 2,
      ""status"": true
    }
  ]
}";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        StoreManager storeManager = (StoreManager)target;
        
        GUILayout.Space(10);
        GUILayout.Label("Editor Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Load Simulated Data"))
        {
            storeManager.RefreshStoreItems(() => {
                Debug.Log("Simulated data loaded!");
            });
        }
        
        if (GUILayout.Button("Test JSON Parsing"))
        {
            storeManager.TestJsonParsing(testJson);
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Test JSON Input", EditorStyles.boldLabel);
        testJson = EditorGUILayout.TextArea(testJson, GUILayout.Height(150));
        
        if (GUILayout.Button("Test Custom JSON"))
        {
            storeManager.TestJsonParsing(testJson);
        }
        
        if (GUILayout.Button("Print Current Items"))
        {
            var items = storeManager.GetAllStoreItems();
            Debug.Log($"Current items count: {items.Count}");
            foreach (var item in items)
            {
                Debug.Log($"- {item.name} ({item.id}): ${item.price} - {item.type} (Qty: {item.quantity})");
            }
        }
    }
}
#endif