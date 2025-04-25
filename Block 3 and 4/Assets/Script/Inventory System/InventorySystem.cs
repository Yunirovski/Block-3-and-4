// Assets/Scripts/Inventory/InventorySystem.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Core inventory system handling item selection, cycling, and use.
/// Supports four slots (0=Camera, 1=Log, 2=Map, 3=Food/Equipment) with special cycling behavior for slot 3.
/// Handles user input for switching slots, toggling ready state, and using the current item.
/// Injects UI references into CameraItem and FoodItem instances.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("Item & UI References")]
    [Tooltip("Parent transform under which the selected item's model will be instantiated.")]
    public Transform itemHolder;

    [Tooltip("Reference to the UI controller managing slot highlights and ready-state visuals.")]
    public InventoryUI inventoryUI;

    [Tooltip("Text field for general debug messages (e.g., switch warnings, camera cooldown).")]
    public TMP_Text debugTextTMP;

    [Tooltip("Text field for detection messages (e.g., camera detection results).")]
    public TMP_Text detectTextTMP;

    [Header("Animation")]
    [Tooltip("Animator component to drive switch-item animations.")]
    public Animator itemAnimator;
    [Tooltip("Trigger parameter name in the Animator to play the switch animation.")]
    public string switchTrigger = "SwitchItem";

    [Header("Slots (0=Camera, 1=Log, 2=Map, 3=Food/Equip)")]
    [Tooltip("List of all possible items the player can switch between.")]
    public List<BaseItem> availableItems;

    // Internals
    private BaseItem currentItem;
    private GameObject currentModel;
    private bool isReady;
    private int pendingIndex = -1;

    private void Start()
    {
        // Initialize cycling list for slot 3
        if (availableItems.Count > 3 && availableItems[3] != null)
            InventoryCycler.InitWith(availableItems[3]);

        // Select first slot
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

    private void HandleSlotSwitch()
    {
        // Direct slot selection via number keys 1-4
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

        // Special handling for slot 3 (Food/Equipment)
        if (pendingIndex == 3)
        {
            // Food-type cycling via [ and ] keys
            if (availableItems[3] is FoodItem foodItem)
            {
                if (Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    foodItem.CycleFoodType(false);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.RightBracket))
                {
                    foodItem.CycleFoodType(true);
                    return;
                }
            }

            // Cycle through registered slot-3 items via scroll wheel or Q/E keys
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) CycleSlot3(true);
            else if (scroll < 0f) CycleSlot3(false);

            if (Input.GetKeyDown(KeyCode.Q)) CycleSlot3(false);
            if (Input.GetKeyDown(KeyCode.E)) CycleSlot3(true);
        }
    }

    private void CycleSlot3(bool forward)
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;

        int current = list.IndexOf(availableItems[3]);
        int next = (current + (forward ? 1 : -1) + list.Count) % list.Count;
        availableItems[3] = list[next];
        debugTextTMP.text = $"Switched to {list[next].itemName}";
        PlaySwitchAnimation();
    }

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

    private void HandleUse()
    {
        if (!isReady || currentItem == null) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            currentItem.OnUse();
    }

    private void PlaySwitchAnimation()
    {
        if (itemAnimator != null)
            itemAnimator.SetTrigger(switchTrigger);
        else
            OnSwitchAnimationComplete();
    }

    /// <summary>
    /// Called by Animation Event or immediately if no Animator.
    /// Destroys previous model, instantiates the new one, calls OnSelect,
    /// and injects UI references into CameraItem and FoodItem.
    /// </summary>
    public void OnSwitchAnimationComplete()
    {
        inventoryUI.HighlightSlot(pendingIndex);

        // Deselect and destroy previous
        if (currentItem != null) currentItem.OnDeselect();
        if (currentModel != null) Destroy(currentModel);

        // Instantiate new model
        currentItem = availableItems[pendingIndex];
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.name = currentItem.itemName + "_Model";

        // Notify selection
        currentItem.OnSelect(currentModel);

        // Inject for FoodItem
        if (currentItem is FoodItem food)
        {
            food.debugText = debugTextTMP;
            food.OnSelect(currentModel);
        }
        // Inject for CameraItem
        else if (currentItem is CameraItem cam)
        {
            cam.Init(Camera.main);
            cam.InitUI(debugTextTMP, detectTextTMP);
        }
    }
}
