using System.Collections.Generic;

/// <summary>
/// Manages the cyclical list of items assignable to ¡°Slot 3¡± in the inventory.
/// Provides methods to initialize, clear, register, remove, and retrieve a safe copy of the list.
/// </summary>
public static class InventoryCycler
{
    // Underlying storage for all items currently in ¡°Slot 3¡±
    private static readonly List<BaseItem> slot3Items = new List<BaseItem>();

    /// <summary>
    /// Initializes the Slot 3 list at scene load or when changing zones.
    /// Clears any existing entries and, if provided, adds the specified initial item.
    /// </summary>
    /// <param name="initial">
    /// The default item to seed into Slot 3 (can be null to start empty).
    /// </param>
    public static void InitWith(BaseItem initial)
    {
        // Remove all existing items from the list
        slot3Items.Clear();

        // If an initial item was provided, add it as the first entry
        if (initial != null)
        {
            slot3Items.Add(initial);
        }
    }

    /// <summary>
    /// Registers a newly purchased or acquired item into Slot 3.
    /// Prevents duplicate entries.
    /// </summary>
    /// <param name="item">
    /// The item to add to the Slot 3 cycle (ignored if null or already present).
    /// </param>
    public static void RegisterItem(BaseItem item)
    {
        // Only add non-null items that aren't already in the list
        if (item != null && !slot3Items.Contains(item))
        {
            slot3Items.Add(item);
        }
    }

    /// <summary>
    /// Removes an item from Slot 3, e.g., when sold or recycled.
    /// </summary>
    /// <param name="item">
    /// The item to remove (ignored if null or not found).
    /// </param>
    public static void RemoveItem(BaseItem item)
    {
        // Safely attempt removal; List.Remove handles missing entries gracefully
        if (item != null)
        {
            slot3Items.Remove(item);
        }
    }

    /// <summary>
    /// Retrieves a copy of the current Slot 3 list.
    /// Returns a new list instance to prevent external modification of the internal list.
    /// </summary>
    /// <returns>
    /// A shallow copy of the list of items currently registered in Slot 3.
    /// </returns>
    public static List<BaseItem> GetSlot3List()
    {
        // Return a new list constructed from the internal list
        return new List<BaseItem>(slot3Items);
    }
}
