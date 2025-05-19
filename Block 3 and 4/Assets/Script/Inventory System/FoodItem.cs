// Assets/Scripts/Items/FoodItem.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject representing a multi-type food item:
/// - Use the [ and ] keys to cycle through available food types.
/// - When used, spawns the selected food prefab at a specified distance in front of the player's camera.
/// - Deducts one unit from the ConsumableManager's food stock.
/// </summary>
[CreateAssetMenu(menuName = "Items/FoodItem")]
public class FoodItem : BaseItem
{
    [Header("Types & Prefabs (must match in order)")]
    [Tooltip("List of all food categories this item can represent.")]
    public List<FoodType> foodTypes;

    [Tooltip("Prefabs corresponding to each entry in foodTypes; indices must align.")]
    public List<GameObject> foodPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Distance (in meters) in front of the camera where the food will be instantiated.")]
    public float spawnDistance = 2f;

    // 当前选择的食物类型索引
    private int currentIndex = 0;

    /// <summary>
    /// Cycles the selected food type, wrapping around at the ends.
    /// Updates the on-screen debug text and logs the change.
    /// </summary>
    /// <param name="forward">
    /// True to advance to the next type; false to go back to the previous type.
    /// </param>
    public void CycleFoodType(bool forward)
    {
        // 验证配置
        if (foodTypes == null || foodPrefabs == null ||
            foodTypes.Count == 0 || foodPrefabs.Count == 0 ||
            foodTypes.Count != foodPrefabs.Count)
        {
            Debug.LogError("FoodItem configuration error: Type and prefab lists must be non-empty and equal length.");
            return;
        }

        // 计算新索引（循环）
        currentIndex = (currentIndex + (forward ? 1 : -1) + foodTypes.Count) % foodTypes.Count;

        // 使用UIManager更新UI
        UIManager.Instance.UpdateFoodTypeText(foodTypes[currentIndex]);
        Debug.Log($"FoodItem: Switched to {foodTypes[currentIndex]} (index={currentIndex})");
    }

    /// <summary>
    /// Called when the item is equipped/selected in the inventory.
    /// Displays the currently selected food type in the debug UI.
    /// </summary>
    /// <param name="model">The GameObject model instantiated for preview (unused here).</param>
    public override void OnSelect(GameObject model)
    {
        // 使用UIManager更新当前食物类型
        UIManager.Instance.UpdateFoodTypeText(foodTypes[currentIndex]);
    }

    /// <summary>
    /// Called when the player uses the item (e.g., left-click).
    /// Spawns the selected food prefab and updates the ConsumableManager.
    /// </summary>
    public override void OnUse()
    {
        // 尝试消耗一单位食物；如果没有剩余则不执行
        if (!ConsumableManager.Instance.UseFood(foodTypes[currentIndex]))
        {
            UIManager.Instance.UpdateCameraDebugText("没有食物剩余！");
            return;
        }

        // 定位主相机以确定生成位置
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("FoodItem.OnUse: Main Camera not found. Cannot spawn food.");
            return;
        }

        // 计算生成位置：在相机前方spawnDistance米处
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * spawnDistance;

        // 获取当前食物类型的预制体
        GameObject prefab = foodPrefabs[currentIndex];
        if (prefab == null)
        {
            Debug.LogError($"FoodItem.OnUse: Prefab at index {currentIndex} is null.");
            return;
        }

        // 在世界中实例化食物物体
        Instantiate(prefab, spawnPos, Quaternion.identity);

        // 更新UI并记录生成动作
        UIManager.Instance.UpdateCameraDebugText($"已生成 {foodTypes[currentIndex]} 在前方 {spawnDistance}m 处");
        Debug.Log($"FoodItem: Instantiated {foodTypes[currentIndex]} at {spawnPos}");
    }

    /// <summary>
    /// 处理物品的每帧更新，支持使用快捷键切换食物类型
    /// </summary>
    public override void HandleUpdate()
    {
        // 使用 [ 和 ] 键切换食物类型
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            CycleFoodType(false); // 上一个
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            CycleFoodType(true); // 下一个
        }
    }
}