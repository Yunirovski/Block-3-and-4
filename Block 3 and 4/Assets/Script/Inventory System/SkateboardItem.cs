using UnityEngine;

[CreateAssetMenu(menuName = "Items/SkateboardItem")]
public class SkateboardItem : BaseItem
{
    [Tooltip("�ٶ��������ʣ����� 1.3 ��ʾ���� 30%")]
    public float speedMultiplier = 1.3f;

    private IMoveController moveController;
    private bool isActive;

    /// <summary>ѡ��ʱ���ҳ�����ʵ�� IMoveController �Ķ���</summary>
    public override void OnSelect(GameObject model)
    {
        // FindObjectsOfType ���� MonoBehaviour������ is �����ɸѡ�ӿ�
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>())
        {
            if (mb is IMoveController mc)
            {
                moveController = mc;
                break;
            }
        }

        if (moveController == null)
            Debug.LogWarning("SkateboardItem: No IMoveController found in scene.");
    }

    public override void OnReady()
    {
        if (!isActive && moveController != null)
        {
            moveController.ModifySpeed(speedMultiplier);
            isActive = true;
            Debug.Log("Skateboard: Speed boost ON");
        }
    }

    public override void OnUnready()
    {
        if (isActive && moveController != null)
        {
            moveController.ModifySpeed(1f);
            isActive = false;
            Debug.Log("Skateboard: Speed boost OFF");
        }
    }

    public override void OnUse() { } // ��������޶���
}
