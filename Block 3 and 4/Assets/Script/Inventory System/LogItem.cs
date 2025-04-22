using UnityEngine;

/// <summary>
/// ScriptableObject representing a journal/log item.
/// When used, shows a UI panel; when un‑readied, hides it.
/// </summary>
[CreateAssetMenu(menuName = "Items/LogItem")]
public class LogItem : BaseItem
{
    [Tooltip("Reference to the UI panel GameObject displaying the log contents.")]
    public GameObject logPanel;

    /// <summary>
    /// Called when the player uses this item (e.g. opens the log).
    /// Activates the logPanel to display its contents.
    /// </summary>
    public override void OnUse()
    {
        if (logPanel != null)
            logPanel.SetActive(true);
        else
            Debug.LogWarning($"LogItem: Missing logPanel reference on '{itemName}'.");
    }

    /// <summary>
    /// Called when the item exits its ready state (e.g. closes the log).
    /// Deactivates the logPanel to hide its contents.
    /// </summary>
    public override void OnUnready()
    {
        if (logPanel != null)
            logPanel.SetActive(false);
        else
            Debug.LogWarning($"LogItem: Missing logPanel reference on '{itemName}'.");
    }
}
