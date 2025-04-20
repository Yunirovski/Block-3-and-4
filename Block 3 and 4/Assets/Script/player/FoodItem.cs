using UnityEngine;

[CreateAssetMenu(menuName = "Items/FoodItem")]
public class FoodItem : BaseItem
{
    public GameObject foodPrefab;   // 食物预制体
    public Transform spawnPoint;    // 生成位置

    public override void OnUse()
    {
        if (foodPrefab != null && spawnPoint != null)
        {
            GameObject obj = Instantiate(foodPrefab, spawnPoint.position, Quaternion.identity);
            // 可在此添加喂食逻辑
        }
    }
}