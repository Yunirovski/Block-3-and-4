using UnityEngine;

[CreateAssetMenu(menuName = "Items/SkateboardItem")]
public class SkateboardItem : BaseItem
{
    public float speedMultiplier = 1.3f;   // 30% 提速
    public float cooldown = 0f;            // 无 CD

    private player_move2 controller;       // 你的人物脚本
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
        // 滑板不需要左键；留空
    }
}
