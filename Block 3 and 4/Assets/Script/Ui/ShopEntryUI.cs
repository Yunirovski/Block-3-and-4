using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component representing a single entry in the shop's item list.
/// Displays the item¡¯s name, price, and unlock requirements, and handles purchase attempts.
/// </summary>
public class ShopEntryUI : MonoBehaviour
{
    [Header("UI References")]

    /// <summary>
    /// Text label showing the item¡¯s name and price or unlock requirements.
    /// </summary>
    [Tooltip("Displays the item name and price/unlock info.")]
    public TMP_Text label;

    /// <summary>
    /// Button the player clicks to attempt a purchase.
    /// </summary>
    [Tooltip("Button used to buy this shop item.")]
    public Button buyButton;

    // Backing data for this entry, set on initialization
    private ShopItemData data;

    /// <summary>
    /// Initializes the entry with its ShopItemData.
    /// Sets up the button callback and refreshes the display.
    /// </summary>
    /// <param name="d">The ShopItemData to represent.</param>
    public void Init(ShopItemData d)
    {
        data = d;

        // Register click listener: forward purchase request to ShopManager
        buyButton.onClick.AddListener(() => ShopManager.Instance.TryBuy(data));

        // Initial UI update based on current currency and unlock status
        Refresh();
    }

    /// <summary>
    /// Updates the label text and button interactivity based on:
    /// - Whether the item is unlocked (TotalStar >= unlockNeedTotal)
    /// - Whether the player can afford it (QuotaStar >= priceQuota)
    /// </summary>
    public void Refresh()
    {
        bool unlocked = CurrencyManager.Instance.TotalStar >= data.unlockNeedTotal;
        bool canAfford = CurrencyManager.Instance.QuotaStar >= data.priceQuota;

        // Enable or disable the buy button
        buyButton.interactable = unlocked && canAfford;

        // Update label: show price if unlocked; otherwise show required total stars
        if (unlocked)
        {
            label.text = $"{data.itemName} - {data.priceQuota}¡ï";
        }
        else
        {
            label.text = $"{data.itemName} - Unlock at ¦²¡ï{data.unlockNeedTotal}";
        }
    }
}
