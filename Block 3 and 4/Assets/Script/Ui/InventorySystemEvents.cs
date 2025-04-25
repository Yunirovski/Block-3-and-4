using System;

public static class InventorySystemEvents
{
    // 广播 (装备, 冷却时长)
    public static Action<BaseItem, float> OnItemCooldownStart;
}
