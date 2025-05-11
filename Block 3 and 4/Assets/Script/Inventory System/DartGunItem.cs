// Assets/Scripts/Items/DartGunItem.cs
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/DartGunItem")]
public class DartGunItem : BaseItem
{
    [Header("麻醉枪参数")]
    [Tooltip("射线最远距离 (m)")]
    public float maxDistance = 20f;
    [Tooltip("发射后冷却时间 (s)")]
    public float cooldown = 3f;
    [Tooltip("命中动物后昏迷持续时间 (s)")]
    public float stunDuration = 10f;

    [Header("射线可视化")]
    [Tooltip("射线材质（用于 LineRenderer）")]
    public Material rayMaterial;
    [Tooltip("射线宽度")]
    public float rayWidth = 0.02f;
    [Tooltip("射线显示时长 (s)")]
    public float rayDuration = 0.1f;

    // —— 运行时状态 —— 
    float nextReadyTime = 0f;
    Camera cam;
    Transform playerRoot;

    public override void OnSelect(GameObject model)
    {
        // 缓存摄像机和玩家根节点用于忽略自体
        cam = Camera.main;
        if (cam == null)
            Debug.LogError("DartGunItem: 找不到主相机");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerRoot = player.transform;
    }

    public override void OnUse()
    {
        Debug.Log("DartGunItem: 尝试发射！");  // 调试输出

        if (cam == null) return;

        // 冷却检查
        if (Time.time < nextReadyTime)
        {
            Debug.Log($"DartGunItem: 冷却中 {(nextReadyTime - Time.time):F1}s");
            return;
        }

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        // 用 RaycastAll 收集所有碰撞，并按距离排序
        var hits = Physics.RaycastAll(origin, dir, maxDistance)
                          .OrderBy(h => h.distance);

        RaycastHit? validHit = null;
        foreach (var h in hits)
        {
            // 跳过距离太近（可能是相机自身）或属于玩家层级的碰撞体
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

            // 如果击中动物，则晕眩
            var animal = hit.collider.GetComponent<AnimalBehavior>();
            if (animal != null)
            {
                animal.Stun(stunDuration);
                Debug.Log($"DartGunItem: 打晕 {animal.gameObject.name}，持续 {stunDuration}s");
            }
            else
            {
                Debug.Log("DartGunItem: 未命中任何可晕眩的动物");
            }
        }
        else
        {
            // 未击中，沿最大距离方向显示射线
            rayEnd = origin + dir * maxDistance;
            Debug.Log("DartGunItem: 射程内无目标");
        }

        // 绘制射线（可视化）
        if (rayMaterial != null)
            ShowLine(origin, rayEnd);
        else
            Debug.DrawLine(origin, rayEnd, Color.cyan, rayDuration);

        // 记录冷却并通知 UI
        nextReadyTime = Time.time + cooldown;
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
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
