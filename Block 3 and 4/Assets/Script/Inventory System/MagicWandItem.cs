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
    private float nextReadyTime = 0f;
    private Transform playerRoot;

    public override void OnSelect(GameObject model)
    {
        // ������Ҹ�������Ϊ "Player"
        playerRoot = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerRoot == null)
            Debug.LogError("MagicWandItem: �Ҳ������Ϊ Player �Ķ���");
    }

    public override void OnUse()
    {
        if (Time.time < nextReadyTime)
        {
            Debug.Log($"MagicWandItem: ��ȴ�� {(nextReadyTime - Time.time):F1}s");
            return;
        }
        if (playerRoot == null) return;

        // �����λ�÷���������Χ
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

        // ��¼��ȴ
        nextReadyTime = Time.time + cooldown;
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
    }
}
