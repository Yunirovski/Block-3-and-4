using UnityEngine;

public class FoodPickup : MonoBehaviour
{
    public FoodItem foodData;     // ��Ӧ��FoodItem����

    private void OnTriggerEnter(Collider other)
    {
        InventorySystem inv = other.GetComponent<InventorySystem>();
        if (inv != null)
        {
            // �����б��е�4����λ����ʳ��
            inv.availableItems[3] = foodData;
            Destroy(gameObject);   // ʰȡ�����ٳ�������
        }
    }
}
