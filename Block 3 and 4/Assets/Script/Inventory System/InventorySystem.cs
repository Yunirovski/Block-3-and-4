// Assets/Scripts/Inventory System/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("Anchors")]
    [Tooltip("Parent node for held models")]
    public Transform itemAnchor;
    [Tooltip("Parent node for foot models, used for skateboard and similar items")]
    public Transform footAnchor;

    [Header("Animator (Optional)")]
    public Animator itemAnimator;
    public string switchTrigger = "SwitchItem";

    [Header("Item List (Cam→Food→Hook→Board→Gun→Wand)")]
    public List<BaseItem> availableItems; // Must include 6 items

    // -- Internal state -- 
    BaseItem currentItem;
    GameObject currentModel;
    int currentIndex;
    int pendingIndex;
    bool ringOpen;

    void Start()
    {
        // Ensure UIManager reference is set
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager not found, please ensure there is a UIManager instance in the scene");
        }

        EquipSlot(0);
    }

    void Update()
    {
        HandleRing();

        if (!ringOpen)
        {
            HandleNumberKeys();
            HandleUse();
        }

        // Camera-specific input: Q key/left click for photos
        if (currentItem is CameraItem cam)
        {
            cam.HandleInput();
        }
        // Skateboard-specific update
        else if (currentItem is SkateboardItem skate)
        {
            skate.HandleUpdate();
        }
    }

    // -- Press E to open/release the toolring -- 
    // -- Press E to open/release the toolring -- 
    void HandleRing()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ringOpen = true;

            // Auto unlock cursor for tool ring
            var mouseManager = FindObjectOfType<PauseAndMouseManager>();
            if (mouseManager != null)
            {
                mouseManager.AutoUnlockCursor();
                Debug.Log("Tool ring opened - cursor unlocked");
            }
            else
            {
                // Fallback if no mouse manager found
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("Tool ring opened - cursor unlocked (fallback)");
            }

            // Use UIManager to display the item radial
            UIManager.Instance.ShowInventoryRadial(BuildUnlockArray(), currentIndex);
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            ringOpen = false;

            // Auto lock cursor when tool ring closes
            var mouseManager = FindObjectOfType<PauseAndMouseManager>();
            if (mouseManager != null)
            {
                mouseManager.AutoLockCursor();
                Debug.Log("Tool ring closed - cursor locked");
            }
            else
            {
                // Fallback if no mouse manager found
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Debug.Log("Tool ring closed - cursor locked (fallback)");
            }

            // Use UIManager to hide the item radial
            UIManager.Instance.HideInventoryRadial();

            // Get the selected item index
            int sel = UIManager.Instance.GetSelectedInventorySlot();
            if (sel == 1) RefreshSlot1List();
            if (sel >= 0 && sel != currentIndex)
                BeginSwitch(sel);
        }

        if (ringOpen)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) UIManager.Instance.StepInventorySelection(+1);
            else if (scroll < -0.01f) UIManager.Instance.StepInventorySelection(-1);
        }
    }

    // -- Number keys 1-6 for switching -- 
    void HandleNumberKeys()
    {
        bool[] unlocked = BuildUnlockArray();
        for (int i = 0; i < availableItems.Count && i < 9; i++)
        {
            if (!unlocked[i]) continue;
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i != currentIndex)
            {
                BeginSwitch(i);
                return;
            }
        }
    }

    // -- Left mouse button for Use -- 
    void HandleUse()
    {
        if (currentItem == null) return;

        // Check if in camera mode
        bool isCameraMode = UIManager.Instance.IsCameraMode();

        bool overUI = EventSystem.current.IsPointerOverGameObject();
        bool allow = !(currentItem is CameraItem) ? !overUI : true;

        // In camera mode, CameraItem handles its own input
        if (Input.GetMouseButtonDown(0) && allow && !isCameraMode)
            currentItem.OnUse();
    }

    // -- Begin switching -- 
    void BeginSwitch(int idx)
    {
        pendingIndex = idx;
        if (itemAnimator != null)
            itemAnimator.SetTrigger(switchTrigger);
        else
            OnSwitchAnimationComplete();
    }

    // Called by Animator event or when no animation
    public void OnSwitchAnimationComplete()
    {
        EquipSlot(pendingIndex);
    }

    // -- Actually equip the new slot -- 
    void EquipSlot(int idx)
    {
        // Clean up old item
        currentItem?.OnUnready();
        currentItem?.OnDeselect();
        if (currentModel) Destroy(currentModel);

        // Record new index & item
        currentIndex = idx;
        currentItem = availableItems[idx];

        // Select parent node: skateboard uses footAnchor, others use itemAnchor
        Transform parentTf = currentItem is SkateboardItem
                            ? footAnchor
                            : itemAnchor;

        // Instantiate model
        if (currentItem.modelPrefab != null)
            currentModel = Instantiate(currentItem.modelPrefab, parentTf);
        else
        {
            currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentModel.transform.SetParent(parentTf, false);
        }

        currentModel.name = currentItem.itemName + "_Model";

        // Apply hold/mount offset
        currentItem.ApplyHoldTransform(currentModel.transform);

        // Callbacks
        currentItem.OnSelect(currentModel);
        currentItem.OnReady();

        // Inject camera-specific reference
        if (currentItem is CameraItem cam)
        {
            cam.Init(Camera.main);
        }
        else if (currentItem is FoodItem food)
        {
            // Use UIManager to update food type text
            string foodTypeName = "Not set";
            if (food.foodTypes.Count > 0 && food.foodTypes[0] != null)
                foodTypeName = food.foodTypes[0].ToString();

            UIManager.Instance.UpdateFoodTypeText(food.foodTypes[0]);
        }

        // Use UIManager to update debug text
        UIManager.Instance.UpdateCameraDebugText($"Switched to {currentItem.itemName}");
    }

    // -- Food slot refresh with Slot3 list -- 
    void RefreshSlot1List()
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;
        if (!list.Contains(availableItems[1]))
            availableItems[1] = list[0];
    }

    // -- Unlock boolean array -- 
    bool[] BuildUnlockArray()
    {
        var pm = ProgressionManager.Instance;
        return new[]
        {
            true,                                  // 0 Camera
            true,                                  // 1 Food
            pm != null && pm.HasGrapple,           // 2 Grapple
            pm != null && pm.HasSkateboard,        // 3 Skateboard
            pm != null && pm.HasDartGun,           // 4 Dart Gun
            pm != null && pm.HasMagicWand          // 5 Magic Wand
        };
    }
}