// Assets/Scripts/Inventory System/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("Anchors / UI")]
    public Transform itemAnchor;          // 手持模型父节点
    public RadialInventoryUI radialUI;    // 圆形工具环 UI
    public Canvas mainHUDCanvas;          // 常规 HUD Canvas
    public Canvas cameraHUDCanvas;        // 相机取景 HUD Canvas
    public TMP_Text debugTextTMP;         // 通用提示文本
    public TMP_Text detectTextTMP;        // 相机结果文本

    [Header("Animator (可选)")]
    public Animator itemAnimator;
    public string switchTrigger = "SwitchItem";

    [Header("Item List (Cam→Food→Hook→Board→Gun→Wand)")]
    public List<BaseItem> availableItems; // 必须填 6 个

    // 内部状态
    BaseItem currentItem;
    GameObject currentModel;
    int currentIndex;
    int pendingIndex;
    bool ringOpen;

    void Start()
    {
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
    }

    // —— I键呼出/松开工具环 —— 
    void HandleRing()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ringOpen = true;
            radialUI.SetUnlockedStates(BuildUnlockArray(), currentIndex);
            radialUI.Show();
        }
        else if (Input.GetKeyUp(KeyCode.I))
        {
            ringOpen = false;
            radialUI.Hide();

            int sel = radialUI.CurrentIndex;
            if (sel == 1) RefreshSlot1List();
            if (sel >= 0 && sel != currentIndex)
                BeginSwitch(sel);
        }

        if (ringOpen)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) radialUI.Step(+1);
            else if (scroll < -0.01f) radialUI.Step(-1);
        }
    }

    // —— 数字键 1-6 兜底切换 —— 
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

    // —— 左键直接 Use —— 
    void HandleUse()
    {
        if (currentItem == null) return;
        bool overUI = EventSystem.current.IsPointerOverGameObject();
        bool allow = !(currentItem is CameraItem) ? !overUI : true;
        if (Input.GetMouseButtonDown(0) && allow)
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

        // 记录新索引
        currentIndex = idx;
        currentItem = availableItems[idx];

        // 实例化模型（可替换为 prefab）
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemAnchor, false);
        currentModel.name = currentItem.itemName + "_Model";

        // 应用持握偏移
        currentItem.ApplyHoldTransform(currentModel.transform);

        // 回调
        currentItem.OnSelect(currentModel);
        currentItem.OnReady();

        // 注入相机或食物专属引用
        if (currentItem is CameraItem cam)
        {
            cam.Init(
                Camera.main,
                mainHUDCanvas,
                cameraHUDCanvas,
                debugTextTMP,
                detectTextTMP   // ← 这里补上第五个参数
            );
        }
        else if (currentItem is FoodItem food)
        {
            food.debugText = debugTextTMP;
        }

        debugTextTMP?.SetText($"切换到 {currentItem.itemName}");
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
            true,                              // 0 相机
            true,                              // 1 食物
            pm != null && pm.HasGrapple,       // 2 抓钩
            pm != null && pm.HasSkateboard,    // 3 滑板
            pm != null && pm.HasDartGun,       // 4 麻醉枪
            pm != null && pm.HasMagicWand      // 5 魔法棒
        };
    }
}
