// Assets/Scripts/Systems/ProgressionManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 进度与解锁中心，增加 Inspector 手动解锁道具开关：
/// • 在 Inspector 勾上“ManualUnlockX”，即可在启动时直接解锁对应道具。  
/// • 其它逻辑与之前完全一致：拍照解锁、桥梁解锁、事件广播、UI 更新。
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Bridge Unlock")]
    public int savannaThreshold = 12;
    public int jungleThreshold = 20;
    public GameObject savannaBridge;
    public GameObject jungleBridge;

    [Header("Item Unlock (ScriptableObjects)")]
    public BaseItem grappleItem;      // 4 种动物
    public BaseItem skateboardItem;   // 8 种动物
    public BaseItem dartGunItem;      // 12 种动物
    public BaseItem magicWandItem;    // 50 ★

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

    const int MagicWandStarThreshold = 50;

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

        OnAnimalStarUpdated?.Invoke(animalKey, clamped);

        CheckBridgeUnlock();
        if (isNew) CheckItemUnlocks();
        CheckMagicWandUnlock();
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
        if (!HasGrapple && UniqueAnimals >= 4) UnlockGrapple();
        if (!HasSkateboard && UniqueAnimals >= 8) UnlockSkateboard();
        if (!HasDartGun && UniqueAnimals >= 12) UnlockDartGun();
    }

    void UnlockGrapple()
    {
        HasGrapple = true;
        InventoryCycler.RegisterItem(grappleItem);
        ShowPopup("已解锁抓钩！按 I 装备");
    }

    void UnlockSkateboard()
    {
        HasSkateboard = true;
        InventoryCycler.RegisterItem(skateboardItem);
        ShowPopup("已解锁滑板！按 I 装备");
    }

    void UnlockDartGun()
    {
        HasDartGun = true;
        InventoryCycler.RegisterItem(dartGunItem);
        ShowPopup("已解锁麻醉枪！");
    }

    void CheckMagicWandUnlock()
    {
        if (HasMagicWand || TotalStars < MagicWandStarThreshold)
            return;
        UnlockMagicWand();
    }

    void UnlockMagicWand()
    {
        HasMagicWand = true;
        InventoryCycler.RegisterItem(magicWandItem);
        ShowPopup($"累计 {MagicWandStarThreshold} ★，魔法棒已解锁！");
    }

    void ShowPopup(string msg)
    {
        if (popupPrefab == null) Debug.Log(msg);
        else Instantiate(popupPrefab).Show(msg);
    }
}
