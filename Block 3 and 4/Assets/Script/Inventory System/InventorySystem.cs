// Assets/Scripts/Inventory System/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("Anchors / UI")]
    [Tooltip("手持模型父节点")]
    public Transform itemAnchor;
    [Tooltip("脚下模型父节点，用于滑板等脚下道具")]
    public Transform footAnchor;
    public RadialInventoryUI radialUI;
    public Canvas mainHUDCanvas;
    public Canvas cameraHUDCanvas;
    public TMP_Text debugTextTMP;
    public TMP_Text detectTextTMP;

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
        Debug.Log("InventorySystem: Start被调用");

        // 确保列表长度足够
        if (availableItems == null || availableItems.Count < 6)
        {
            Debug.LogWarning("InventorySystem: availableItems列表为空或长度不足，创建新列表");
            availableItems = new List<BaseItem>(6);
            while (availableItems.Count < 6)
            {
                availableItems.Add(null);
            }
        }

        // 尝试初始化槽位
        try
        {
            EquipSlot(0);
            Debug.Log("InventorySystem: 成功初始化装备槽");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InventorySystem: 初始化装备槽失败: {e.Message}");
        }
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
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool allow = !(currentItem is CameraItem) ? !overUI : true;
        if (Input.GetMouseButtonDown(0) && allow)
            currentItem.OnUse();
    }

    // —— 开始切换 —— 
    void BeginSwitch(int idx)
    {
        if (idx < 0 || idx >= availableItems.Count || availableItems[idx] == null)
        {
            Debug.LogWarning($"InventorySystem: 无法切换到槽位 {idx}，无效的索引或道具为空");
            return;
        }

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
        if (idx < 0 || idx >= availableItems.Count || availableItems[idx] == null)
        {
            Debug.LogWarning($"InventorySystem: 无法装备槽位 {idx}，无效的索引或道具为空");
            return;
        }

        // 清理旧物
        try
        {
            currentItem?.OnUnready();
            currentItem?.OnDeselect();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InventorySystem: 清理旧道具时出错: {e.Message}");
        }

        if (currentModel)
        {
            try
            {
                Destroy(currentModel);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"InventorySystem: 销毁旧模型时出错: {e.Message}");
            }
        }

        try
        {
            // 记录新索引 & 道具
            currentIndex = idx;
            currentItem = availableItems[idx];

            // 选择父节点：滑板用 footAnchor，其它用 itemAnchor
            Transform parentTf = currentItem is SkateboardItem
                                ? footAnchor
                                : itemAnchor;

            if (parentTf == null)
            {
                Debug.LogError("InventorySystem: 父节点为空，无法装备道具");
                return;
            }

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

            // 注入相机或食物专属引用
            if (currentItem is CameraItem cam)
            {
                cam.Init(
                    Camera.main,
                    mainHUDCanvas,
                    cameraHUDCanvas,
                    debugTextTMP,
                    detectTextTMP
                );
            }
            else if (currentItem is FoodItem food)
            {
                food.debugText = debugTextTMP;
            }

            debugTextTMP?.SetText($"切换到 {currentItem.itemName}");
            Debug.Log($"InventorySystem: 成功切换到道具 {currentItem.itemName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InventorySystem: 装备道具时出错: {e.Message}");
        }
    }

    // —— 食物槽随 Slot3 列表刷新 —— 
    void RefreshSlot1List()
    {
        try
        {
            var list = InventoryCycler.GetSlot3List();
            if (list.Count == 0) return;
            if (!list.Contains(availableItems[1]))
                availableItems[1] = list[0];

            Debug.Log("InventorySystem: 刷新食物槽成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InventorySystem: 刷新食物槽失败: {e.Message}");
        }
    }

    // 修改解锁布尔数组方法，添加额外的安全检查
    bool[] BuildUnlockArray()
    {
        // 默认解锁状态，如果找不到ProgressionManager则使用此状态
        bool hasGrapple = false;
        bool hasSkateboard = false;
        bool hasDartGun = false;
        bool hasMagicWand = false;

        // 尝试从ProgressionManager获取解锁状态
        var pm = FindProgressionManager();
        if (pm != null)
        {
            hasGrapple = pm.HasGrapple;
            hasSkateboard = pm.HasSkateboard;
            hasDartGun = pm.HasDartGun;
            hasMagicWand = pm.HasMagicWand;

            Debug.Log($"InventorySystem: 从ProgressionManager获取解锁状态 - 抓钩:{hasGrapple}, 滑板:{hasSkateboard}, 麻醉枪:{hasDartGun}, 魔法棒:{hasMagicWand}");
        }
        else
        {
            Debug.LogWarning("InventorySystem: 无法获取ProgressionManager，尝试从PlayerPrefs加载解锁状态");

            // 如果找不到ProgressionManager，尝试从PlayerPrefs读取保存的状态
            if (PlayerPrefs.HasKey("HasGrapple"))
            {
                hasGrapple = PlayerPrefs.GetInt("HasGrapple") == 1;
                hasSkateboard = PlayerPrefs.GetInt("HasSkateboard") == 1;
                hasDartGun = PlayerPrefs.GetInt("HasDartGun") == 1;
                hasMagicWand = PlayerPrefs.GetInt("HasMagicWand") == 1;

                Debug.Log($"InventorySystem: 从PlayerPrefs恢复解锁状态 - 抓钩:{hasGrapple}, 滑板:{hasSkateboard}, 麻醉枪:{hasDartGun}, 魔法棒:{hasMagicWand}");
            }
        }

        return new[]
        {
            true,              // 0 相机
            true,              // 1 食物
            hasGrapple,        // 2 抓钩
            hasSkateboard,     // 3 滑板
            hasDartGun,        // 4 麻醉枪
            hasMagicWand       // 5 魔法棒
        };
    }

    // 添加一个安全的方法来获取ProgressionManager实例
    private ProgressionManager FindProgressionManager()
    {
        try
        {
            // 首先尝试通过静态Instance获取
            var pm = ProgressionManager.Instance;

            // 如果失败，尝试在场景中查找
            if (pm == null)
            {
                pm = FindObjectOfType<ProgressionManager>();
                if (pm != null)
                {
                    Debug.Log("InventorySystem: 在场景中找到ProgressionManager");
                }
            }

            return pm;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InventorySystem: 查找ProgressionManager时出错: {e.Message}");
            return null;
        }
    }
}