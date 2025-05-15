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

    // ��ǰѡ���ʳ����������
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
        // ��֤����
        if (foodTypes == null || foodPrefabs == null ||
            foodTypes.Count == 0 || foodPrefabs.Count == 0 ||
            foodTypes.Count != foodPrefabs.Count)
        {
            Debug.LogError("FoodItem configuration error: Type and prefab lists must be non-empty and equal length.");
            return;
        }

        // ������������ѭ����
        currentIndex = (currentIndex + (forward ? 1 : -1) + foodTypes.Count) % foodTypes.Count;

        // ʹ��UIManager����UI
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
        // ʹ��UIManager���µ�ǰʳ������
        UIManager.Instance.UpdateFoodTypeText(foodTypes[currentIndex]);
    }

    /// <summary>
    /// Called when the player uses the item (e.g., left-click).
    /// Spawns the selected food prefab and updates the ConsumableManager.
    /// </summary>
    public override void OnUse()
    {
        // ��������һ��λʳ����û��ʣ����ִ��
        if (!ConsumableManager.Instance.UseFood(foodTypes[currentIndex]))
        {
            UIManager.Instance.UpdateCameraDebugText("û��ʳ��ʣ�࣡");
            return;
        }

        // ��λ�������ȷ������λ��
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("FoodItem.OnUse: Main Camera not found. Cannot spawn food.");
            return;
        }

        // ��������λ�ã������ǰ��spawnDistance�״�
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * spawnDistance;

        // ��ȡ��ǰʳ�����͵�Ԥ����
        GameObject prefab = foodPrefabs[currentIndex];
        if (prefab == null)
        {
            Debug.LogError($"FoodItem.OnUse: Prefab at index {currentIndex} is null.");
            return;
        }

        // ��������ʵ����ʳ������
        Instantiate(prefab, spawnPos, Quaternion.identity);

        // ����UI����¼���ɶ���
        UIManager.Instance.UpdateCameraDebugText($"������ {foodTypes[currentIndex]} ��ǰ�� {spawnDistance}m ��");
        Debug.Log($"FoodItem: Instantiated {foodTypes[currentIndex]} at {spawnPos}");
    }

    /// <summary>
    /// ������Ʒ��ÿ֡���£�֧��ʹ�ÿ�ݼ��л�ʳ������
    /// </summary>
    public override void HandleUpdate()
    {
        // ʹ�� [ �� ] ���л�ʳ������
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            CycleFoodType(false); // ��һ��
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            CycleFoodType(true); // ��һ��
        }
    }
}