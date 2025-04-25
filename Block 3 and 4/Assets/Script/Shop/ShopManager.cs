using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the in-game shop.  
/// Press B to open/close the shop, which pauses/resumes the game and toggles the cursor.  
/// Displays all items from the catalog, allows purchasing consumables (film/food) or equipment  
/// (registered into slot 3 for cycling).  
/// Automatically subscribes to and unsubscribes from the CurrencyManager's currency-changed event.
/// </summary>
public class ShopManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance for global access.
    /// </summary>
    public static ShopManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Root GameObject for the shop panel (should start inactive).")]
    public GameObject shopRoot;

    [Tooltip("Parent transform of the ScrollView content where item entries are instantiated.")]
    public Transform contentHolder;

    [Tooltip("Prefab containing the UI for one shop entry (e.g., button + text).")]
    public GameObject itemEntryPrefab;

    [Tooltip("Text field displaying the player's current quota (¡ï).")]
    public TMP_Text quotaText;

    [Header("Catalog Data")]
    [Tooltip("Array of shop items defined in the Inspector.")]
    public ShopItemData[] catalog;

    private void Awake()
    {
        // Initialize singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Build the UI entries for each catalog item
        BuildUI();

        // Update the quota display initially
        UpdateQuotaLabel();

        // Subscribe to currency changes to keep quota display in sync
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += UpdateQuotaLabel;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from currency change events to prevent memory leaks
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateQuotaLabel;
        }
    }

    private void Update()
    {
        // Toggle the shop panel when pressing B
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleShop();
        }
    }

    /// <summary>
    /// Instantiates one UI entry per catalog item under the contentHolder.
    /// Called once at startup.
    /// </summary>
    private void BuildUI()
    {
        foreach (var data in catalog)
        {
            GameObject entryGO = Instantiate(itemEntryPrefab, contentHolder);
            var entryUI = entryGO.GetComponent<ShopEntryUI>();
            entryUI.Init(data);
        }
    }

    /// <summary>
    /// Opens or closes the shop panel.
    /// When opened: pauses the game, unlocks cursor, and refreshes each entry's purchase state.
    /// When closed: resumes the game and locks/hides cursor.
    /// </summary>
    private void ToggleShop()
    {
        bool activating = !shopRoot.activeSelf;
        shopRoot.SetActive(activating);

        // Pause or resume game time
        Time.timeScale = activating ? 0f : 1f;

        // Toggle cursor lock and visibility
        Cursor.lockState = activating ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = activating;

        if (activating)
        {
            // Refresh all entries so purchase buttons reflect current currency and unlock status
            foreach (Transform child in contentHolder)
            {
                var entryUI = child.GetComponent<ShopEntryUI>();
                if (entryUI != null)
                {
                    entryUI.Refresh();
                }
            }
        }
    }

    /// <summary>
    /// Updates the quota (¡ï) text at the top of the shop UI.
    /// </summary>
    private void UpdateQuotaLabel()
    {
        if (quotaText != null && CurrencyManager.Instance != null)
        {
            quotaText.text = $"Quota¡ï: {CurrencyManager.Instance.QuotaStar}";
        }
    }

    /// <summary>
    /// Attempts to purchase the given ShopItemData.
    /// Called by ShopEntryUI when the player clicks Buy.
    /// Checks unlock requirements, spends currency, and grants the item.
    /// </summary>
    /// <param name="data">The shop item the player wants to buy.</param>
    public void TryBuy(ShopItemData data)
    {
        // Must have collected enough total stars to unlock the item
        if (CurrencyManager.Instance.TotalStar < data.unlockNeedTotal)
        {
            Debug.Log($"ShopManager: Unlock requirement not met ({data.unlockNeedTotal}¡ï required).");
            return;
        }

        // Must have enough current quota stars to pay the price
        if (!CurrencyManager.Instance.TrySpendStars(data.priceQuota))
        {
            Debug.Log("ShopManager: Not enough quota¡ï to purchase.");
            return;
        }

        // Grant the purchased item
        switch (data.type)
        {
            case ShopItemType.Consumable:
                // Distinguish between film and food by name convention
                if (data.itemName.Contains("Film"))
                    ConsumableManager.Instance.AddFilm(data.amount);
                else
                    ConsumableManager.Instance.AddFood(data.amount);
                break;

            case ShopItemType.Equipment:
                // Register equipment into Slot 3 cycling
                InventoryCycler.RegisterItem(data.linkedItem);
                Debug.Log($"ShopManager: Purchased equipment '{data.itemName}'. Use Q/E to cycle.");
                break;
        }
    }
}
