using UnityEngine;

[CreateAssetMenu(menuName = "Items/SkateboardItem")]
public class SkateboardItem : BaseItem
{
    [Tooltip("速度提升倍率，例如 1.3 表示加速 30%")]
    public float speedMultiplier = 1.3f;

    private IMoveController moveController;
    private bool isActive;

    /// <summary>选中时查找场景中实现 IMoveController 的对象</summary>
    public override void OnSelect(GameObject model)
    {
        // FindObjectsOfType 返回 MonoBehaviour，再用 is 运算符筛选接口
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

    public override void OnUse() { } // 滑板左键无动作
}
