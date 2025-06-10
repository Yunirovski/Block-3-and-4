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

    // -- Click support --
    private int lastRadialIndex = -1; // 记录上次物品环的选择状态

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
        else
        {
            // 物品环打开时，检查点击选择
            HandleRadialSelection();
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
    void HandleRing()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ringOpen = true;
            lastRadialIndex = currentIndex; // 记录当前选择

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
            {
                BeginSwitch(sel);
                Debug.Log($"Tool ring selection changed from {currentIndex} to {sel}");
            }
        }

        if (ringOpen)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) UIManager.Instance.StepInventorySelection(+1);
            else if (scroll < -0.01f) UIManager.Instance.StepInventorySelection(-1);
        }
    }

    // -- Handle radial UI click selection --
    void HandleRadialSelection()
    {
        if (UIManager.Instance?.radialInventoryUI == null) return;

        int currentRadialIndex = UIManager.Instance.GetSelectedInventorySlot();

        // 检查选择是否发生变化（无论是通过点击还是滚轮）
        if (currentRadialIndex != lastRadialIndex && currentRadialIndex >= 0)
        {
            lastRadialIndex = currentRadialIndex;

            // 实时预览效果（可选）
            // 可以在这里添加预览逻辑，比如显示物品名称等
            if (currentRadialIndex < availableItems.Count && availableItems[currentRadialIndex] != null)
            {
                string itemName = availableItems[currentRadialIndex].itemName;
                Debug.Log($"Radial selection changed to: {itemName} (index {currentRadialIndex})");

                // 可以通过 UIManager 显示物品信息
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateCameraDebugText($"Selected: {itemName}");
                }
            }
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
        true,
        pm != null && pm.HasCamera,            // 1                                // 1 Food (always available)
        pm != null && pm.HasGrapple,           // 2 Grapple
        pm != null && pm.HasSkateboard,        // 3 Skateboard
        pm != null && pm.HasDartGun,           // 4 Dart Gun
        pm != null && pm.HasMagicWand          // 5 Magic Wand
    };
    }

    // -- Public methods for external access --

    /// <summary>
    /// 获取当前装备的物品索引
    /// </summary>
    public int GetCurrentItemIndex()
    {
        return currentIndex;
    }

    /// <summary>
    /// 获取当前装备的物品
    /// </summary>
    public BaseItem GetCurrentItem()
    {
        return currentItem;
    }

    /// <summary>
    /// 强制切换到指定物品（供外部调用）
    /// </summary>
    public void SwitchToItem(int itemIndex)
    {
        if (itemIndex >= 0 && itemIndex < availableItems.Count)
        {
            bool[] unlocked = BuildUnlockArray();
            if (unlocked[itemIndex])
            {
                BeginSwitch(itemIndex);
            }
            else
            {
                Debug.LogWarning($"Cannot switch to item {itemIndex}: item is locked");
            }
        }
        else
        {
            Debug.LogError($"Invalid item index: {itemIndex}");
        }
    }

    /// <summary>
    /// 检查物品环是否打开
    /// </summary>
    public bool IsRadialOpen()
    {
        return ringOpen;
    }
}