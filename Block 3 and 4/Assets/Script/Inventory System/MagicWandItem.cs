using UnityEngine;

/// <summary>
/// A magic wand item that, when used, attracts nearby animals toward the player.
/// Has a configurable effect radius and cooldown period.
/// </summary>
[CreateAssetMenu(menuName = "Items/MagicWandItem")]
public class MagicWandItem : BaseItem
{
    [Header("Wand Effect Settings")]
    [Tooltip("Radius (in world units) within which animals will be affected.")]
    public float radius = 20f;

    [Tooltip("Time (in seconds) before the wand can be used again.")]
    public float cooldown = 60f;

    // Timestamp (Time.time) when the wand will next be ready to use
    private float nextReadyTime;

    /// <summary>
    /// Called when the player uses the wand (e.g., left-click).
    /// Scans for nearby AnimalEvent components within the radius and triggers attraction behavior.
    /// Enforces the cooldown between uses and notifies any listeners of cooldown start.
    /// </summary>
    public override void OnUse()
    {
        // If still on cooldown, ignore the use request
        if (Time.time < nextReadyTime)
            return;

        // Perform an overlap sphere to find all colliders within the effect radius
        Vector3 origin = Camera.main.transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, radius);

        foreach (var hit in hits)
        {
            // If the collider has an AnimalEvent component, attract that animal
            if (hit.TryGetComponent<AnimalEvent>(out var animalEvent))
            {
                Debug.Log($"MagicWandItem: Attracting {animalEvent.animalName}");
                // TODO: Implement AI logic so the animal changes its target to the player
                // Example: animalEvent.SetTargetPlayer(Camera.main.transform);
            }
        }

        // Set the next allowed use time based on the cooldown
        nextReadyTime = Time.time + cooldown;

        // Notify any systems listening for cooldown start (e.g., UI cooldown indicator)
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the wand's effect radius in the Scene view for debugging
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Camera.main.transform.position, radius);
    }
}
