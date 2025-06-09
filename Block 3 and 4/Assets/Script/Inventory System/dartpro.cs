// Assets/Scripts/Projectiles/DartProjectile.cs - 优化版本，更好的生命周期管理
using UnityEngine;

public class DartProjectile : MonoBehaviour
{
    [Header("Dart Settings")]
    [Tooltip("昏迷持续时间")]
    public float stunDuration = 10f;

    [Tooltip("镖的生命周期（秒）")]
    public float lifetime = 7f; // 默认改为7秒

    [Tooltip("击中目标后是否粘住")]
    public bool stickToTarget = true;

    [Tooltip("粘住后的偏移")]
    public Vector3 stickOffset = Vector3.zero;

    [Header("Visual Effects")]
    [Tooltip("销毁时的特效")]
    public GameObject destroyEffect;

    [Tooltip("击中目标时的特效")]
    public GameObject hitEffect;

    [Header("Debug Settings")]
    [Tooltip("显示镖的调试信息（屏幕UI）")]
    public bool showDebugUI = false;

    [Tooltip("显示镖的Gizmos调试线框")]
    public bool showDebugGizmos = false;
    // 内部状态
    private bool hasHit = false;
    private float timer = 0f;
    private Rigidbody rb;
    private Collider col;
    private bool isStuckToTarget = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        timer = 0f;

