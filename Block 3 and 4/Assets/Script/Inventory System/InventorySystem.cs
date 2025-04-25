using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Core inventory system handling item selection, cycling, and use.
/// Supports four slots (0=Camera, 1=Log, 2=Map, 3=Food/Equipment) with special cycling behavior for slot 3.
/// Handles user input for switching slots, toggling ready state, and using the current item.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("Item & UI References")]
    [Tooltip("Parent transform under which the selected item's model will be instantiated.")]
    public Transform itemHolder;

    [Tooltip("Reference to the UI controller managing slot highlights and ready-state visuals.")]
    public InventoryUI inventoryUI;

    [Tooltip("Text field for general debug messages (e.g., switch warnings).")]
    public TMP_Text debugTextTMP;

    [Tooltip("Text field for detection messages (e.g., camera detections).")]
    public TMP_Text detectTextTMP;

    [Tooltip("World-space point where spawned items (like food) should appear.")]
    public Transform spawnPoint;

    [Header("Animation")]
    [Tooltip("Animator component to drive switch-item animations.")]
    public Animator itemAnimator;

    [Tooltip("Trigger parameter name in the Animator to play the switch animation.")]
    public string switchTrigger = "SwitchItem";

    [Header("Slots (0=Camera, 1=Log, 2=Map, 3=Food/Equip)")]
    [Tooltip("List of all possible items the player can switch between.")]
    public List<BaseItem> availableItems;

    // The currently selected item and its instantiated model
    private BaseItem currentItem;
    private GameObject currentModel;

    // Tracks whether the current item is in 'ready' (equipped) state
    private bool isReady;

    // Index of the slot pending to be switched to (used during animation)
    private int pendingIndex = -1;

    private void Start()
    {
        // If slot 3 has a valid initial item, seed the InventoryCycler for cycling
        if (availableItems.Count > 3 && availableItems[3] != null)
        {
            InventoryCycler.InitWith(availableItems[3]);
        }

        // Default to the first slot on start
        if (availableItems.Count > 0)
        {
            pendingIndex = 0;
            inventoryUI.HighlightSlot(0);
            PlaySwitchAnimation();
        }
    }

    private void Update()
    {
        HandleSlotSwitch();
        HandleReadyToggle();
        HandleUse();
    }

    /// <summary>
    /// Handles input for switching between inventory slots.
    /// Supports numeric keys 1–4, special cycling for slot 3, and bracket keys for food-type cycling.
    /// </summary>
    private void HandleSlotSwitch()
    {
        // 1) Direct slot selection via number keys
        for (int i = 0; i < availableItems.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (isReady)
                {
                    debugTextTMP.text = "Please holster your current item first!";
                    return;
                }
                if (pendingIndex == i) return;

                pendingIndex = i;
                inventoryUI.HighlightSlot(i);
                PlaySwitchAnimation();
                return;
            }
        }

        // 2) Special handling for slot 3 (Food/Equipment)
        if (pendingIndex == 3)
        {
            // — Food-type cycling via [ and ] keys
            if (availableItems[3] is FoodItem foodItem)
            {
                if (Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    Debug.Log("InventorySystem: Detected [ key");
                    foodItem.CycleFoodType(false);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.RightBracket))
                {
                    Debug.Log("InventorySystem: Detected ] key");
                    foodItem.CycleFoodType(true);
                    return;
                }
            }

            // — Cycle through registered slot-3 items via scroll wheel or Q/E keys
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta > 0f) CycleSlot3(true);
            else if (scrollDelta < 0f) CycleSlot3(false);

            if (Input.GetKeyDown(KeyCode.Q)) CycleSlot3(false);
            if (Input.GetKeyDown(KeyCode.E)) CycleSlot3(true);
        }
    }

    /// <summary>
    /// Cycles through all items registered in slot 3 (via InventoryCycler).
    /// Updates availableItems[3], displays debug text, and plays switch animation.
    /// </summary>
    /// <param name="forward">True to cycle forward; false to cycle backward.</param>
    private void CycleSlot3(bool forward)
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;

        int currentIndex = list.IndexOf(availableItems[3]);
        int nextIndex = (currentIndex + (forward ? 1 : -1) + list.Count) % list.Count;

        availableItems[3] = list[nextIndex];
        debugTextTMP.text = $"Switched to {list[nextIndex].itemName}";
        PlaySwitchAnimation();
    }

    /// <summary>
    /// Toggles the 'ready' (equipped) state of the current item on right mouse button.
    /// Updates animations, UI, and calls lifecycle methods on the item.
    /// </summary>
    private void HandleReadyToggle()
    {
        if (Input.GetMouseButtonDown(1) && currentItem != null)
        {
            isReady = !isReady;
            if (isReady) currentItem.OnReady();
            else currentItem.OnUnready();

            inventoryUI.SetReadyState(isReady);
        }
    }

    /// <summary>
    /// Executes the current item's use action on left mouse button, if ready and not clicking over UI.
    /// </summary>
    private void HandleUse()
    {
        if (!isReady || currentItem == null) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            currentItem.OnUse();
        }
    }

    /// <summary>
    /// Triggers the switch-item animation, or immediately completes the switch if no Animator is assigned.
    /// </summary>
    private void PlaySwitchAnimation()
    {
        if (itemAnimator != null)
        {
            itemAnimator.SetTrigger(switchTrigger);
        }
        else
        {
            OnSwitchAnimationComplete();
        }
    }

    /// <summary>
    /// Called by the Animator via animation event, or manually if no animation is used.
    /// Responsible for destroying the old model, instantiating the new one, 
    /// and invoking select callbacks on the new item.
    /// </summary>
    public void OnSwitchAnimationComplete()
    {
        Debug.Log($"OnSwitchAnimationComplete: pendingIndex={pendingIndex}");
        inventoryUI.HighlightSlot(pendingIndex);

        // Deselect and clean up previous item/model
        if (currentItem != null) currentItem.OnDeselect();
        if (currentModel != null) Destroy(currentModel);

        // Instantiate and initialize the new item
        currentItem = availableItems[pendingIndex];
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.name = currentItem.itemName + "_Model";

        currentItem.OnSelect(currentModel);

        // Inject dependencies for specific item types
        if (currentItem is FoodItem food)
        {
            // Inject the debug text UI so the FoodItem can display messages
            food.debugText = debugTextTMP;
            Debug.Log("Injected debugText into FoodItem");
            food.OnSelect(currentModel);
        }

        else if (currentItem is CameraItem cam)
        {
            cam.Init(Camera.main);
            cam.InitUI(debugTextTMP, detectTextTMP);
        }
    }
}
