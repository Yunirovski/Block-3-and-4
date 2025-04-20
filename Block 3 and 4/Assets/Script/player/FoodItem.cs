using UnityEngine;

[CreateAssetMenu(menuName = "Items/FoodItem")]
public class FoodItem : BaseItem
{
    public GameObject foodPrefab;   // ʳ��Ԥ����
    public Transform spawnPoint;    // ����λ��

    public override void OnUse()
    {
        if (foodPrefab != null && spawnPoint != null)
        {
            GameObject obj = Instantiate(foodPrefab, spawnPoint.position, Quaternion.identity);
            // ���ڴ����ιʳ�߼�
        }
    }
}