        Debug.Log($"麻醉镖生成，生命周期: {lifetime}秒，昏迷时间: {stunDuration}秒");
    }

    void Update()
    {
        // 生命周期计时
        timer += Time.deltaTime;

        // 检查是否超过生命周期
        if (timer >= lifetime)
        {
            DestroySelf();
            return;
        }

        // 如果还在飞行，让镖朝向飞行方向
        if (!hasHit && rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }

        // 如果镖粘住了目标，检查目标是否还存在
        if (isStuckToTarget && transform.parent == null)
        {
            Debug.Log("目标消失，镖自动销毁");
            DestroySelf();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 避免重复触发
        if (hasHit) return;

        GameObject hitObject = collision.gameObject;
        Debug.Log($"麻醉镖击中: {hitObject.name}");

        // 检查是否击中普通动物
        AnimalBehavior animal = hitObject.GetComponent<AnimalBehavior>();
        if (animal != null)
        {
            // 让动物昏迷
            animal.Stun(stunDuration);
            Debug.Log($"动物 {animal.name} 被麻醉，持续 {stunDuration} 秒");

            // 标记击中
            hasHit = true;

            // 创建击中特效
            CreateEffect(collision.contacts[0].point, hitEffect);

            // 粘住目标
            if (stickToTarget)
            {
                StickToTarget(collision);
            }
            else
            {
                // 不粘住就直接销毁
                DestroySelf();
            }
            return;
        }

        // 检查是否击中鸟类
        PigeonBehavior bird = hitObject.GetComponent<PigeonBehavior>();
        if (bird != null)
        {
            // 让鸟类昏迷
            bird.Stun(stunDuration);
            Debug.Log($"鸟类 {bird.name} 被麻醉，持续 {stunDuration} 秒");

            // 标记击中
            hasHit = true;

            // 创建击中特效
            CreateEffect(collision.contacts[0].point, hitEffect);

            // 粘住目标
            if (stickToTarget)
            {
                StickToTarget(collision);
            }
            else
            {
                // 不粘住就直接销毁
                DestroySelf();
            }
            return;
        }

        // 击中其他物体，停止运动并粘住
        hasHit = true;
        StickToTarget(collision);

        // 创建击中特效
        CreateEffect(collision.contacts[0].point, hitEffect);

        Debug.Log($"麻醉镖击中 {hitObject.name}，已粘住");
    }

    /// <summary>
    /// 粘住目标
    /// </summary>
    private void StickToTarget(Collision collision)
    {
        // 停止物理运动
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 禁用碰撞体避免进一步碰撞
        if (col != null)
        {
            col.enabled = false;
        }

        // 设置位置和朝向
        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = collision.contacts[0].normal;

        transform.position = hitPoint + hitNormal * 0.1f + stickOffset;
        transform.rotation = Quaternion.LookRotation(-hitNormal);

        // 成为目标的子物体（如果目标不是玩家）
        Transform target = collision.transform;
        if (target != null && !target.CompareTag("Player"))
        {
            transform.SetParent(target);
            isStuckToTarget = true;
            Debug.Log($"麻醉镖粘住了 {target.name}");
        }

        // 粘住后稍微减少生命周期，但不会少于2秒
        float remainingTime = lifetime - timer;
        if (remainingTime > 5f)
        {
            lifetime = timer + 5f; // 粘住后最多再存在5秒
            Debug.Log($"麻醉镖粘住目标，剩余生命周期调整为 {lifetime - timer:F1} 秒");
        }
    }

    /// <summary>
    /// 创建视觉特效
    /// </summary>
    private void CreateEffect(Vector3 position, GameObject effectPrefab)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    /// <summary>
    /// 销毁自己
    /// </summary>
    private void DestroySelf()
    {
        Debug.Log($"麻醉镖生命周期结束，销毁 (存在了 {timer:F1}/{lifetime:F1} 秒)");

        // 创建销毁特效
        CreateEffect(transform.position, destroyEffect);

        Destroy(gameObject);
    }

    /// <summary>
    /// 外部调用：立即销毁
    /// </summary>
    public void ForceDestroy()
    {
        Debug.Log("麻醉镖被强制销毁");
        DestroySelf();
    }

    /// <summary>
    /// 检查是否已经击中目标
    /// </summary>
    public bool HasHit()
    {
        return hasHit;
    }

    /// <summary>
    /// 获取剩余生命时间
    /// </summary>
    public float GetRemainingLifetime()
    {
        return Mathf.Max(0f, lifetime - timer);
    }

    /// <summary>
    /// 获取生存时间百分比
    /// </summary>
    public float GetLifetimePercentage()
    {
        return Mathf.Clamp01(timer / lifetime);
    }

    /// <summary>
    /// 设置新的生命周期（可在运行时调用）
    /// </summary>
    public void SetLifetime(float newLifetime)
    {
        if (newLifetime > 0)
        {
            lifetime = newLifetime;
            Debug.Log($"麻醉镖生命周期更新为 {lifetime} 秒");
        }
    }

    /// <summary>
    /// 延长生命周期
    /// </summary>
    public void ExtendLifetime(float additionalTime)
    {
        if (additionalTime > 0)
        {
            lifetime += additionalTime;
            Debug.Log($"麻醉镖生命周期延长 {additionalTime} 秒，新周期: {lifetime} 秒");
        }
    }

    // 调试显示
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !showDebugGizmos) return;

        // 显示生命周期状态
        float remaining = GetRemainingLifetime();
        float percentage = GetLifetimePercentage();

        // 根据剩余时间改变颜色
        Color gizmoColor;
        if (remaining > lifetime * 0.5f)
            gizmoColor = Color.green;
        else if (remaining > lifetime * 0.2f)
            gizmoColor = Color.yellow;
        else
            gizmoColor = Color.red;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // 显示飞行方向
        if (!hasHit && rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        }

        // 显示生命周期进度
        if (hasHit)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f,
                               new Vector3(1f * (1f - percentage), 0.1f, 0.1f));
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying || !showDebugUI) return;  // 添加开关控制

        // 在Scene视图中显示调试信息
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width &&
                screenPos.y > 0 && screenPos.y < Screen.height)
            {
                float remaining = GetRemainingLifetime();
                string debugText = $"Dart: {remaining:F1}s";

                GUI.Label(new Rect(screenPos.x - 30, Screen.height - screenPos.y - 10, 60, 20),
                         debugText, GUI.skin.box);
            }
        }
    }

}