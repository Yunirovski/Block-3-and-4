using UnityEngine;

/// <summary>
/// Component that allows the player to pick up a FoodItem asset when entering its trigger collider.
/// Attach this to a GameObject with a trigger Collider to represent the pickup in the scene.
/// </summary>
public class FoodPickup : MonoBehaviour
{
    [Tooltip("ScriptableObject asset representing the food to add to the inventory")]
    public FoodItem foodData;

    /// <summary>
    /// Called by Unity when another collider enters this trigger zone.
    /// If the other object has an InventorySystem, this food item is placed into the inventory and
    /// the pickup object is destroyed to prevent further collection.
    /// </summary>
    /// <param name="other">The Collider of the object that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Try to get the InventorySystem component from the other object
        InventorySystem inv = other.GetComponent<InventorySystem>();
        if (inv == null)
        {
            Debug.LogWarning($"FoodPickup: No InventorySystem found on {other.gameObject.name}.");
            return;
        }

        // Assume the 4th slot (index 3) in availableItems is reserved for food
        int slotIndex = 3;
        if (slotIndex < 0 || slotIndex >= inv.availableItems.Count)
            Debug.LogError($"FoodPickup: Slot index {slotIndex} is out of range (inventory size: {inv.availableItems.Count}).");

        // Assign this foodData to the reserved slot
        inv.availableItems[slotIndex] = foodData;
        Debug.Log($"FoodPickup: Added '{foodData.itemName}' to inventory slot {slotIndex}.");

        // Destroy the pickup object so it can't be collected again
        Destroy(gameObject);
    }
}

