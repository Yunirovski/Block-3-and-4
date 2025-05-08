// Assets/Scripts/ProgressionManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 管理玩家的拍照进度、星星累积与道具/区域解锁。
/// 现在新增：可对外查询、监听每个动物的最高星级。</summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    /* ---------- 区域桥梁 ---------- */
    [Header("区域桥梁")]
    public int savannaThreshold = 12;
    public int jungleThreshold = 20;
    public GameObject savannaBridge;
    public GameObject jungleBridge;

    /* ---------- 道具 ScriptableObject ---------- */
    [Header("解锁道具")]
    public BaseItem grappleItem;
    public BaseItem skateboardItem;
    public BaseItem dartGunItem;
    public BaseItem magicWandItem; // 50★ 解锁

    /* ---------- 弹窗 (可选) ---------- */
    [Header("弹窗预制体（可选）")]
    public PopupController popupPrefab;

    /* ---------- 内部数据 ---------- */
    // 保存每个动物的最高星级
    readonly Dictionary<string, int> bestStars = new();

    /// <summary>所有动物的最高星级（只读字典）。Key=animalName, Value=最高星数</summary>
    public IReadOnlyDictionary<string, int> BestStars => bestStars;

    /// <summary>注册成功时触发。参数：(animalName, newBestStars)。</summary>
    public event Action<string, int> OnAnimalStarUpdated;

    public int TotalStars { get; private set; }  // Σ 所有最高星
    public int UniqueAnimals { get; private set; }  // 拍到过至少1★的物种数

    /* ---------- 解锁状态 ---------- */
    public bool HasGrapple, HasSkateboard, HasDartGun, HasMagicWand;
    const int MagicWandStarThreshold = 50;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>
    /// 拍照后调用，更新该物种的最高星级，并触发解锁检查和事件通知。</summary>
    /// <param name="animalKey">动物唯一键</param>
    /// <param name="stars">本次照片星级</param>
    /// <param name="isEasterEgg">彩蛋动物可达5★</param>
    public void RegisterStars(string animalKey, int stars, bool isEasterEgg)
    {
        if (string.IsNullOrEmpty(animalKey) || stars <= 0) return;

        int cap = isEasterEgg ? 5 : 4;
        int clamped = Mathf.Clamp(stars, 1, cap);
        bool isNewSpecies = false;

        if (bestStars.TryGetValue(animalKey, out int prev))
        {
            if (clamped <= prev) return;            // 没有超过历史最高
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

        // 通知 UI 或其他系统：该 animalKey 的最高星更新到 clamped
        OnAnimalStarUpdated?.Invoke(animalKey, clamped);

        // 区域/道具解锁
        CheckBridgeUnlock();
        if (isNewSpecies) CheckItemUnlocks();
        CheckWandUnlock();
    }

    #region 桥梁解锁
    void CheckBridgeUnlock()
    {
        if (savannaBridge && !savannaBridge.activeSelf && TotalStars >= savannaThreshold)
        {
            savannaBridge.SetActive(true);
            ShowPopup($"已获得 {savannaThreshold} ★！热带草原开放");
        }
        if (jungleBridge && !jungleBridge.activeSelf && TotalStars >= jungleThreshold)
        {
            jungleBridge.SetActive(true);
            ShowPopup($"已获得 {jungleThreshold} ★！丛林开放");
        }
    }
    #endregion

    #region 道具解锁
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
    #endregion

    #region 魔法棒解锁
    void CheckWandUnlock()
    {
        if (HasMagicWand || TotalStars < MagicWandStarThreshold) return;
        HasMagicWand = true;
        InventoryCycler.RegisterItem(magicWandItem);
        ShowPopup($"恭喜！累计 {MagicWandStarThreshold} ★，魔法棒已解锁");
    }
    #endregion

    #region 弹窗
    void ShowPopup(string msg)
    {
        if (popupPrefab == null) { Debug.Log(msg); return; }
        Instantiate(popupPrefab).Show(msg);
    }
    #endregion
}
