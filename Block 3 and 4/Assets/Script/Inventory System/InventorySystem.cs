// Assets/Scripts/Inventory/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("Anchors / UI")]
    public Transform itemAnchor;          // MainCamera > ItemAnchor
    public RadialInventoryUI radialUI;
    public Canvas mainHUDCanvas;
    public Canvas cameraHUDCanvas;
    public TMP_Text debugTextTMP;
    public TMP_Text detectTextTMP;

    [Header("Item List (Cam→Food→Hook→Board→Gun→Wand)")]
    public List<BaseItem> availableItems;         // 固定 6 槽

    /* ───── 内部状态 ───── */
    BaseItem curItem;
    GameObject curModel;
    int curIdx;
    bool ringOpen;

    /* ================================================================= */
    void Start() => EquipSlot(0);

    void Update()
    {
        HandleRing();
        if (!ringOpen)
        {
            HandleNumberKeys();
            HandleUse();
        }
        if (curItem is CameraItem cam) cam.HandleInput();
    }

    /* =============== I 键圆环 =============== */
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
            if (sel >= 0 && sel != curIdx) EquipSlot(sel);
        }

        if (ringOpen)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) radialUI.Step(+1);
            else if (scroll < -0.01f) radialUI.Step(-1);
        }
    }

    /* =========== 数字键兜底 =========== */
    void HandleNumberKeys()
    {
        bool[] unlocked = BuildUnlocks();
        for (int i = 0; i < availableItems.Count && i < 9; i++)
        {
            if (!unlocked[i]) continue;
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i != curIdx)
            { EquipSlot(i); return; }
        }
    }

    /* ============= 左键 Use ============= */
    void HandleUse()
    {
        if (curItem == null) return;
        bool overUI = EventSystem.current.IsPointerOverGameObject();
        bool allow = !(curItem is CameraItem) ? !overUI : true;

        if (Input.GetMouseButtonDown(0) && allow)
            curItem.OnUse();
    }

    /* ============= EquipSlot ============= */
    void EquipSlot(int idx)
    {
        /* 1. 移除旧物 */
        curItem?.OnUnready();
        curItem?.OnDeselect();
        if (curModel) Destroy(curModel);

        /* 2. 记录新物 */
        curIdx = idx;
        curItem = availableItems[idx];

        /* 3. 实例化模型 */
        GameObject prefab = curItem.modelPrefab;
        curModel = prefab ? Instantiate(prefab) :
                            GameObject.CreatePrimitive(PrimitiveType.Cube);
        curModel.transform.SetParent(itemAnchor, false);
        curModel.name = curItem.itemName + "_Model";

        /* 4. 应用偏移 & 通知脚本 */
        curItem.ApplyHoldTransform(curModel.transform);
        curItem.OnSelect(curModel);
        curItem.OnReady();

        /* 5. 相机 / 食物特别注入 */
        if (curItem is CameraItem cam)
        {
            cam.Init(Camera.main, mainHUDCanvas, cameraHUDCanvas,
                     debugTextTMP, detectTextTMP);
        }
        else if (curItem is FoodItem food)
        { food.debugText = debugTextTMP; }

        debugTextTMP?.SetText($"切换到 {curItem.itemName}");
    }

    /* ========= 食物槽跟随 Slot3 列表 ========= */
    void RefreshFoodSlot()
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;
        if (!list.Contains(availableItems[1]))
            availableItems[1] = list[0];
    }

    /* ============ 解锁布尔数组 ============ */
    bool[] BuildUnlocks()
    {
        var pm = ProgressionManager.Instance;
        return new[]
        {
            true,                            // 0 相机
            true,                            // 1 食物
            pm && pm.HasGrapple,             // 2 抓钩
            pm && pm.HasSkateboard,          // 3 滑板
            pm && pm.HasDartGun,             // 4 麻醉枪
            pm && pm.HasMagicWand           // 5 魔法棒
        };
    }
}
