// Assets/Scripts/Systems/ProgressionManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 进度与解锁中心，使用星星总量解锁道具：
/// • 在 Inspector 设置各道具所需星星数量
/// • 在 Inspector 勾上"ManualUnlockX"，即可在启动时直接解锁对应道具
/// • 通过拍照收集星星，自动解锁对应道具
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    private static ProgressionManager _instance;
    public static ProgressionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ProgressionManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("ProgressionManager");
                    _instance = go.AddComponent<ProgressionManager>();
                    go.AddComponent<SurvivalComponent>();
                    Debug.Log("ProgressionManager: 自动创建实例");
                }
            }
            return _instance;
        }
    }

    [Header("Bridge Unlock")]
    public int savannaThreshold = 12;
    public int jungleThreshold = 20;
    public GameObject savannaBridge;
    public GameObject jungleBridge;

    [Header("Item Unlock Thresholds")]
    [Tooltip("解锁抓钩所需的星星数")]
    public int grappleStarsNeeded = 8;
    [Tooltip("解锁滑板所需的星星数")]
    public int skateboardStarsNeeded = 15;
    [Tooltip("解锁麻醉枪所需的星星数")]
    public int dartGunStarsNeeded = 25;
    [Tooltip("解锁魔法棒所需的星星数")]
    public int magicWandStarsNeeded = 50;

    [Header("Item Unlock (ScriptableObjects)")]
    public BaseItem grappleItem;      // 解锁需要 grappleStarsNeeded 星
    public BaseItem skateboardItem;   // 解锁需要 skateboardStarsNeeded 星
    public BaseItem dartGunItem;      // 解锁需要 dartGunStarsNeeded 星
    public BaseItem magicWandItem;    // 解锁需要 magicWandStarsNeeded 星

    [Header("Manual Unlock Flags (Inspector)")]
    [Tooltip("勾选后启动时直接解锁抓钩")]
    public bool ManualUnlockGrapple = false;
    [Tooltip("勾选后启动时直接解锁滑板")]
    public bool ManualUnlockSkateboard = false;
    [Tooltip("勾选后启动时直接解锁麻醉枪")]
    public bool ManualUnlockDartGun = false;
    [Tooltip("勾选后启动时直接解锁魔法棒")]
    public bool ManualUnlockMagicWand = false;

    [Header("Optional Popup Prefab")]
    public PopupController popupPrefab;

    // 备份静态数据，防止实例销毁时丢失
    private static Dictionary<string, int> _savedBestStars = new Dictionary<string, int>();
    private static int _savedTotalStars = 0;
    private static int _savedUniqueAnimals = 0;
    private static bool _savedHasGrapple = false;
    private static bool _savedHasSkateboard = false;
    private static bool _savedHasDartGun = false;
    private static bool _savedHasMagicWand = false;

    // —— 内部进度数据 —— //
    readonly Dictionary<string, int> bestStars = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> BestStars => bestStars;

    public int TotalStars { get; private set; }
    public int UniqueAnimals { get; private set; }

    public bool HasGrapple { get; private set; }
    public bool HasSkateboard { get; private set; }
    public bool HasDartGun { get; private set; }
    public bool HasMagicWand { get; private set; }

    /// <summary>动物星级更新事件 (animalKey, newStars)</summary>
    public event Action<string, int> OnAnimalStarUpdated;

    void Awake()
    {
        Debug.Log($"ProgressionManager: Awake被调用 - {gameObject.name}");

        if (_instance != null && _instance != this)
        {
            Debug.Log($"ProgressionManager: 发现重复实例，禁用此组件 {gameObject.name}");
            this.enabled = false;
            return;
        }

        _instance = this;

        // 确保有效性
        if (GetComponent<SurvivalComponent>() == null)
        {
            gameObject.AddComponent<SurvivalComponent>();
        }

        DontDestroyOnLoad(gameObject);
        Debug.Log("ProgressionManager: 单例实例已初始化");

        // 加载已保存的状态
        RestoreSavedData();
    }

    private void Start()
    {
        Debug.Log("ProgressionManager: Start被调用，准备进行手动解锁检查");
        // 延迟进行手动解锁，确保其他系统已初始化
        Invoke("CheckManualUnlock", 0.5f);
    }

    // 从静态变量恢复数据
    private void RestoreSavedData()
    {
        // 先从静态变量恢复数据
        if (_savedBestStars.Count > 0)
        {
            bestStars.Clear();
            foreach (var pair in _savedBestStars)
            {
                bestStars[pair.Key] = pair.Value;
            }

            TotalStars = _savedTotalStars;
            UniqueAnimals = _savedUniqueAnimals;
            HasGrapple = _savedHasGrapple;
            HasSkateboard = _savedHasSkateboard;
            HasDartGun = _savedHasDartGun;
            HasMagicWand = _savedHasMagicWand;

            Debug.Log($"ProgressionManager: 从静态变量恢复数据 - 星星总数:{TotalStars}");
        }
        // 如果静态变量没有数据，尝试从PlayerPrefs加载
        else
        {
            LoadSavedStates();
        }
    }

    // 将当前数据保存到静态变量
    private void SaveCurrentData()
    {
        _savedBestStars = new Dictionary<string, int>(bestStars);
        _savedTotalStars = TotalStars;
        _savedUniqueAnimals = UniqueAnimals;
        _savedHasGrapple = HasGrapple;
        _savedHasSkateboard = HasSkateboard;
        _savedHasDartGun = HasDartGun;
        _savedHasMagicWand = HasMagicWand;

        Debug.Log($"ProgressionManager: 数据已保存到静态变量 - 星星总数:{TotalStars}");

        // 同时保存解锁状态到PlayerPrefs
        SaveUnlockStatesToPrefs();
    }

    // 添加新方法来保存解锁状态到PlayerPrefs
    private void SaveUnlockStatesToPrefs()
    {
        PlayerPrefs.SetInt("HasGrapple", HasGrapple ? 1 : 0);
        PlayerPrefs.SetInt("HasSkateboard", HasSkateboard ? 1 : 0);
        PlayerPrefs.SetInt("HasDartGun", HasDartGun ? 1 : 0);
        PlayerPrefs.SetInt("HasMagicWand", HasMagicWand ? 1 : 0);
        PlayerPrefs.SetInt("TotalStars", TotalStars);
        PlayerPrefs.Save();

        Debug.Log($"ProgressionManager: 解锁状态已保存到PlayerPrefs - 抓钩:{HasGrapple}, 滑板:{HasSkateboard}, 麻醉枪:{HasDartGun}, 魔法棒:{HasMagicWand}");
    }

    // 加载已保存的状态
    private void LoadSavedStates()
    {
        if (PlayerPrefs.HasKey("HasGrapple"))
        {
            HasGrapple = PlayerPrefs.GetInt("HasGrapple") == 1;
            HasSkateboard = PlayerPrefs.GetInt("HasSkateboard") == 1;
            HasDartGun = PlayerPrefs.GetInt("HasDartGun") == 1;
            HasMagicWand = PlayerPrefs.GetInt("HasMagicWand") == 1;
            TotalStars = PlayerPrefs.GetInt("TotalStars", 0);

            Debug.Log($"ProgressionManager: 从PlayerPrefs加载解锁状态 - 抓钩:{HasGrapple}, 滑板:{HasSkateboard}, 麻醉枪:{HasDartGun}, 魔法棒:{HasMagicWand}, 星星:{TotalStars}");
        }
    }

    private void CheckManualUnlock()
    {
        Debug.Log("ProgressionManager: 开始检查手动解锁标志");

        // 手动解锁逻辑移到这里，确保所有相关系统已初始化
        if (ManualUnlockGrapple)
        {
            Debug.Log("ProgressionManager: 检测到抓钩手动解锁标志为true");
            if (!HasGrapple) UnlockGrapple();
        }

        if (ManualUnlockSkateboard)
        {
            Debug.Log("ProgressionManager: 检测到滑板手动解锁标志为true");
            if (!HasSkateboard) UnlockSkateboard();
        }

        if (ManualUnlockDartGun)
        {
            Debug.Log("ProgressionManager: 检测到麻醉枪手动解锁标志为true");
            if (!HasDartGun) UnlockDartGun();
        }

        if (ManualUnlockMagicWand)
        {
            Debug.Log("ProgressionManager: 检测到魔法棒手动解锁标志为true");
            if (!HasMagicWand) UnlockMagicWand();
        }

        // 检查道具项是否有效，如果无效则输出错误日志
        if (ManualUnlockGrapple && grappleItem == null) Debug.LogError("无法解锁抓钩: grappleItem为null");
        if (ManualUnlockSkateboard && skateboardItem == null) Debug.LogError("无法解锁滑板: skateboardItem为null");
        if (ManualUnlockDartGun && dartGunItem == null) Debug.LogError("无法解锁麻醉枪: dartGunItem为null");
        if (ManualUnlockMagicWand && magicWandItem == null) Debug.LogError("无法解锁魔法棒: magicWandItem为null");

        Debug.Log("ProgressionManager: 手动解锁检查完成");

        // 保存当前状态
        SaveCurrentData();
    }

    /// <summary>
    /// 拍照后汇报某物种的新星级，由 AnimalEvent.TriggerEvent 调用。
    /// </summary>
    public void RegisterStars(string animalKey, int stars, bool isEasterEgg)
    {
        if (string.IsNullOrEmpty(animalKey) || stars <= 0) return;

        int cap = isEasterEgg ? 5 : 4;
        int clamped = Mathf.Clamp(stars, 1, cap);
        bool isNew = false;

        if (bestStars.TryGetValue(animalKey, out int prev))
        {
            if (clamped <= prev) return;
            bestStars[animalKey] = clamped;
            TotalStars += clamped - prev;
        }
        else
        {
            bestStars[animalKey] = clamped;
            TotalStars += clamped;
            UniqueAnimals++;
            isNew = true;
        }

        // 通知UI更新星星数量
        OnAnimalStarUpdated?.Invoke(animalKey, clamped);
        var scoreManager = ScoreManager.Instance;
        if (scoreManager != null)
            scoreManager.SetStars(TotalStars);

        // 检查桥梁和道具解锁
        CheckBridgeUnlock();
        CheckItemUnlocks();

        // 保存当前状态
        SaveCurrentData();
    }

    void CheckBridgeUnlock()
    {
        if (savannaBridge && !savannaBridge.activeSelf &&
            TotalStars >= savannaThreshold)
        {
            savannaBridge.SetActive(true);
            ShowPopup($"累计 {savannaThreshold} ★，热带草原开放！");
        }
        if (jungleBridge && !jungleBridge.activeSelf &&
            TotalStars >= jungleThreshold)
        {
            jungleBridge.SetActive(true);
            ShowPopup($"累计 {jungleThreshold} ★，丛林开放！");
        }
    }

    void CheckItemUnlocks()
    {
        // 基于星星总数检查道具解锁
        if (!HasGrapple && TotalStars >= grappleStarsNeeded) UnlockGrapple();
        if (!HasSkateboard && TotalStars >= skateboardStarsNeeded) UnlockSkateboard();
        if (!HasDartGun && TotalStars >= dartGunStarsNeeded) UnlockDartGun();
        if (!HasMagicWand && TotalStars >= magicWandStarsNeeded) UnlockMagicWand();
    }

    void UnlockGrapple()
    {
        if (grappleItem == null)
        {
            Debug.LogError("无法解锁抓钩: grappleItem为null");
            return;
        }

        HasGrapple = true;
        try
        {
            InventoryCycler.RegisterItem(grappleItem);
            Debug.Log("解锁抓钩成功: 已注册到InventoryCycler!");
        }
        catch (Exception e)
        {
            Debug.LogError($"注册抓钩到InventoryCycler失败: {e.Message}");
        }

        ShowPopup($"已收集 {grappleStarsNeeded} ★，解锁抓钩！按 I 装备");
        Debug.Log("解锁抓钩成功!");

        // 保存状态
        SaveCurrentData();
    }

    void UnlockSkateboard()
    {
        if (skateboardItem == null)
        {
            Debug.LogError("无法解锁滑板: skateboardItem为null");
            return;
        }

        HasSkateboard = true;
        try
        {
            InventoryCycler.RegisterItem(skateboardItem);
            Debug.Log("解锁滑板成功: 已注册到InventoryCycler!");
        }
        catch (Exception e)
        {
            Debug.LogError($"注册滑板到InventoryCycler失败: {e.Message}");
        }

        ShowPopup($"已收集 {skateboardStarsNeeded} ★，解锁滑板！按 I 装备");
        Debug.Log("解锁滑板成功!");

        // 保存状态
        SaveCurrentData();
    }

    void UnlockDartGun()
    {
        if (dartGunItem == null)
        {
            Debug.LogError("无法解锁麻醉枪: dartGunItem为null");
            return;
        }

        HasDartGun = true;
        try
        {
            InventoryCycler.RegisterItem(dartGunItem);
            Debug.Log("解锁麻醉枪成功: 已注册到InventoryCycler!");
        }
        catch (Exception e)
        {
            Debug.LogError($"注册麻醉枪到InventoryCycler失败: {e.Message}");
        }

        ShowPopup($"已收集 {dartGunStarsNeeded} ★，解锁麻醉枪！按 I 装备");
        Debug.Log("解锁麻醉枪成功!");

        // 保存状态
        SaveCurrentData();
    }

    void UnlockMagicWand()
    {
        if (magicWandItem == null)
        {
            Debug.LogError("无法解锁魔法棒: magicWandItem为null");
            return;
        }

        HasMagicWand = true;
        try
        {
            InventoryCycler.RegisterItem(magicWandItem);
            Debug.Log("解锁魔法棒成功: 已注册到InventoryCycler!");
        }
        catch (Exception e)
        {
            Debug.LogError($"注册魔法棒到InventoryCycler失败: {e.Message}");
        }

        ShowPopup($"已收集 {magicWandStarsNeeded} ★，魔法棒已解锁！按 I 装备");
        Debug.Log("解锁魔法棒成功!");

        // 保存状态
        SaveCurrentData();
    }

    void ShowPopup(string msg)
    {
        if (popupPrefab == null) Debug.Log(msg);
        else Instantiate(popupPrefab).Show(msg);
    }

    private void OnDisable()
    {
        Debug.Log($"ProgressionManager: OnDisable被调用 - {gameObject.name}");

        // 保存当前状态
        SaveCurrentData();
    }

    private void OnDestroy()
    {
        Debug.Log($"ProgressionManager: OnDestroy被调用 - {gameObject.name}");

        // 保存当前状态
        SaveCurrentData();

        // 只有当当前实例被销毁时才清除静态引用
        if (_instance == this)
        {
            Debug.Log("ProgressionManager: 单例实例被销毁，但静态数据已保存");
            _instance = null;
        }
    }

    // 添加OnApplicationQuit确保游戏退出时也保存数据
    private void OnApplicationQuit()
    {
        Debug.Log("ProgressionManager: 应用程序退出，保存数据");
        SaveCurrentData();
    }

    // 添加一个组件确保游戏对象不会被销毁
    public class SurvivalComponent : MonoBehaviour
    {
        private void Awake()
        {
            // 确保持久化
            DontDestroyOnLoad(gameObject);
            Debug.Log($"SurvivalComponent: Awake在 {gameObject.name}");
        }

        private void OnDestroy()
        {
            Debug.LogWarning($"SurvivalComponent: 检测到尝试销毁 {gameObject.name}，这可能导致ProgressionManager不可用");
        }
    }
}