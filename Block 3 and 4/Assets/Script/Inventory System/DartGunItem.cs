using UnityEngine;

[CreateAssetMenu(menuName = "Items/DartGunItem")]
public class DartGunItem : BaseItem
{
    public float range = 25f;
    public float cooldown = 20f;

    private float nextReadyTime;

    public override void OnUse()
    {
        if (Time.time < nextReadyTime) return;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
                            out RaycastHit hit, range))
        {
            if (hit.collider.GetComponent<AnimalEvent>())
            {
                Debug.Log("DartGun 命中动物，进入布偶状态！（占位）");
                // TODO: 设置 Rigidbody & Animator 以布偶化
            }
        }

        nextReadyTime = Time.time + cooldown;
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
    }
}
