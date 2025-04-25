using UnityEngine;

[CreateAssetMenu(menuName = "Items/SkateboardItem")]
public class SkateboardItem : BaseItem
{
    public float speedMultiplier = 1.3f;   // 30% ����
    public float cooldown = 0f;            // �� CD

    private player_move2 controller;       // �������ű�
    private bool active;

    public override void OnSelect(GameObject model)
    {
        controller = Object.FindObjectOfType<player_move2>();
    }

    public override void OnReady()
    {
        if (controller != null && !active)
        {
            controller.walkSpeed *= speedMultiplier;
            controller.runSpeed *= speedMultiplier;
            active = true;
        }
    }

    public override void OnUnready()
    {
        if (controller != null && active)
        {
            controller.walkSpeed /= speedMultiplier;
            controller.runSpeed /= speedMultiplier;
            active = false;
        }
    }

    public override void OnUse()
    {
        // ���岻��Ҫ���������
    }
}
