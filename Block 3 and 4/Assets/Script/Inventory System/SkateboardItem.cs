using UnityEngine;

[CreateAssetMenu(menuName = "Items/SkateboardItem")]
public class SkateboardItem : BaseItem
{
    [Header("Skateboard Settings")]
    [Tooltip("Multiplier applied to the base movement speed (e.g., 1.3 = +30% speed).")]
    public float speedMultiplier = 1.3f;

    // Reference to the scene¡¯s movement controller, used to adjust player speed
    private IMoveController moveController;

    // Tracks whether the speed boost is currently active
    private bool isActive;

    /// <summary>
    /// Called when this item is selected in the inventory.
    /// Searches the scene for an object implementing IMoveController to apply speed modifications.
    /// </summary>
    /// <param name="model">Instantiated model for preview (unused here).</param>
    public override void OnSelect(GameObject model)
    {
        // Search all MonoBehaviours in the scene, then filter by IMoveController
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>())
        {
            if (mb is IMoveController mc)
            {
                moveController = mc;
                break;
            }
        }

        // Warn if no movement controller was found to apply the skateboard effect
        if (moveController == null)
        {
            Debug.LogWarning("SkateboardItem: No IMoveController implementation found in the scene.");
        }
    }

    /// <summary>
    /// Called when the player readies (equips) the skateboard.
    /// Applies the speed multiplier to the movement controller.
    /// </summary>
    public override void OnReady()
    {
        if (!isActive && moveController != null)
        {
            moveController.ModifySpeed(speedMultiplier);
            isActive = true;
            Debug.Log("SkateboardItem: Speed boost activated.");
        }
    }

    /// <summary>
    /// Called when the player un-readies (holsters) the skateboard.
    /// Resets the speed multiplier back to normal.
    /// </summary>
    public override void OnUnready()
    {
        if (isActive && moveController != null)
        {
            moveController.ModifySpeed(1f);  // Restore base speed
            isActive = false;
            Debug.Log("SkateboardItem: Speed boost deactivated.");
        }
    }

    /// <summary>
    /// Called when the player uses the skateboard (e.g., left-click).
    /// Skateboard has no direct 'use' action beyond ready/unready, so this is intentionally empty.
    /// </summary>
    public override void OnUse()
    {
        // No action on use; speed is managed via ready/unready toggles
    }
}
