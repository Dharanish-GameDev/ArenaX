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
            ""id"": ""coin_1"",
            ""name"": ""100 Coins"",
            ""description"": ""Small coin pack"",
            ""price"": 0.99,
            ""type"": ""coin""
        },
        {
            ""id"": ""coin_2"",
            ""name"": ""500 Coins"",
            ""description"": ""Medium coin pack"",
            ""price"": 4.99,
            ""type"": ""coin""
        },
        {
            ""id"": ""coin_3"",
            ""name"": ""1000 Coins"",
            ""description"": ""Large coin pack"",
            ""price"": 9.99,
            ""type"": ""coin""
        },
        {
            ""id"": ""diamond_1"",
            ""name"": ""50 Diamonds"",
            ""description"": ""Small diamond pack"",
            ""price"": 1.99,
            ""type"": ""diamond""
        },
        {
            ""id"": ""diamond_2"",
            ""name"": ""150 Diamonds"",
            ""description"": ""Medium diamond pack"",
            ""price"": 4.99,
            ""type"": ""diamond""
        },
        {
            ""id"": ""diamond_3"",
            ""name"": ""500 Diamonds"",
            ""description"": ""Large diamond pack"",
            ""price"": 14.99,
            ""type"": ""diamond""
        },
        {
            ""id"": ""silver_1"",
            ""name"": ""250 Silver"",
            ""description"": ""Small silver pack"",
            ""price"": 1.49,
            ""type"": ""silver""
        },
        {
            ""id"": ""silver_2"",
            ""name"": ""1000 Silver"",
            ""description"": ""Medium silver pack"",
            ""price"": 4.49,
            ""type"": ""silver""
        },
        {
            ""id"": ""silver_3"",
            ""name"": ""2500 Silver"",
            ""description"": ""Large silver pack"",
            ""price"": 9.99,
            ""type"": ""silver""
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
                success = false, 
                message = "Item not found" 
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
            itemId = itemId,
            receipt = receipt
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
                        Debug.Log($"Purchase successful: {response.message}");
                        OnItemPurchased?.Invoke(item);
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
                message = $"Successfully purchased {item.name}!"
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
                message = "Simulated purchase failure"
            };
            
            Debug.Log($"<color=red>[SIMULATED]</color> Purchase failed: {item.name}");
            OnPurchaseFailed?.Invoke(response.message);
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
        var category = GetCategoryFromType(item.type);
        
        return category switch
        {
            StoreCategory.Coin => $"${item.price:F2}",
            StoreCategory.Diamond => $"${item.price:F2}",
            StoreCategory.Silver => $"${item.price:F2}",
            _ => $"${item.price:F2}"
        };
    }

    /// <summary>
    /// Load store items from server - FIXED VERSION
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

        // Use real API - TRY BOTH METHODS
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
                    // Try to parse the response
                    var items = JsonConvert.DeserializeObject<List<StoreItem>>(rawResponse);
                    
                    if (items != null && items.Count > 0)
                    {
                        CategorizeItems(items);
                        isInitialized = true;
                        Debug.Log($"Successfully loaded {items.Count} store items");
                        OnStoreItemsLoaded?.Invoke();
                    }
                    else if (items != null && items.Count == 0)
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
                    
                    // Try alternative parsing
                    TryAlternativeParsing(rawResponse);
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
    /// Try alternative parsing methods
    /// </summary>
    private void TryAlternativeParsing(string rawResponse)
    {
        try
        {
            // Sometimes the response might be wrapped differently
            // Try to see if it's a different format
            Debug.Log("Trying alternative parsing...");
            
            // Remove whitespace and check first character
            string trimmed = rawResponse.Trim();
            
            if (trimmed.StartsWith("["))
            {
                Debug.Log("Response is a JSON array, trying direct deserialization...");
                
                // Already tried this, maybe the StoreItem class doesn't match?
                var items = JsonConvert.DeserializeObject<List<StoreItem>>(rawResponse);
                if (items != null)
                {
                    Debug.Log($"Alternative parsing successful: {items.Count} items");
                    CategorizeItems(items);
                    OnStoreItemsLoaded?.Invoke();
                    return;
                }
            }
            else if (trimmed.StartsWith("{"))
            {
                Debug.Log("Response is a JSON object, trying wrapper...");
                
                // Maybe it's wrapped in an object?
                var wrapper = JsonConvert.DeserializeObject<StoreItemsResponseWrapper>(rawResponse);
                if (wrapper != null && wrapper.items != null)
                {
                    Debug.Log($"Wrapper parsing successful: {wrapper.items.Count} items");
                    CategorizeItems(wrapper.items);
                    OnStoreItemsLoaded?.Invoke();
                    return;
                }
            }
            
            Debug.LogError("All parsing attempts failed");
        }
        catch (Exception e)
        {
            Debug.LogError($"Alternative parsing failed: {e.Message}");
        }
    }

    /// <summary>
    /// Wrapper class for alternative response format
    /// </summary>
    [Serializable]
    private class StoreItemsResponseWrapper
    {
        public List<StoreItem> items;
        public string status;
        public string message;
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
            
            // Parse the simulated JSON
            var items = JsonConvert.DeserializeObject<List<StoreItem>>(simulatedStoreItemsJson);
            
            if (items != null)
            {
                CategorizeItems(items);
                isInitialized = true;
                Debug.Log($"<color=green>[SIMULATED]</color> Loaded {items.Count} store items");
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
            
            var items = JsonConvert.DeserializeObject<List<StoreItem>>(testJson);
            if (items != null)
            {
                Debug.Log($"Test successful! Parsed {items.Count} items");
                foreach (var item in items)
                {
                    Debug.Log($"Item: {item.id}, Name: {item.name}, Price: {item.price}, Type: {item.type}");
                }
            }
            else
            {
                Debug.LogError("Test failed: items is null");
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

