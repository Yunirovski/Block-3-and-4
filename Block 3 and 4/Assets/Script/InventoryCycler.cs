using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 暂时的占位类：维护一个“槽 3 可循环物品”列表，
/// 目前仅返回游戏开始时已有的那一个 FoodItem，
/// 以后会由商店系统动态把抓钩 / 滑板等注册进来。
/// </summary>
public static class InventoryCycler
{
    // 内部静态列表
    private static readonly List<BaseItem> slot3List = new List<BaseItem>();

    /// <summary>在游戏启动时把初始物品注册进来。</summary>
    public static void InitWith(BaseItem initial)
    {
        if (initial != null && !slot3List.Contains(initial))
            slot3List.Add(initial);
    }

    /// <summary>商店购买新装备后调用，把装备加入循环。</summary>
    public static void RegisterItem(BaseItem item)
    {
        if (item != null && !slot3List.Contains(item))
            slot3List.Add(item);
    }

    /// <summary>返回当前可循环列表（供 InventorySystem 调用）。</summary>
    public static List<BaseItem> GetSlot3List() => slot3List;
}
