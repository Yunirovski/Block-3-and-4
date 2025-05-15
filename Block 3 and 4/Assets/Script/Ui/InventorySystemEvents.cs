using System;

/// <summary>
/// 库存系统事件中心：提供跨类通信的事件
/// </summary>
public static class InventorySystemEvents
{
    /// <summary>
    /// 物品开始冷却时调用，通知UI显示冷却效果
    /// </summary>
    /// <param name="item">进入冷却的物品</param>
    /// <param name="cooldownSeconds">冷却时间(秒)</param>
    public static Action<BaseItem, float> OnItemCooldownStart;

    static InventorySystemEvents()
    {
        // 订阅冷却事件，转发到UIManager
        OnItemCooldownStart += HandleCooldownStart;
    }

    // 处理冷却事件
    private static void HandleCooldownStart(BaseItem item, float duration)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartItemCooldown(item, duration);
        }
    }
}