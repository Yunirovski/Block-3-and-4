// Assets/Scripts/Items/MagicWandItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/MagicWandItem")]
public class MagicWandItem : BaseItem
{
    [Header("法杖设置")]
    [Tooltip("吸引半径 (m)")]
    public float radius = 30f;
    [Tooltip("吸引冷却时间 (s)")]
    public float cooldown = 30f;
    [Tooltip("吸引持续时间 (s)")]
    public float attractDuration = 10f;

    // —— 运行时状态 —— 
    float nextReadyTime = 0f;
    Transform playerRoot;

    /// <summary>
    /// 允许运行时修改冷却时间
    /// </summary>
    public void SetCooldown(float cd)
    {
        cooldown = Mathf.Max(0f, cd);
    }

    public override void OnSelect(GameObject model)
    {
        // 直接用主相机作为吸引目标
        if (Camera.main != null)
            playerRoot = Camera.main.transform;
        else
            Debug.LogError("MagicWandItem: 找不到主相机");
    }

    public override void OnUse()
    {
        // 冷却判断
        if (Time.time < nextReadyTime)
        {
            Debug.Log($"MagicWandItem: 冷却中，剩余 {(nextReadyTime - Time.time):F1}s");
            return;
        }
        if (playerRoot == null) return;

        // 在玩家位置发射吸引波
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

        Debug.Log($"MagicWandItem: 吸引 {count} 只动物，持续 {attractDuration}s");

        // 记录下次可用时间
        nextReadyTime = Time.time + cooldown;

        // 通知 UI（如有订阅者）
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
    }
}
