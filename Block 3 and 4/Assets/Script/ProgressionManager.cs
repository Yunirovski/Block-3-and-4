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
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 手动解锁
        if (ManualUnlockGrapple && !HasGrapple) UnlockGrapple();
        if (ManualUnlockSkateboard && !HasSkateboard) UnlockSkateboard();
        if (ManualUnlockDartGun && !HasDartGun) UnlockDartGun();
        if (ManualUnlockMagicWand && !HasMagicWand) UnlockMagicWand();
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
        HasGrapple = true;
        InventoryCycler.RegisterItem(grappleItem);
        ShowPopup($"已收集 {grappleStarsNeeded} ★，解锁抓钩！按 I 装备");
    }

    void UnlockSkateboard()
    {
        HasSkateboard = true;
        InventoryCycler.RegisterItem(skateboardItem);
        ShowPopup($"已收集 {skateboardStarsNeeded} ★，解锁滑板！按 I 装备");
    }

    void UnlockDartGun()
    {
        HasDartGun = true;
        InventoryCycler.RegisterItem(dartGunItem);
        ShowPopup($"已收集 {dartGunStarsNeeded} ★，解锁麻醉枪！按 I 装备");
    }

    void UnlockMagicWand()
    {
        HasMagicWand = true;
        InventoryCycler.RegisterItem(magicWandItem);
        ShowPopup($"已收集 {magicWandStarsNeeded} ★，魔法棒已解锁！按 I 装备");
    }

    void ShowPopup(string msg)
    {
        if (popupPrefab == null) Debug.Log(msg);
        else Instantiate(popupPrefab).Show(msg);
    }
}