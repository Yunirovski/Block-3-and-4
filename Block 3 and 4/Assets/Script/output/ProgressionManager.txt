// Assets/Scripts/Systems/ProgressionManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 进度与解锁中心，使用星星总量解锁道具：
/// • 在 Inspector 设置各道具所需星星数量
/// • 在 Inspector 勾上"ManualUnlockX"，即可在启动时直接解锁对应道具
/// • 通过拍照收集星星，自动解锁对应道具
/// • 支持进度持久化保存与加载
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

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

    [Header("Save Settings")]
    [Tooltip("是否启用自动保存")]
    public bool enableAutoSave = true;
    [Tooltip("自动保存间隔（秒）")]
    public float autoSaveInterval = 60f;
    [Tooltip("存档文件名")]
    public string saveFileName = "player_progress.json";

    // —— 内部进度数据 —— //
    private Dictionary<string, int> bestStars = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> BestStars => bestStars;

    public int TotalStars { get; private set; }
    public int UniqueAnimals { get; private set; }

    public bool HasGrapple { get; private set; }
    public bool HasSkateboard { get; private set; }
    public bool HasDartGun { get; private set; }
    public bool HasMagicWand { get; private set; }

    // 存档路径
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

    // 自动保存计时器
    private float autoSaveTimer = 0f;

    // 是否已初始化
    private bool isInitialized = false;

    /// <summary>动物星级更新事件 (animalKey, newStars)</summary>
    public event Action<string, int> OnAnimalStarUpdated;

    /// <summary>游戏进度保存完成事件</summary>
    public event Action OnProgressSaved;

    /// <summary>游戏进度加载完成事件</summary>
    public event Action OnProgressLoaded;

    [Serializable]
    private class SaveData
    {
        public Dictionary<string, int> bestStars = new Dictionary<string, int>();
        public int totalStars;
        public int uniqueAnimals;
        public bool hasGrapple;
        public bool hasSkateboard;
        public bool hasDartGun;
        public bool hasMagicWand;
        public bool savannaUnlocked;
        public bool jungleUnlocked;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 加载游戏进度
        LoadProgress();

        // 手动解锁（仅在编辑器中起效）
        if (Application.isEditor)
        {
            if (ManualUnlockGrapple && !HasGrapple) UnlockGrapple();
            if (ManualUnlockSkateboard && !HasSkateboard) UnlockSkateboard();
            if (ManualUnlockDartGun && !HasDartGun) UnlockDartGun();
            if (ManualUnlockMagicWand && !HasMagicWand) UnlockMagicWand();
        }

        // 检查区域解锁状态
        if (savannaBridge != null)
            savannaBridge.SetActive(TotalStars >= savannaThreshold);
        if (jungleBridge != null)
            jungleBridge.SetActive(TotalStars >= jungleThreshold);

        // 更新ScoreManager
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SetStars(TotalStars);

        // 标记初始化完成
        isInitialized = true;
    }

    void Update()
    {
        // 处理自动保存
        if (enableAutoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveProgress();
                autoSaveTimer = 0f;
            }
        }
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

        // 获得新星星后保存进度
        if (isInitialized && enableAutoSave)
        {
            SaveProgress();
        }
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
        HasGrapple = true;
        InventoryCycler.RegisterItem(grappleItem);
        ShowPopup($"已收集 {grappleStarsNeeded} ★，解锁抓钩！按 I 装备");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockSkateboard()
    {
        HasSkateboard = true;
        InventoryCycler.RegisterItem(skateboardItem);
        ShowPopup($"已收集 {skateboardStarsNeeded} ★，解锁滑板！按 I 装备");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockDartGun()
    {
        HasDartGun = true;
        InventoryCycler.RegisterItem(dartGunItem);
        ShowPopup($"已收集 {dartGunStarsNeeded} ★，解锁麻醉枪！按 I 装备");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockMagicWand()
    {
        HasMagicWand = true;
        InventoryCycler.RegisterItem(magicWandItem);
        ShowPopup($"已收集 {magicWandStarsNeeded} ★，魔法棒已解锁！按 I 装备");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void ShowPopup(string msg)
    {
        // 使用UIManager显示弹窗
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPopup(msg);
        }
        else
        {
            Debug.Log($"ProgressionManager: {msg}");
        }
    }

    /// <summary>
    /// 保存游戏进度
    /// </summary>
    public void SaveProgress()
    {
        try
        {
            // 创建存档数据
            SaveData saveData = new SaveData
            {
                bestStars = new Dictionary<string, int>(bestStars),
                totalStars = TotalStars,
                uniqueAnimals = UniqueAnimals,
                hasGrapple = HasGrapple,
                hasSkateboard = HasSkateboard,
                hasDartGun = HasDartGun,
                hasMagicWand = HasMagicWand,
                savannaUnlocked = savannaBridge != null && savannaBridge.activeSelf,
                jungleUnlocked = jungleBridge != null && jungleBridge.activeSelf
            };

            // 序列化为JSON
            string jsonData = JsonUtility.ToJson(saveData, true);

            // 写入文件
            File.WriteAllText(SaveFilePath, jsonData);

            Debug.Log($"游戏进度已保存到: {SaveFilePath}");
            OnProgressSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"保存游戏进度失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载游戏进度
    /// </summary>
    public void LoadProgress()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("没有找到存档文件，使用默认设置");
            InitializeDefaultProgress();
            return;
        }

        try
        {
            // 读取文件
            string jsonData = File.ReadAllText(SaveFilePath);

            // 反序列化
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

            // 应用存档数据
            bestStars = new Dictionary<string, int>(saveData.bestStars);
            TotalStars = saveData.totalStars;
            UniqueAnimals = saveData.uniqueAnimals;
            HasGrapple = saveData.hasGrapple;
            HasSkateboard = saveData.hasSkateboard;
            HasDartGun = saveData.hasDartGun;
            HasMagicWand = saveData.hasMagicWand;

            // 重新注册已解锁的物品
            if (HasGrapple && grappleItem != null)
                InventoryCycler.RegisterItem(grappleItem);
            if (HasSkateboard && skateboardItem != null)
                InventoryCycler.RegisterItem(skateboardItem);
            if (HasDartGun && dartGunItem != null)
                InventoryCycler.RegisterItem(dartGunItem);
            if (HasMagicWand && magicWandItem != null)
                InventoryCycler.RegisterItem(magicWandItem);

            Debug.Log($"成功加载游戏进度: {TotalStars}颗星星, {UniqueAnimals}种动物");
            OnProgressLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"加载游戏进度失败: {e.Message}");
            InitializeDefaultProgress();
        }
    }

    /// <summary>
    /// 初始化默认进度
    /// </summary>
    private void InitializeDefaultProgress()
    {
        bestStars = new Dictionary<string, int>();
        TotalStars = 0;
        UniqueAnimals = 0;
        HasGrapple = false;
        HasSkateboard = false;
        HasDartGun = false;
        HasMagicWand = false;
    }

    /// <summary>
    /// 重置所有进度（通常用于"新游戏"）
    /// </summary>
    public void ResetProgress()
    {
        // 确认对话框
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowConfirmation(
                "重置进度",
                "确定要重置所有游戏进度吗？这将删除所有收集的星星和解锁的物品。此操作不可撤销！",
                () => {
                    // 确认重置
                    PerformProgressReset();
                },
                null // 取消不执行任何操作
            );
        }
        else
        {
            PerformProgressReset();
        }
    }

    /// <summary>
    /// 执行进度重置
    /// </summary>
    private void PerformProgressReset()
    {
        // 重置内部数据
        InitializeDefaultProgress();

        // 更新UI
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SetStars(0);

        // 禁用桥梁
        if (savannaBridge != null)
            savannaBridge.SetActive(false);
        if (jungleBridge != null)
            jungleBridge.SetActive(false);

        // 保存重置后的状态
        SaveProgress();

        // 显示提示
        ShowPopup("游戏进度已重置");
    }

    /// <summary>
    /// 在游戏退出前保存进度
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    /// <summary>
    /// 在游戏暂停时保存进度
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveProgress();
        }
    }
}