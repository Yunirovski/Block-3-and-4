using System.Collections.Generic;

/// <summary>
/// 管理“槽3”的可循环物品列表：Init/Clear/Register/Remove/GetCopy
/// </summary>
public static class InventoryCycler
{
    private static readonly List<BaseItem> slot3List = new List<BaseItem>();

    // 场景启动或切区时调用：清空并注入初始
    public static void InitWith(BaseItem initial)
    {
        slot3List.Clear();
        if (initial != null)
            slot3List.Add(initial);
    }

    // 购买后调用：注册装备
    public static void RegisterItem(BaseItem item)
    {
        if (item != null && !slot3List.Contains(item))
            slot3List.Add(item);
    }

    // 卖掉或回收时调用：移除
    public static void RemoveItem(BaseItem item)
    {
        if (item != null)
            slot3List.Remove(item);
    }

    // InventorySystem 用，返回拷贝防误改
    public static List<BaseItem> GetSlot3List()
        => new List<BaseItem>(slot3List);
}
