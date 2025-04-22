using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Manages the player’s inventory with “locked” slot switching:
/// prevents changing items while the current item is in its ready state.
/// Handles selection/deselection, ready/unready toggling, and item usage.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("Item Container & UI References")]
    [Tooltip("Parent transform under which the selected item's model will be instantiated.")]
    public Transform itemHolder;
    [Tooltip("Controller for the bottom‑bar UI (slot highlighting, ready state icon).")]
    public InventoryUI inventoryUI;

    [Header("On‑Screen Debug & Detection Text")]
    [Tooltip("Text field for displaying debug or status messages (e.g. errors, hints).")]
    public TMP_Text debugTextTMP;
    [Tooltip("Text field for displaying detection results (used by CameraItem).")]
    public TMP_Text detectTextTMP;

    [Header("Switch Animation Settings")]
    [Tooltip("Animator component used to play the item‑switch animation.")]
    public Animator itemAnimator;
    [Tooltip("Name of the trigger parameter in the Animator to start switching.")]
    public string switchTrigger = "SwitchItem";

    [Header("Available Items (Slots 0=Camera, 1=Log, 2=Map, 3=Food)")]
    [Tooltip("List of BaseItem assets representing each inventory slot.")]
    public List<BaseItem> availableItems;

    // --- Internal State ---
    private BaseItem currentItem;      // Currently selected item
    private GameObject currentModel;   // Instantiated placeholder/model for the current item
    private bool isReady;              // Whether the current item is in its ready state
    private int pendingIndex = -1;     // Index of the next item to switch to

    /// <summary>
    /// On start, if there are any items available, prepare to select the first slot.
    /// </summary>
    private void Start()
    {
        if (availableItems != null && availableItems.Count > 0)
        {
            pendingIndex = 0;
            inventoryUI.HighlightSlot(0);
            PlaySwitchAnimation();
        }
    }

    /// <summary>
    /// Per‑frame input handling:
    /// 1) Number keys (1–4) to switch slots (locked if an item is ready).
    /// 2) Right mouse button to toggle ready/unready on the current item.
    /// 3) Left mouse button to use the item when it is ready (ignores UI clicks).
    /// </summary>
    private void Update()
    {
        // 1) Slot switching via number keys
        for (int i = 0; i < availableItems.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                // Prevent switching while an item is in ready state
                if (isReady)
                {
                    if (debugTextTMP != null)
                        debugTextTMP.text = "Please holster your current item first!";
                    return;
                }

                // If the chosen slot is already pending, do nothing
                if (pendingIndex == i)
                    return;

                pendingIndex = i;
                inventoryUI.HighlightSlot(i);
                PlaySwitchAnimation();
                return;
            }
        }

        // 2) Toggle ready/unready on right-click
        if (Input.GetMouseButtonDown(1) && currentItem != null)
        {
            isReady = !isReady;
            if (isReady)
                currentItem.OnReady();
            else
                currentItem.OnUnready();

            inventoryUI.SetReadyState(isReady);
        }

        // 3) Use the item on left-click if it's ready and not clicking on UI
        if (isReady && Input.GetMouseButtonDown(0) && currentItem != null)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
                currentItem.OnUse();
        }
    }

    /// <summary>
    /// Triggers the switch animation, or immediately completes the switch
    /// if no Animator has been assigned.
    /// </summary>
    private void PlaySwitchAnimation()
    {
        if (itemAnimator != null)
            itemAnimator.SetTrigger(switchTrigger);
        else
            OnSwitchAnimationComplete();
    }

    /// <summary>
    /// Called via Animation Event at the end of the switch animation.
    /// Destroys the previous model, instantiates the new one, and notifies the item.
    /// Also injects CameraItem references when appropriate.
    /// </summary>
    public void OnSwitchAnimationComplete()
    {
        // Highlight the newly selected slot
        inventoryUI.HighlightSlot(pendingIndex);

        // Deselect and clean up the previous item
        if (currentItem != null)
            currentItem.OnDeselect();
        if (currentModel != null)
            Destroy(currentModel);

        // Set the new current item
        currentItem = availableItems[pendingIndex];

        // Instantiate a placeholder cube as the item model
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.name = currentItem.itemName + "_Model";

        // Notify the item that it has been selected
        currentItem.OnSelect(currentModel);

        // If this is a CameraItem, inject the camera and UI references
        if (currentItem is CameraItem camItem)
        {
            camItem.Init(Camera.main);
            camItem.InitUI(debugTextTMP, detectTextTMP);
        }
    }
}
