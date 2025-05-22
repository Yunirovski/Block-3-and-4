// Assets/Scripts/Items/DartGunItem.cs
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/DartGunItem")]
public class DartGunItem : BaseItem
{
    [Header("麻醉枪设置")]
    [Tooltip("射击最远距离 (m)")]
    public float maxDistance = 20f;
    [Tooltip("射击冷却时间 (s)")]
    public float cooldown = 3f;
    [Tooltip("击中动物昏迷持续时间 (s)")]
    public float stunDuration = 10f;

    [Header("光线可视化")]
    [Tooltip("光线材质（需要 LineRenderer）")]
    public Material rayMaterial;
    [Tooltip("光线宽度")]
    public float rayWidth = 0.02f;
    [Tooltip("光线显示时间 (s)")]
    public float rayDuration = 0.1f;

    // 运行 时状态 变量 
    float nextReadyTime = 0f;
    Camera cam;
    Transform playerRoot;

    public override void OnSelect(GameObject model)
    {
        // 查找相机和玩家根节点以便于计算
        cam = Camera.main;
        if (cam == null)
            Debug.LogError("DartGunItem: 找不到相机");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerRoot = player.transform;
    }

    public override void OnUse()
    {
        Debug.Log("DartGunItem: 尝试发射！");  // 调试信息

        if (cam == null) return;

        // 冷却检查
        if (Time.time < nextReadyTime)
        {
            float remainTime = nextReadyTime - Time.time;
            UIManager.Instance.UpdateCameraDebugText($"麻醉枪冷却中: {remainTime:F1}秒");
            return;
        }

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        // 用 RaycastAll 收集所有碰撞体，按距离排序处理
        var hits = Physics.RaycastAll(origin, dir, maxDistance)
                          .OrderBy(h => h.distance);

        RaycastHit? validHit = null;
        foreach (var h in hits)
        {
            // 过滤距离太近或属于玩家自身或玩家子级的碰撞体
            if (h.distance < 0.1f) continue;
            if (playerRoot != null && h.collider.transform.IsChildOf(playerRoot)) continue;

            validHit = h;
            break;
        }

        Vector3 rayEnd;
        if (validHit.HasValue)
        {
            var hit = validHit.Value;
            rayEnd = hit.point;

            // 检查并对动物麻醉
            var animal = hit.collider.GetComponent<AnimalBehavior>();
            if (animal != null)
            {
                animal.Stun(stunDuration);
                UIManager.Instance.UpdateCameraDebugText($"麻醉了 {animal.gameObject.name}，持续 {stunDuration}秒");
            }
            else
            {
                UIManager.Instance.UpdateCameraDebugText("未击中任何可麻醉的动物");
            }
        }
        else
        {
            // 未命中，光线延伸到最大距离
            rayEnd = origin + dir * maxDistance;
            UIManager.Instance.UpdateCameraDebugText("未击中目标");
        }

        // 创建光线（可视化）
        if (rayMaterial != null)
            ShowLine(origin, rayEnd);
        else
            Debug.DrawLine(origin, rayEnd, Color.cyan, rayDuration);

        // 记录冷却、通知 UI
        nextReadyTime = Time.time + cooldown;

        // 使用UIManager显示冷却
        UIManager.Instance.StartItemCooldown(this, cooldown);
    }

    void ShowLine(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("DartRay");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = rayMaterial;
        lr.startWidth = rayWidth;
        lr.endWidth = rayWidth;
        lr.useWorldSpace = true;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        Destroy(go, rayDuration);
    }
}