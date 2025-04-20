using UnityEngine;

public class FoodPickup : MonoBehaviour
{
    public FoodItem foodData;     // 对应的FoodItem数据

    private void OnTriggerEnter(Collider other)
    {
        InventorySystem inv = other.GetComponent<InventorySystem>();
        if (inv != null)
        {
            // 假设列表中第4个槽位用于食物
            inv.availableItems[3] = foodData;
            Destroy(gameObject);   // 拾取后销毁场景物体
        }
    }
}
