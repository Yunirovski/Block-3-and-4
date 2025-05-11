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

    // ���� ����ʱ���� ���� 
    private float nextReadyTime = 0f;
    private Transform playerRoot;

    public override void OnSelect(GameObject model)
    {
        if (Camera.main != null)
            playerRoot = Camera.main.transform;
        else
            Debug.LogError("MagicWandItem: �Ҳ��������");

        // �����װ������ʱ����ӡ��ǰ cooldown
        Debug.Log($"MagicWandItem ��װ������ǰ��ȴʱ�䣺{cooldown}s");
    }

    public override void OnUse()
    {
        // ��ӡ����֤�ǲ��Ƕ�ȡ������ Inspector ��ĵ�ֵ
        Debug.Log($"MagicWandItem.OnUse() called��cooldown = {cooldown}s��nextReadyTime = {nextReadyTime:F2}");

        if (Time.time < nextReadyTime)
        {
            Debug.Log($"MagicWandItem: ��ȴ�� {(nextReadyTime - Time.time):F1}s");
            return;
        }
        if (playerRoot == null) return;

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

        Debug.Log($"MagicWandItem: �ɹ����� {count} ֻ������� {attractDuration}s
