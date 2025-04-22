using UnityEngine;

/// <summary>
/// ScriptableObject representing a map item.
/// When used, displays the map panel; when un‑readied, hides it.
/// </summary>
[CreateAssetMenu(menuName = "Items/MapItem")]
public class MapItem : BaseItem
{
    [Tooltip("Reference to the UI panel GameObject that displays the map.")]
    public GameObject mapPanel;

    /// <summary>
    /// Called when the player uses the map item (e.g. opens the map).
    /// Activates the mapPanel to show the map.
    /// </summary>
    public override void OnUse()
    {
        if (mapPanel != null)
            mapPanel.SetActive(true);
        else
            Debug.LogWarning($"MapItem: Missing mapPanel reference on '{itemName}'.");
    }

    /// <summary>
    /// Called when the item exits its ready state (e.g. closes the map).
    /// Deactivates the mapPanel to hide the map.
    /// </summary>
    public override void OnUnready()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);
        else
            Debug.LogWarning($"MapItem: Missing mapPanel reference on '{itemName}'.");
    }
}
