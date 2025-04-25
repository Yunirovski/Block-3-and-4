using UnityEngine;

[CreateAssetMenu(menuName = "Items/GrappleItem")]
public class GrappleItem : BaseItem
{
    [Header("Grapple Params")]
    public float maxDistance = 30f;   // 射线最长距离
    public float pullSpeed = 15f;   // 拉动速度
    public float cooldown = 4f;    // 冷却

    private Transform player;         // 玩家 Transform
    private float nextReadyTime;

    public override void OnSelect(GameObject model)
    {
        player = Camera.main.transform.root;     // 获取玩家根
    }

    public override void OnUse()
    {
        // 冷却检查
        if (Time.time < nextReadyTime) return;

        // 射线：注意 origin 用 position，第四参数是 maxDistance(float)
        if (Physics.Raycast(Camera.main.transform.position,
                            Camera.main.transform.forward,
                            out RaycastHit hit,
                            maxDistance))
        {
            // 在玩家身上加一个 GrappleMover，让它把玩家拉到目标点
            var mover = player.gameObject.AddComponent<GrappleMover>();
            mover.Init(hit.point, pullSpeed);

            // 冷却计时 & 通知 HUD
            nextReadyTime = Time.time + cooldown;
            InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
        }
    }
}

/// <summary>
/// 运行时挂在玩家上的小组件：把玩家朝目标点拉动，抵达后自动销毁自己
/// </summary>
public class GrappleMover : MonoBehaviour
{
    private Vector3 target;
    private float speed;

    public void Init(Vector3 point, float pullSpeed)
    {
        target = point;
        speed = pullSpeed;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 1f)
            Destroy(this);     // 到点即结束
    }
}
