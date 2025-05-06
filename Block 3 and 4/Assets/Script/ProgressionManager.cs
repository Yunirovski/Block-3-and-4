using UnityEngine;
using System.Collections.Generic;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    /* ---------- 区域桥梁 ---------- */
    public int savannaThreshold = 12;            // 保持
    public int jungleThreshold = 20;            // 保持
    public GameObject savannaBridge;
    public GameObject jungleBridge;

    /* ---------- 道具 ScriptableObject ---------- */
    public BaseItem grappleItem;      // 4 种
    public BaseItem skateboardItem;   // 8 种
    public BaseItem dartGunItem;      // 12 种
    public BaseItem magicWandItem;    // 50 ★

    /* ---------- 弹窗 (可选) ---------- */
    public PopupController popupPrefab;

    /* ---------- 内部数据 ---------- */
    readonly Dictionary<string, int> bestStars = new();
    public int TotalStars { get; private set; }
    public int UniqueAnimals { get; private set; }

    /* 道具解锁布尔 */
    public bool HasGrapple, HasSkateboard, HasDartGun, HasMagicWand;

    /* 50★ 解锁魔法棒 */
    const int MagicWandStarThreshold = 50;

    /* ===================================================================== */
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); }
    }

    /* ---------------- 注册星星 ---------------- */
    public void RegisterStars(string animalKey, int stars, bool isEasterEgg)
    {
        if (string.IsNullOrEmpty(animalKey) || stars <= 0) return;

        int cap = isEasterEgg ? 5 : 4;
        int clamped = Mathf.Clamp(stars, 1, cap);
        bool newSpec = false;

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
            newSpec = true;
        }

        CheckBridgeUnlock();
        if (newSpec) CheckItemUnlocks();
        CheckWandUnlock();                  // 每次都检测魔法棒
    }

    /* ---------------- 桥梁解锁 ---------------- */
    void CheckBridgeUnlock()
    {
        if (savannaBridge && !savannaBridge.activeSelf &&
            TotalStars >= savannaThreshold)
        {
            savannaBridge.SetActive(true);
            ShowPopup($"已获得 {savannaThreshold} ★！热带草原开放");
        }

        if (jungleBridge && !jungleBridge.activeSelf &&
            TotalStars >= jungleThreshold)
        {
            jungleBridge.SetActive(true);
            ShowPopup($"已获得 {jungleThreshold} ★！丛林开放");
        }
    }

    /* ---------------- 道具解锁 ---------------- */
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
        ShowPopup("滑板已解锁！按 I 装备后用 Q 上/下板");
    }
    void UnlockDartGun()
    {
        HasDartGun = true;
        InventoryCycler.RegisterItem(dartGunItem);
        ShowPopup("麻醉枪已解锁！按 I 装备");
    }

    void CheckWandUnlock()
    {
        if (HasMagicWand || TotalStars < MagicWandStarThreshold) return;

        HasMagicWand = true;
        InventoryCycler.RegisterItem(magicWandItem);
        ShowPopup($"恭喜！累计 {MagicWandStarThreshold} ★，魔法棒已解锁");
    }

    /* ---------------- 弹窗 ---------------- */
    void ShowPopup(string msg)
    {
        if (popupPrefab == null) { Debug.Log(msg); return; }
        Instantiate(popupPrefab).Show(msg);
    }
}
