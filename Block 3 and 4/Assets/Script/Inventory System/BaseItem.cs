using UnityEngine;

/// <summary>
/// Abstract base class for all in‑game items.
/// Derive from this class to implement custom selection,
/// readiness, and usage behavior for your items.
/// </summary>
public abstract class BaseItem : ScriptableObject
{
    [Tooltip("Display name and identifier for this item")]
    public string itemName;

    /// <summary>
    /// Called when the player selects this item.
    /// Override to add custom logic — for example, highlight the model or show info.
    /// </summary>
    /// <param name="model">
    /// The instantiated GameObject representing this item  
    /// (e.g. the 3D model you might highlight or animate).
    /// </param>
    public virtual void OnSelect(GameObject model)
    {
        // Default implementation does nothing.
        // Derived items can use 'model' to enable visual feedback.
    }

    /// <summary>
    /// Called when the player deselects this item.
    /// Override to remove highlights or hide item-specific UI.
    /// </summary>
    public virtual void OnDeselect()
    {
        // Default implementation does nothing.
    }

    /// <summary>
    /// Called when the item enters a "ready" state (e.g. drawn or primed).
    /// Override to prepare animations, enable effects, or display UI hints.
    /// </summary>
    public virtual void OnReady()
    {
        // Default implementation does nothing.
    }

    /// <summary>
    /// Called when the item exits its "ready" state.
    /// Override to clean up any effects or UI enabled in OnReady().
    /// </summary>
    public virtual void OnUnready()
    {
        // Default implementation does nothing.
    }

    /// <summary>
    /// Called when the item is actually used (e.g. player presses the use button
    /// while the item is ready).  
    /// Must be implemented by every concrete item to define its primary effect.
    /// </summary>
    public abstract void OnUse();
}
