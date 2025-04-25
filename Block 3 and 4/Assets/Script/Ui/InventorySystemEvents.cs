using System;

/// <summary>
/// Centralized event hub for inventory system broadcasts.
/// </summary>
public static class InventorySystemEvents
{
    /// <summary>
    /// Invoked when an item begins its cooldown period after use.
    /// </summary>
    /// <param name="item">The BaseItem instance entering cooldown.</param>
    /// <param name="cooldownSeconds">Length of the cooldown in seconds.</param>
    public static Action<BaseItem, float> OnItemCooldownStart;
}
