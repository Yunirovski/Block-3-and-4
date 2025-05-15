using System;

/// <summary>
/// ���ϵͳ�¼����ģ��ṩ����ͨ�ŵ��¼�
/// </summary>
public static class InventorySystemEvents
{
    /// <summary>
    /// ��Ʒ��ʼ��ȴʱ���ã�֪ͨUI��ʾ��ȴЧ��
    /// </summary>
    /// <param name="item">������ȴ����Ʒ</param>
    /// <param name="cooldownSeconds">��ȴʱ��(��)</param>
    public static Action<BaseItem, float> OnItemCooldownStart;

    static InventorySystemEvents()
    {
        // ������ȴ�¼���ת����UIManager
        OnItemCooldownStart += HandleCooldownStart;
    }

    // ������ȴ�¼�
    private static void HandleCooldownStart(BaseItem item, float duration)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartItemCooldown(item, duration);
        }
    }
}