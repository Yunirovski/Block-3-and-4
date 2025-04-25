using UnityEngine;

[CreateAssetMenu(menuName = "Items/MagicWandItem")]
public class MagicWandItem : BaseItem
{
    public float radius = 20f;
    public float cooldown = 60f;

    private float nextReadyTime;

    public override void OnUse()
    {
        if (Time.time < nextReadyTime) return;

        Collider[] cols = Physics.OverlapSphere(Camera.main.transform.position, radius);
        foreach (var col in cols)
        {
            if (col.TryGetComponent(out AnimalEvent ae))
            {
                Debug.Log($"MagicWand 吸引 {ae.animalName}");
                // TODO: AI 改变目标，向玩家移动
            }
        }

        nextReadyTime = Time.time + cooldown;
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
    }
}
