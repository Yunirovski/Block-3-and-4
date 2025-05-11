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
    private float nextReadyTime = 0f;
    private Transform playerRoot;

    public override void OnSelect(GameObject model)
    {
        // 假设玩家根物体标记为 "Player"
        playerRoot = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerRoot == null)
            Debug.LogError("MagicWandItem: 找不到标记为 Player 的对象");
    }

    public override void OnUse()
    {
        if (Time.time < nextReadyTime)
        {
            Debug.Log($"MagicWandItem: 冷却中 {(nextReadyTime - Time.time):F1}s");
            return;
        }
        if (playerRoot == null) return;

        // 在玩家位置发出吸引范围
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

        // 记录冷却
        nextReadyTime = Time.time + cooldown;
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
    }
}
