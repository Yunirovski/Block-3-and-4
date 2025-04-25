using System.Collections.Generic;

/// <summary>
/// ������3���Ŀ�ѭ����Ʒ�б�Init/Clear/Register/Remove/GetCopy
/// </summary>
public static class InventoryCycler
{
    private static readonly List<BaseItem> slot3List = new List<BaseItem>();

    // ��������������ʱ���ã���ղ�ע���ʼ
    public static void InitWith(BaseItem initial)
    {
        slot3List.Clear();
        if (initial != null)
            slot3List.Add(initial);
    }

    // �������ã�ע��װ��
    public static void RegisterItem(BaseItem item)
    {
        if (item != null && !slot3List.Contains(item))
            slot3List.Add(item);
    }

    // ���������ʱ���ã��Ƴ�
    public static void RemoveItem(BaseItem item)
    {
        if (item != null)
            slot3List.Remove(item);
    }

    // InventorySystem �ã����ؿ��������
    public static List<BaseItem> GetSlot3List()
        => new List<BaseItem>(slot3List);
}
