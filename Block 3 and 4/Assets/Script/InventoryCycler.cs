using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ʱ��ռλ�ࣺά��һ������ 3 ��ѭ����Ʒ���б�
/// Ŀǰ��������Ϸ��ʼʱ���е���һ�� FoodItem��
/// �Ժ�����̵�ϵͳ��̬��ץ�� / �����ע�������
/// </summary>
public static class InventoryCycler
{
    // �ڲ���̬�б�
    private static readonly List<BaseItem> slot3List = new List<BaseItem>();

    /// <summary>����Ϸ����ʱ�ѳ�ʼ��Ʒע�������</summary>
    public static void InitWith(BaseItem initial)
    {
        if (initial != null && !slot3List.Contains(initial))
            slot3List.Add(initial);
    }

    /// <summary>�̵깺����װ������ã���װ������ѭ����</summary>
    public static void RegisterItem(BaseItem item)
    {
        if (item != null && !slot3List.Contains(item))
            slot3List.Add(item);
    }

    /// <summary>���ص�ǰ��ѭ���б��� InventorySystem ���ã���</summary>
    public static List<BaseItem> GetSlot3List() => slot3List;
}
