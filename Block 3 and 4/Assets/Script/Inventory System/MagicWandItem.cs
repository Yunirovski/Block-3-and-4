// Assets/Scripts/Items/MagicWandItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/MagicWandItem")]
public class MagicWandItem : BaseItem
{
    [Header("魔法设置")]
    [Tooltip("作用半径 (m)")]
    public float radius = 30f;
    [Tooltip("魔法冷却时间 (s)")]
    public float cooldown = 30f;
    [Tooltip("吸引持续时间 (s)")]
    public float attractDuration = 10f;

    // 内部 运行时状态 变量 
    float nextReadyTime = 0f;
    Transform playerRoot;

    /// <summary>
    /// 开发模式下修改冷却时间
    /// </summary>
    public void SetCooldown(float cd)
    {
        cooldown = Mathf.Max(0f, cd);
    }

    public override void OnSelect(GameObject model)
    {
        // 直接以相机为吸引目标
        if (Camera.main != null)
            playerRoot = Camera.main.transform;
        else
            Debug.LogError("MagicWandItem: 找不到相机");
    }

    public override void OnUse()
    {
        // 冷却判断
        if (Time.time < nextReadyTime)
        {
            float remainTime = nextReadyTime - Time.time;
            UIManager.Instance.UpdateCameraDebugText($"魔法棒冷却中: 剩余 {remainTime:F1}秒");
            return;
        }
        if (playerRoot == null) return;

        // 以玩家位置发出吸引效果
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

        UIManager.Instance.UpdateCameraDebugText($"吸引了 {count} 只动物，持续 {attractDuration}秒");

        // 记录下次可用时间
        nextReadyTime = Time.time + cooldown;

        // 使用UIManager显示冷却
        UIManager.Instance.StartItemCooldown(this, cooldown);
    }
}