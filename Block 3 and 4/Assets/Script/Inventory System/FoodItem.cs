using UnityEngine;

/// <summary>
/// ScriptableObject representing a consumable food item.
/// When used, this will instantiate a food prefab at a specified spawn point.
/// </summary>
[CreateAssetMenu(menuName = "Items/FoodItem")]
public class FoodItem : BaseItem
{
    [Tooltip("Prefab of the food object to spawn when this item is used")]
    public GameObject foodPrefab;

    [Tooltip("Transform indicating where to instantiate the food prefab")]
    public Transform spawnPoint;

    /// <summary>
    /// Called when the player uses this item (e.g., presses the use button).
    /// Spawns the configured food prefab at the designated spawn point.
    /// </summary>
    public override void OnUse()
    {
        if (!ConsumableManager.Instance.UseFood())   // 胶卷一样
        {
            Debug.Log("没有食物可投放！");
            return;
        }
        // Verify that both prefab and spawn point have been assigned
        if (foodPrefab != null && spawnPoint != null)
        {
            // Instantiate the food prefab with no rotation
            GameObject spawnedFood = Instantiate(
                foodPrefab,
                spawnPoint.position,
                Quaternion.identity
            );

            // TODO: Add feeding logic here (e.g., reduce hunger, play sound or animation)
        }
        else
        {
            // Warn if setup is incomplete to help catch configuration errors
            Debug.LogWarning(
                "FoodItem OnUse failed: " +
                (foodPrefab == null ? "foodPrefab is null. " : "") +
                (spawnPoint == null ? "spawnPoint is null." : "")
            );
        }
    }
}
