// Assets/Scripts/Items/FoodItem.cs

using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 多类型食物道具：
///   - 使用 [ / ] 切换当前食物类型  
///   - 使用时在玩家摄像机前方 spawnDistance 米处生成对应 prefab  
///   - 扣除 ConsumableManager 中的食物数量  
/// </summary>
[CreateAssetMenu(menuName = "Items/FoodItem")]
public class FoodItem : BaseItem
{
    [Header("Types & Prefabs (parallel lists)")]
    [Tooltip("食物类型列表")]
    public List<FoodType> foodTypes;
    [Tooltip("对应 Prefab 列表，顺序要一一对应 foodTypes")]
    public List<GameObject> foodPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("在摄像机前方多少米生成食物")]
    public float spawnDistance = 2f;

    [HideInInspector] public TMP_Text debugText;  // 由 InventorySystem 注入

    private int currentIndex = 0;

    /// <summary>
    /// 切换当前食物类型
    /// </summary>
    /// <param name="forward">true 切到下一个，false 切到上一个</param>
    public void CycleFoodType(bool forward)
    {
        if (foodTypes == null || foodPrefabs == null ||
            foodTypes.Count == 0 || foodPrefabs.Count == 0 ||
            foodTypes.Count != foodPrefabs.Count)
        {
            Debug.LogError("FoodItem 切换失败：列表未正确配置");
            return;
        }

        currentIndex = (currentIndex + (forward ? 1 : -1) + foodTypes.Count) % foodTypes.Count;
        debugText?.SetText($"切换食物: {foodTypes[currentIndex]}");
        Debug.Log($"FoodItem: 切换到 {foodTypes[currentIndex]} (index={currentIndex})");
    }

    public override void OnSelect(GameObject model)
    {
        // 选中时显示当前类型
        debugText?.SetText($"当前食物: {foodTypes[currentIndex]}");
    }

    public override void OnUse()
    {
        // 扣除库存
        if (!ConsumableManager.Instance.UseFood())
        {
            debugText?.SetText("没有食物可投放！");
            return;
        }

        // 计算摄像机前方位置
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("FoodItem: 找不到 Main Camera，无法投放");
            return;
        }

        Vector3 spawnPos = cam.transform.position + cam.transform.forward * spawnDistance;

        // 实例化对应 prefab
        GameObject prefab = foodPrefabs[currentIndex];
        if (prefab == null)
        {
            Debug.LogError($"FoodItem: foodPrefabs[{currentIndex}] 未赋值");
            return;
        }

        Instantiate(prefab, spawnPos, Quaternion.identity);
        debugText?.SetText($"投放 {foodTypes[currentIndex]} 于前方{spawnDistance}米处");
        Debug.Log($"FoodItem: 在 {spawnPos} 位置生成了 {foodTypes[currentIndex]}");
    }
}
