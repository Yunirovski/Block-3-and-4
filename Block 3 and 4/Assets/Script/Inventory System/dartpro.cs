// Assets/Scripts/Projectiles/DartProjectile.cs - 修改后支持鸟类的版本
using UnityEngine;

public class DartProjectile : MonoBehaviour
{
    [Header("Dart Settings")]
    [Tooltip("昏迷持续时间")]
    public float stunDuration = 10f;

    [Tooltip("镖的生命周期（秒）")]
    public float lifetime = 30f;

    [Tooltip("击中目标后是否粘住")]
    public bool stickToTarget = true;

    [Tooltip("粘住后的偏移")]
    public Vector3 stickOffset = Vector3.zero;

    // 内部状态
    private bool hasHit = false;
    private float timer = 0f;
    private Rigidbody rb;
    private Collider col;

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

        // 检查是否击中鸟类 (新增支持!)
        PigeonBehavior bird = hitObject.GetComponent<PigeonBehavior>();
        if (bird != null)
        {
            // 让鸟类昏迷
            bird.Stun(stunDuration);
            Debug.Log($"鸟类 {bird.name} 被麻醉，持续 {stunDuration} 秒");

            // 标记击中
            hasHit = true;

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

        // 成为目标的子物体（可选）
        Transform target = collision.transform;
        if (target != null)
        {
            transform.SetParent(target);
            Debug.Log($"麻醉镖粘住了 {target.name}");
        }

        // 粘住后减少生命周期
        lifetime = Mathf.Min(lifetime, 10f);
    }

    /// <summary>
    /// 销毁自己
    /// </summary>
    private void DestroySelf()
    {
        Debug.Log("麻醉镖生命周期结束，销毁");
        Destroy(gameObject);
    }

    /// <summary>
    /// 外部调用：立即销毁
    /// </summary>
    public void ForceDestroy()
    {
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

    // 调试显示
    void OnDrawGizmosSelected()
    {
        // 显示生命周期
        if (Application.isPlaying)
        {
            float remaining = GetRemainingLifetime();
            Gizmos.color = remaining > 5f ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            // 显示飞行方向
            if (!hasHit && rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
            }
        }
    }
}