using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the UI representation of the player's inventory slots,
/// including highlighting the selected slot and displaying the ready icon.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Slot UI")]
    [Tooltip("List of UI Image components representing each inventory slot background.")]
    public List<Image> slotImages;

    [Tooltip("Color used for non-selected (normal) slots.")]
    public Color normalColor = Color.white;

    [Tooltip("Color used to highlight the currently selected slot.")]
    public Color highlightColor = Color.yellow;

    [Header("Ready State Icon")]
    [Tooltip("UI GameObject that indicates when an item is in the ready-to-use state.")]
    public GameObject readyIcon;

    /// <summary>
    /// Highlights the slot at the given index by setting its image color
    /// to <see cref="highlightColor"/> and resetting all others to <see cref="normalColor"/>.
    /// </summary>
    /// <param name="index">Index of the slot to highlight (0-based).</param>
    public void HighlightSlot(int index)
    {
        // Iterate through all slot images and update their colors based on the selected index.
        for (int i = 0; i < slotImages.Count; i++)
        {
            bool isSelected = (i == index);
            slotImages[i].color = isSelected ? highlightColor : normalColor;
        }
    }

    /// <summary>
    /// Toggles the visibility of the ready state icon.
    /// </summary>
    /// <param name="ready">True to show the icon, false to hide it.</param>
    public void SetReadyState(bool ready)
    {
        if (readyIcon == null)
        {
            Debug.LogWarning("InventoryUI: readyIcon reference is missing.");
            return;
        }

        // Activate or deactivate the ready icon based on the ready parameter.
        readyIcon.SetActive(ready);
    }
}
