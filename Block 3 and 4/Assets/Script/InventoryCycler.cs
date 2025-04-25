using System.Collections.Generic;

/// <summary>
/// ������3����ѭ������Ʒ�б���ʼע�롢��̬ע��/�Ƴ��������б���ȡ������
/// </summary>
public static class InventoryCycler
{
    // ���������ݵ�˽���б�
    private static readonly List<BaseItem> slot3List = new List<BaseItem>();

    /// <summary>
    /// ��ʼ��ʱ���ã�����վ��б��ٰѳ�ʼ��Ʒ���롣
    /// </summary>
    public static void InitWith(BaseItem initial)
    {
        slot3List.Clear();
        if (initial != null)
            slot3List.Add(initial);
    }

    /// <summary>
    /// ����װ������ã��������б�����ע�ᣨ���ظ���ӣ���
    /// </summary>
    public static void RegisterItem(BaseItem item)
    {
        if (item != null && !slot3List.Contains(item))
            slot3List.Add(item);
    }

    /// <summary>
    /// ��Ҫɾ��ĳ��װ��������������ʧЧʱ���ɵ��ô˷�����
    /// </summary>
    public static void RemoveItem(BaseItem item)
    {
        if (item != null)
            slot3List.Remove(item);
    }

    /// <summary>
    /// InventorySystem �� Q/E �ֻ�ʱ���ã��ṩһ�����������б���ֹ�ⲿ��ġ�
    /// </summary>
    public static List<BaseItem> GetSlot3List()
    {
        return new List<BaseItem>(slot3List);
    }
}
