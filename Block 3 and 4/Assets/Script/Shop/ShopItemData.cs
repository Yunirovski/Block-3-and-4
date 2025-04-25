using UnityEngine;

/// <summary>
/// Defines the category of a shop item.
/// </summary>
public enum ShopItemType
{
    /// <summary>
    /// Consumable items that are used up on purchase/use (e.g., film, food).
    /// </summary>
    Consumable,

    /// <summary>
    /// Equipment items that can be equipped and cycled in Slot 3 (e.g., tools, weapons).
    /// </summary>
    Equipment
}

/// <summary>
/// ScriptableObject holding configuration data for a single shop entry.
/// Create instances under ¡°Shop/ItemData¡± in the Assets menu.
/// </summary>
[CreateAssetMenu(menuName = "Shop/ItemData")]
public class ShopItemData : ScriptableObject
{
    [Header("Basic Info")]

    /// <summary>
    /// User-facing name displayed in the shop UI.
    /// </summary>
    [Tooltip("Name shown to the player in the shop list.")]
    public string itemName;

    /// <summary>
    /// Whether this item is a Consumable or Equipment.
    /// </summary>
    [Tooltip("Determines purchase behavior: Consumable vs. Equipment.")]
    public ShopItemType type;

    [Header("Pricing & Unlock")]

    /// <summary>
    /// Number of quota¡ï required to buy this item.
    /// </summary>
    [Tooltip("Stars spent from your current quota to purchase.")]
    public int priceQuota;

    /// <summary>
    /// Total stars collected required to unlock this item.
    /// For consumables, set to 0 to make always available.
    /// </summary>
    [Tooltip("Total collection¡ï needed before this item appears in the shop.")]
    public int unlockNeedTotal;

    [Header("Item Effects")]

    /// <summary>
    /// If this is Equipment, reference the corresponding BaseItem ScriptableObject
    /// that will be registered into Slot 3 for cycling.
    /// </summary>
    [Tooltip("Assigned BaseItem for equipment purchases.")]
    public BaseItem linkedItem;

    /// <summary>
    /// Amount granted when purchasing a consumable (e.g., number of film rolls or food units).
    /// </summary>
    [Tooltip("Quantity received for consumable items.")]
    public int amount = 1;
}
