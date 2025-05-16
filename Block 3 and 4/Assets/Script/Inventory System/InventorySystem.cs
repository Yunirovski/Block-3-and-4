// Assets/Scripts/Inventory System/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("Anchors")]
    [Tooltip("手持模型父节点")]
    public Transform itemAnchor;
    [Tooltip("脚下模型父节点，用于滑板等脚下道具")]
    public Transform footAnchor;

    [Header("Animator (可选)")]
    public Animator itemAnimator;
    public string switchTrigger = "SwitchItem";

    [Header("Item List (Cam→Food→Hook→Board→Gun→Wand)")]
    public List<BaseItem> availableItems; // 必须填 6 个

    // —— 内部状态 —— 
    BaseItem currentItem;
    GameObject currentModel;
    int currentIndex;
    int pendingIndex;
    bool ringOpen;

    void Start()
    {
        // 确保UIManager引用已经设置
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager未找到，请确保场景中有UIManager实例");
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

        // 相机专属输入：Q键/左键拍照
        if (currentItem is CameraItem cam)
        {
            cam.HandleInput();
        }
        // 滑板专属每帧更新
        else if (currentItem is SkateboardItem skate)
        {
            skate.HandleUpdate();
        }
    }

    // —— I 键呼出/松开工具环 —— 
    void HandleRing()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ringOpen = true;

            // 使用UIManager显示物品环
            UIManager.Instance.ShowInventoryRadial(BuildUnlockArray(), currentIndex);
        }
        else if (Input.GetKeyUp(KeyCode.I))
        {
            ringOpen = false;

            // 使用UIManager隐藏物品环
            UIManager.Instance.HideInventoryRadial();

            // 获取选择的物品索引
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

    // —— 数字键 1-6 切换 —— 
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

    // —— 鼠标左键 Use —— 
    void HandleUse()
    {
        if (currentItem == null) return;

        // 检查是否处于相机模式
        bool isCameraMode = UIManager.Instance.IsCameraMode();

        bool overUI = EventSystem.current.IsPointerOverGameObject();
        bool allow = !(currentItem is CameraItem) ? !overUI : true;

        // 在相机模式下，CameraItem自己处理输入
        if (Input.GetMouseButtonDown(0) && allow && !isCameraMode)
            currentItem.OnUse();
    }

    // —— 开始切换 —— 
    void BeginSwitch(int idx)
    {
        pendingIndex = idx;
        if (itemAnimator != null)
            itemAnimator.SetTrigger(switchTrigger);
        else
            OnSwitchAnimationComplete();
    }

    // Animator 事件或无动画时调用
    public void OnSwitchAnimationComplete()
    {
        EquipSlot(pendingIndex);
    }

    // —— 真正装备新槽 —— 
    void EquipSlot(int idx)
    {
        // 清理旧物
        currentItem?.OnUnready();
        currentItem?.OnDeselect();
        if (currentModel) Destroy(currentModel);

        // 记录新索引 & 道具
        currentIndex = idx;
        currentItem = availableItems[idx];

        // 选择父节点：滑板用 footAnchor，其它用 itemAnchor
        Transform parentTf = currentItem is SkateboardItem
                            ? footAnchor
                            : itemAnchor;

        // 实例化模型
        if (currentItem.modelPrefab != null)
            currentModel = Instantiate(currentItem.modelPrefab, parentTf);
        else
        {
            currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentModel.transform.SetParent(parentTf, false);
        }

        currentModel.name = currentItem.itemName + "_Model";

        // 应用持握 / 挂载偏移
        currentItem.ApplyHoldTransform(currentModel.transform);

        // 回调
        currentItem.OnSelect(currentModel);
        currentItem.OnReady();

        // 注入相机专属引用
        if (currentItem is CameraItem cam)
        {
            cam.Init(Camera.main);
        }
        else if (currentItem is FoodItem food)
        {
            // 使用UIManager更新食物类型文本
            string foodTypeName = "未设置";
            if (food.foodTypes.Count > 0 && food.foodTypes[0] != null)
                foodTypeName = food.foodTypes[0].ToString();

            UIManager.Instance.UpdateFoodTypeText(food.foodTypes[0]);
        }

        // 使用UIManager更新调试文本
        UIManager.Instance.UpdateCameraDebugText($"切换到 {currentItem.itemName}");
    }

    // —— 食物槽随 Slot3 列表刷新 —— 
    void RefreshSlot1List()
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;
        if (!list.Contains(availableItems[1]))
            availableItems[1] = list[0];
    }

    // —— 解锁布尔数组 —— 
    bool[] BuildUnlockArray()
    {
        var pm = ProgressionManager.Instance;
        return new[]
        {
            true,                                  // 0 相机
            true,                                  // 1 食物
            pm != null && pm.HasGrapple,           // 2 抓钩
            pm != null && pm.HasSkateboard,        // 3 滑板
            pm != null && pm.HasDartGun,           // 4 麻醉枪
            pm != null && pm.HasMagicWand          // 5 魔法棒
        };
    }
}