// Assets/Scripts/Items/MagicWandItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/MagicWandItem")]
public class MagicWandItem : BaseItem
{
    [Header("��������")]
    [Tooltip("�����뾶 (m)")]
    public float radius = 30f;
    [Tooltip("������ȴʱ�� (s)")]
    public float cooldown = 30f;
    [Tooltip("��������ʱ�� (s)")]
    public float attractDuration = 10f;

    // ���� ����ʱ״̬ ���� 
    float nextReadyTime = 0f;
    Transform playerRoot;

    /// <summary>
    /// ��������ʱ�޸���ȴʱ��
    /// </summary>
    public void SetCooldown(float cd)
    {
        cooldown = Mathf.Max(0f, cd);
    }

    public override void OnSelect(GameObject model)
    {
        // ֱ�����������Ϊ����Ŀ��
        if (Camera.main != null)
            playerRoot = Camera.main.transform;
        else
            Debug.LogError("MagicWandItem: �Ҳ��������");
    }

    public override void OnUse()
    {
        // ��ȴ�ж�
        if (Time.time < nextReadyTime)
        {
            Debug.Log($"MagicWandItem: ��ȴ�У�ʣ�� {(nextReadyTime - Time.time):F1}s");
            return;
        }
        if (playerRoot == null) return;

        // �����λ�÷���������
        Collider[] hits = Physics.OverlapSphere(playerRoot.position, radius);
        int count = 0;
        foreach (var col in hits)
        {
            var animal = col.GetComponent<AnimalBehavior>();
            if (animal != null)
            {
                animal.Attract(playerRoot, attractDuration);
                count++;
            }
        }

        Debug.Log($"MagicWandItem: ���� {count} ֻ������� {attractDuration}s");

        // ��¼�´ο���ʱ��
        nextReadyTime = Time.time + cooldown;

        // ֪ͨ UI�����ж����ߣ�
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
    }
}
