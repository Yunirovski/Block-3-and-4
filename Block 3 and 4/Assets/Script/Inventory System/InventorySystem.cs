// Assets/Scripts/Inventory/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 圆形工具环版背包系统：<br/>
/// • I 长按呼出工具环，滚轮 / 数字键 / 鼠标移动选槽 <br/>
/// • 松开 I → 装备所选物品，并立即进入 Ready 状态 <br/>
/// • 左键直接 OnUse（无右键 Ready 流程）<br/>
/// • 相机在取景状态下可继续左键拍照（忽略 PointerOverUI）
/// </summary>
public class InventorySystem : MonoBehaviour
{
    /* ───── 引用 & UI ───── */
    [Header("Anchors / UI")]
    public Transform itemAnchor;          // MainCamera 下 ItemAnchor
    public RadialInventoryUI radialUI;            // 圆形工具环
    public Canvas mainHUDCanvas;       // 常规 HUD
    public Canvas cameraHUDCanvas;     // 相机取景 HUD
    public TMP_Text debugTextTMP;        // 左上 Debug
    public TMP_Text detectTextTMP;       // 拍照结果

    [Header("Animator (可选)")]
    public Animator itemAnimator;
    public string switchTrigger = "SwitchItem";

    [Header("Item List (Cam→Food→Hook→Board→Gun→Wand)")]
    public List<BaseItem> availableItems;         // 固定 6 槽

    /* ───── 内部状态 ───── */
    BaseItem currentItem;
    GameObject currentModel;
    int currentIndex;
    int pendingIndex;
    bool ringOpen;

    /* ===================================================================== */
    void Start() => EquipSlot(0);

    void Update()
    {
        HandleRing();                 // I 键呼出 / 松开
        if (!ringOpen)
        {
            HandleNumberKeys();       // 1-6 直切
            HandleUse();              // 左键 Use
        }

        // 相机专属输入：Q / 左键拍照
        if (currentItem is CameraItem cam) cam.HandleInput();
    }

    /* ======================== I 键圆环 ======================== */
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
            int sel = radialUI.CurrentIndex;
            ringOpen = false;
            radialUI.Hide();

            if (sel == 1) RefreshSlot1List();      // 食物槽动态刷新
            if (sel >= 0 && sel != currentIndex) BeginSwitch(sel);
        }

        if (ringOpen)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) radialUI.Step(+1);
            else if (scroll < -0.01f) radialUI.Step(-1);
        }
    }

    /* ====================== 数字键兜底 ====================== */
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

    /* ======================== 左键 Use ======================== */
    void HandleUse()
    {
        if (currentItem == null) return;

        bool pointerOnUI = EventSystem.current.IsPointerOverGameObject();
        bool allowClick = !(currentItem is CameraItem) ? !pointerOnUI : true;

        if (Input.GetMouseButtonDown(0) && allowClick)
            currentItem.OnUse();
    }

    /* ======================= 切槽流程 ======================= */
    void BeginSwitch(int idx)
    {
        pendingIndex = idx;
        if (itemAnimator) itemAnimator.SetTrigger(switchTrigger);
        else OnSwitchAnimationComplete();
    }
    public void OnSwitchAnimationComplete() => EquipSlot(pendingIndex);

    void EquipSlot(int idx)
    {
        /* 1. 清理旧物 */
        currentItem?.OnUnready();
        currentItem?.OnDeselect();
        if (currentModel) Destroy(currentModel);

        /* 2. 记录新索引 */
        currentIndex = idx;
        currentItem = availableItems[idx];

        /* 3. 实例化模型（可换真正 prefab） */
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemAnchor, false);
        currentModel.name = currentItem.itemName + "_Model";

        /* 4. 应用持握偏移 */
        currentItem.ApplyHoldTransform(currentModel.transform);

        /* 5. 通知脚本 */
        currentItem.OnSelect(currentModel);
        currentItem.OnReady();                  // 立即 Ready

        /* 6. 专属注入 */
        if (currentItem is CameraItem cam)
        {
            cam.Init(Camera.main, mainHUDCanvas, cameraHUDCanvas, debugTextTMP, detectTextTMP);
        }
        else if (currentItem is FoodItem food)
        {
            food.debugText = debugTextTMP;
        }

        debugTextTMP?.SetText($"切换到 {currentItem.itemName}");
    }

    /* ================ Slot-1（食物）动态刷新 ================ */
    void RefreshSlot1List()
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;
        if (!list.Contains(availableItems[1]))
            availableItems[1] = list[0];
    }

    /* =================== 解锁布尔数组 =================== */
    bool[] BuildUnlockArray()
    {
        var pm = ProgressionManager.Instance;
        return new[]
        {
            true,                                // 0 相机
            true,                                // 1 食物
            pm && pm.HasGrapple,                 // 2 抓钩
            pm && pm.HasSkateboard,              // 3 滑板
            pm && pm.HasDartGun,                 // 4 麻醉枪
            pm && pm.HasMagicWand               // 5 魔法棒
        };
    }
}
