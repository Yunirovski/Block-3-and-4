// Assets/Scripts/Inventory/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 圆形工具环版背包系统：
/// • I 长按呼出工具环，滚轮/数字键/鼠标移动选槽
/// • 松开 I → 装备所选物品，并立即进入 Ready 状态
/// • 左键直接 OnUse（相机模式下仍可拍照）
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("Anchors / UI")]
    public Transform itemAnchor;          // MainCamera > ItemAnchor
    public RadialInventoryUI radialUI;    // 圆形 UI
    public Canvas mainHUDCanvas;          // 常规 HUD
    public Canvas cameraHUDCanvas;        // 相机取景 HUD
    public TMP_Text debugTextTMP;         // 上方调试/提示

    [Header("Item List (Cam→Food→Hook→Board→Gun→Wand)")]
    public List<BaseItem> availableItems; // 固定 6 槽

    // 当前状态
    BaseItem curItem;
    GameObject curModel;
    int curIdx;
    bool ringOpen;

    void Start() => EquipSlot(0);

    void Update()
    {
        HandleRing();

        if (!ringOpen)
        {
            HandleNumberKeys();
            HandleUse();
        }

        // 相机专属输入
        if (curItem is CameraItem cam)
            cam.HandleInput();
    }

    // I 键弹出圆环
    void HandleRing()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ringOpen = true;
            radialUI.SetUnlockedStates(BuildUnlocks(), curIdx);
            radialUI.Show();
        }
        else if (Input.GetKeyUp(KeyCode.I))
        {
            ringOpen = false;
            radialUI.Hide();

            int sel = radialUI.CurrentIndex;
            if (sel == 1) RefreshFoodSlot();
            if (sel >= 0 && sel != curIdx)
                EquipSlot(sel);
        }

        if (ringOpen)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) radialUI.Step(+1);
            else if (scroll < -0.01f) radialUI.Step(-1);
        }
    }

    // 数字键兜底
    void HandleNumberKeys()
    {
        var unlocks = BuildUnlocks();
        for (int i = 0; i < availableItems.Count && i < 9; i++)
        {
            if (!unlocks[i]) continue;
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i != curIdx)
            {
                EquipSlot(i);
                return;
            }
        }
    }

    // 左键 Use
    void HandleUse()
    {
        if (curItem == null) return;

        bool overUI = EventSystem.current.IsPointerOverGameObject();
        bool allow = !(curItem is CameraItem) ? !overUI : true;
        if (Input.GetMouseButtonDown(0) && allow)
            curItem.OnUse();
    }

    // 装备槽
    void EquipSlot(int idx)
    {
        // 1. 卸载旧物
        curItem?.OnUnready();
        curItem?.OnDeselect();
        if (curModel) Destroy(curModel);

        // 2. 记录新索引
        curIdx = idx;
        curItem = availableItems[idx];

        // 3. 实例化模型（若无 Prefab 则生成立方体）
        GameObject prefab = curItem.modelPrefab;
        curModel = prefab != null
            ? Instantiate(prefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        curModel.transform.SetParent(itemAnchor, false);
        curModel.name = curItem.itemName + "_Model";

        // 4. 应用手持偏移 & 通知脚本
        curItem.ApplyHoldTransform(curModel.transform);
        curItem.OnSelect(curModel);
        curItem.OnReady();

        // 5. 相机 / 食物专属注入
        if (curItem is CameraItem cam)
        {
            cam.Init(Camera.main,
                     mainHUDCanvas,
                     cameraHUDCanvas,
                     debugTextTMP);
        }
        else if (curItem is FoodItem food)
        {
            food.debugText = debugTextTMP;
        }

        debugTextTMP?.SetText($"切换到 {curItem.itemName}");
    }

    // 食物槽动态刷新
    void RefreshFoodSlot()
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;
        if (!list.Contains(availableItems[1]))
            availableItems[1] = list[0];
    }

    // 解锁布尔数组
    bool[] BuildUnlocks()
    {
        var pm = ProgressionManager.Instance;
        return new bool[]
        {
            true,                                // 相机
            true,                                // 食物
            pm && pm.HasGrapple,                 // 抓钩
            pm && pm.HasSkateboard,              // 滑板
            pm && pm.HasDartGun,                 // 麻醉枪
            pm && pm.HasMagicWand                // 魔法棒
        };
    }
}
