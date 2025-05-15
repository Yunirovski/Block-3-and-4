// Assets/Scripts/Items/MagicWandItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/MagicWandItem")]
public class MagicWandItem : BaseItem
{
    [Header("ħ������")]
    [Tooltip("���ð뾶 (m)")]
    public float radius = 30f;
    [Tooltip("ħ����ȴʱ�� (s)")]
    public float cooldown = 30f;
    [Tooltip("��������ʱ�� (s)")]
    public float attractDuration = 10f;

    // �ڲ� ����ʱ״̬ ���� 
    float nextReadyTime = 0f;
    Transform playerRoot;

    /// <summary>
    /// ����ģʽ���޸���ȴʱ��
    /// </summary>
    public void SetCooldown(float cd)
    {
        cooldown = Mathf.Max(0f, cd);
    }

    public override void OnSelect(GameObject model)
    {
        // ֱ�������Ϊ����Ŀ��
        if (Camera.main != null)
            playerRoot = Camera.main.transform;
        else
            Debug.LogError("MagicWandItem: �Ҳ������");
    }

    public override void OnUse()
    {
        // ��ȴ�ж�
        if (Time.time < nextReadyTime)
        {
            float remainTime = nextReadyTime - Time.time;
            UIManager.Instance.UpdateCameraDebugText($"ħ������ȴ��: ʣ�� {remainTime:F1}��");
            return;
        }
        if (playerRoot == null) return;

        // �����λ�÷�������Ч��
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

        UIManager.Instance.UpdateCameraDebugText($"������ {count} ֻ������� {attractDuration}��");

        // ��¼�´ο���ʱ��
        nextReadyTime = Time.time + cooldown;

        // ʹ��UIManager��ʾ��ȴ
        UIManager.Instance.StartItemCooldown(this, cooldown);
    }
}