using System.Collections.Generic;

/// <summary>
/// 管理“槽3”可循环的物品列表：初始注入、动态注册/移除、重置列表、获取副本。
/// </summary>
public static class InventoryCycler
{
    // 真正存数据的私有列表
    private static readonly List<BaseItem> slot3List = new List<BaseItem>();

    /// <summary>
    /// 初始化时调用：先清空旧列表，再把初始物品加入。
    /// </summary>
    public static void InitWith(BaseItem initial)
    {
        slot3List.Clear();
        if (initial != null)
            slot3List.Add(initial);
    }

    /// <summary>
    /// 购买装备后调用：若不在列表中则注册（不重复添加）。
    /// </summary>
    public static void RegisterItem(BaseItem item)
    {
        if (item != null && !slot3List.Contains(item))
            slot3List.Add(item);
    }

    /// <summary>
    /// 若要删除某件装备（比如卖掉、失效时）可调用此方法。
    /// </summary>
    public static void RemoveItem(BaseItem item)
    {
        if (item != null)
            slot3List.Remove(item);
    }

    /// <summary>
    /// InventorySystem 在 Q/E 轮换时调用：提供一个“拷贝”列表，防止外部误改。
    /// </summary>
    public static List<BaseItem> GetSlot3List()
    {
        return new List<BaseItem>(slot3List);
    }
}
