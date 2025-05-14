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

    [Header("Optional Popup Prefab")]
    public PopupController popupPrefab;

    // —— 内部进度数据 —— //
    readonly Dictionary<string, int> bestStars = new();
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
        if (Instance != null && Instance != this)
        {
            Debug.Log($"ProgressionManager: 发现重复实例，禁用此组件 {gameObject.name}");
            this.enabled = false;
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("ProgressionManager: 单例实例已初始化");
    }

    private void Start()
    {
        Debug.Log("ProgressionManager: Start被调用，准备进行手动解锁检查");
        // 延迟进行手动解锁，确保其他系统已初始化
        Invoke("CheckManualUnlock", 0.5f);
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
        InventoryCycler.RegisterItem(grappleItem);
        ShowPopup($"已收集 {grappleStarsNeeded} ★，解锁抓钩！按 I 装备");
        Debug.Log("解锁抓钩成功!");
    }

    void UnlockSkateboard()
    {
        if (skateboardItem == null)
        {
            Debug.LogError("无法解锁滑板: skateboardItem为null");
            return;
        }

        HasSkateboard = true;
        InventoryCycler.RegisterItem(skateboardItem);
        ShowPopup($"已收集 {skateboardStarsNeeded} ★，解锁滑板！按 I 装备");
        Debug.Log("解锁滑板成功!");
    }

    void UnlockDartGun()
    {
        if (dartGunItem == null)
        {
            Debug.LogError("无法解锁麻醉枪: dartGunItem为null");
            return;
        }

        HasDartGun = true;
        InventoryCycler.RegisterItem(dartGunItem);
        ShowPopup($"已收集 {dartGunStarsNeeded} ★，解锁麻醉枪！按 I 装备");
        Debug.Log("解锁麻醉枪成功!");
    }

    void UnlockMagicWand()
    {
        if (magicWandItem == null)
        {
            Debug.LogError("无法解锁魔法棒: magicWandItem为null");
            return;
        }

        HasMagicWand = true;
        InventoryCycler.RegisterItem(magicWandItem);
        ShowPopup($"已收集 {magicWandStarsNeeded} ★，魔法棒已解锁！按 I 装备");
        Debug.Log("解锁魔法棒成功!");
    }

    void ShowPopup(string msg)
    {
        if (popupPrefab == null) Debug.Log(msg);
        else Instantiate(popupPrefab).Show(msg);
    }

    private void OnDisable()
    {
        // 只有在组件被禁用且为当前实例时，清除静态引用
        if (Instance == this && !this.enabled)
        {
            Debug.Log("ProgressionManager: 单例实例被禁用");
            // 不清除静态引用，保持实例
            // Instance = null; 
        }
    }
}