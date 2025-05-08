// Assets/Scripts/Systems/ProgressionManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 负责管理玩家拍照进度：
/// - 记录每种动物的最高星级  
/// - 计算累计星星 & 已拍物种数  
/// - 解锁桥梁与道具  
/// - 广播星级更新以供 UI 订阅  
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Bridge Unlock Thresholds")]
    [Tooltip("获得此数量星星后解锁热带草原桥梁")]
    public int savannaThreshold = 12;
    [Tooltip("获得此数量星星后解锁丛林桥梁")]
    public int jungleThreshold = 20;
    public GameObject savannaBridge;
    public GameObject jungleBridge;

    [Header("Item Unlock ScriptableObjects")]
    public BaseItem grappleItem;     // 4 种不同动物解锁
    public BaseItem skateboardItem;  // 8 种不同动物解锁
    public BaseItem dartGunItem;     // 12 种不同动物解锁
    public BaseItem magicWandItem;   // 累计 50★ 解锁

    [Header("Optional Popup Prefab")]
    public PopupController popupPrefab;

    // —— 内部数据 —— //
    readonly Dictionary<string, int> bestStars = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> BestStars => bestStars;

    public int TotalStars { get; private set; }
    public int UniqueAnimals { get; private set; }

    public bool HasGrapple { get; private set; }
    public bool HasSkateboard { get; private set; }
    public bool HasDartGun { get; private set; }
    public bool HasMagicWand { get; private set; }

    const int MagicWandStarThreshold = 50;

    /// <summary>
    /// 当某个物种的最高星级更新时广播 (animalKey, newStars)
    /// </summary>
    public event Action<string, int> OnAnimalStarUpdated;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>
    /// 拍照后汇报某物种的新星级。  
    /// </summary>
    /// <param name="animalKey">AnimalEvent.animalName</param>
    /// <param name="stars">最终星级 (1–4/5★)</param>
    /// <param name="isEasterEgg">是否彩蛋动物</param>
    public void RegisterStars(string animalKey, int stars, bool isEasterEgg)
    {
        if (string.IsNullOrEmpty(animalKey) || stars <= 0)
            return;

        int cap = isEasterEgg ? 5 : 4;
        int clamped = Mathf.Clamp(stars, 1, cap);
        bool isNewSpecies = false;

        if (bestStars.TryGetValue(animalKey, out int prev))
        {
            if (clamped <= prev)
                return; // 未突破
            bestStars[animalKey] = clamped;
            TotalStars += (clamped - prev);
        }
        else
        {
            bestStars[animalKey] = clamped;
            TotalStars += clamped;
            UniqueAnimals++;
            isNewSpecies = true;
        }

        // 广播给 UI
        OnAnimalStarUpdated?.Invoke(animalKey, clamped);

        // 检查解锁
        CheckBridgeUnlock();
        if (isNewSpecies) CheckItemUnlocks();
        CheckMagicWandUnlock();
    }

    void CheckBridgeUnlock()
    {
        if (savannaBridge && !savannaBridge.activeSelf && TotalStars >= savannaThreshold)
        {
            savannaBridge.SetActive(true);
            ShowPopup($"已获得 {savannaThreshold} ★，热带草原开放");
        }
        if (jungleBridge && !jungleBridge.activeSelf && TotalStars >= jungleThreshold)
        {
            jungleBridge.SetActive(true);
            ShowPopup($"已获得 {jungleThreshold} ★，丛林开放");
        }
    }

    void CheckItemUnlocks()
    {
        if (!HasGrapple && UniqueAnimals >= 4) UnlockGrapple();
        if (!HasSkateboard && UniqueAnimals >= 8) UnlockSkateboard();
        if (!HasDartGun && UniqueAnimals >= 12) UnlockDartGun();
    }

    void UnlockGrapple()
    {
        HasGrapple = true;
        InventoryCycler.RegisterItem(grappleItem);
        ShowPopup("抓钩已解锁！按 I 装备");
    }

    void UnlockSkateboard()
    {
        HasSkateboard = true;
        InventoryCycler.RegisterItem(skateboardItem);
        ShowPopup("滑板已解锁！按 I 装备");
    }

    void UnlockDartGun()
    {
        HasDartGun = true;
        InventoryCycler.RegisterItem(dartGunItem);
        ShowPopup("麻醉枪已解锁！按 I 装备");
    }

    void CheckMagicWandUnlock()
    {
        if (HasMagicWand || TotalStars < MagicWandStarThreshold)
            return;
        HasMagicWand = true;
        InventoryCycler.RegisterItem(magicWandItem);
        ShowPopup($"累计 {MagicWandStarThreshold} ★，魔法棒已解锁");
    }

    void ShowPopup(string msg)
    {
        if (popupPrefab == null)
            Debug.Log(msg);
        else
            Instantiate(popupPrefab).Show(msg);
    }
}
