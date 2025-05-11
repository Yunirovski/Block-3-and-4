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

    // —— 运行时缓存 —— 
    private float nextReadyTime = 0f;
    private Transform playerRoot;

    public override void OnSelect(GameObject model)
    {
        if (Camera.main != null)
            playerRoot = Camera.main.transform;
        else
            Debug.LogError("MagicWandItem: 找不到主相机");

        // 当玩家装备法杖时，打印当前 cooldown
        Debug.Log($"MagicWandItem 已装备，当前冷却时间：{cooldown}s");
    }

    public override void OnUse()
    {
        // 打印以验证是不是读取了你在 Inspector 里改的值
        Debug.Log($"MagicWandItem.OnUse() called；cooldown = {cooldown}s，nextReadyTime = {nextReadyTime:F2}");

        if (Time.time < nextReadyTime)
        {
            Debug.Log($"MagicWandItem: 冷却中 {(nextReadyTime - Time.time):F1}s");
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

        Debug.Log($"MagicWandItem: 成功吸引 {count} 只动物，持续 {attractDuration}s
