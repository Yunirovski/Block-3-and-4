// Assets/Scripts/Projectiles/DartProjectile.cs - 简化版本
using UnityEngine;

/// <summary>
/// 简化的麻醉镖物理和碰撞处理
/// 像扔苹果一样简单，但命中动物时有昏迷效果
/// </summary>
public class DartProjectile : MonoBehaviour
{
    [Header("麻醉镖设置")]
    [Tooltip("昏迷持续时间")]
    public float stunDuration = 10f;

    [Tooltip("镖的生命周期（秒）")]
    public float lifetime = 30f;

    [Tooltip("击中动物时的音效")]
    public AudioClip hitAnimalSound;

    [Tooltip("击中其他物体时的音效")]
    public AudioClip hitObjectSound;

    [Range(0f, 1f)]
    [Tooltip("音效音量")]
    public float soundVolume = 0.8f;

    // 内部状态
    private bool hasHit = false;
    private AudioSource audioSource;
    private Rigidbody rb;
    private float spawnTime;

    void Start()
    {
        // 记录生成时间
        spawnTime = Time.time;

        // 获取组件
        rb = GetComponent<Rigidbody>();

        // 添加音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D音效
            audioSource.playOnAwake = false;
        }

        // 设置自动销毁
        Destroy(gameObject, lifetime);

        Debug.Log($"麻醉镖已生成，将在 {lifetime} 秒后自动销毁");
    }

    void Update()
    {
        // 让镖朝向飞行方向（如果还在飞行中）
        if (!hasHit && rb != null && rb.linearVelocity.magnitude > 0.5f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return; // 避免重复处理

        hasHit = true;
        Vector3 hitPoint = collision.contacts[0].point;
        Collider hitCollider = collision.collider;

        Debug.Log($"麻醉镖击中: {hitCollider.name}");

        // 停止物理运动
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 检查是否击中动物
        AnimalBehavior animal = hitCollider.GetComponent<AnimalBehavior>();
        if (animal != null)
        {
            HandleAnimalHit(animal, hitPoint);
        }
        else
        {
            HandleObjectHit(hitCollider, hitPoint);
        }

        // 让镖稍微嵌入表面
        EmbedInSurface(collision);
    }

    /// <summary>
    /// 处理击中动物
    /// </summary>
    private void HandleAnimalHit(AnimalBehavior animal, Vector3 hitPoint)
    {
        Debug.Log($"麻醉镖击中动物: {animal.name}，昏迷 {stunDuration} 秒");

        // 让动物昏迷
        animal.Stun(stunDuration);

        // 播放击中动物音效
        PlaySound(hitAnimalSound);

        // 显示效果信息
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText($"击中 {animal.name}! 昏迷 {stunDuration}s");
        }

        // 镖在击中动物后很快消失
        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// 处理击中其他物体
    /// </summary>
    private void HandleObjectHit(Collider hitCollider, Vector3 hitPoint)
    {
        Debug.Log($"麻醉镖击中物体: {hitCollider.name}");

        // 播放击中物体音效
        PlaySound(hitObjectSound);

        // 镖会留在地上一段时间
        // 已经通过Start()中的Destroy设置了自动销毁
    }

    /// <summary>
    /// 让镖嵌入表面
    /// </summary>
    private void EmbedInSurface(Collision collision)
    {
        if (collision.contacts.Length == 0) return;

        Vector3 normal = collision.contacts[0].normal;
        Vector3 hitPoint = collision.contacts[0].point;

        // 将镖稍微嵌入表面
        transform.position = hitPoint - normal * 0.05f;

        // 让镖朝向撞击法线的反方向
        transform.rotation = Quaternion.LookRotation(-normal);

        // 如果击中的是会移动的物体，将镖附加到上面
        Rigidbody hitRb = collision.collider.GetComponent<Rigidbody>();
        if (hitRb != null && !hitRb.isKinematic)
        {
            transform.SetParent(collision.transform);
            Debug.Log($"麻醉镖附加到移动物体: {collision.collider.name}");
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    void OnDestroy()
    {
        // 从活跃列表中移除
        DartGunItem.RemoveDartFromActiveList(gameObject);

        Debug.Log("麻醉镖已销毁");
    }

    /// <summary>
    /// 检查镖是否还在飞行中
    /// </summary>
    public bool IsFlying()
    {
        return !hasHit && rb != null && rb.linearVelocity.magnitude > 0.5f;
    }

    /// <summary>
    /// 获取镖的飞行时间
    /// </summary>
    public float GetFlightTime()
    {
        return Time.time - spawnTime;
    }

    /// <summary>
    /// 强制销毁镖（外部调用）
    /// </summary>
    public void ForceDestroy()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    // 调试可视化
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 显示镖的状态
        Gizmos.color = hasHit ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // 如果还在飞行，显示速度向量
        if (IsFlying())
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // 显示详细信息
        if (Application.isPlaying)
        {
            // 显示生命周期进度
            float remainingTime = lifetime - (Time.time - spawnTime);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, remainingTime / lifetime);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
                $"飞行时间: {GetFlightTime():F1}s\n" +
                $"剩余时间: {remainingTime:F1}s\n" +
                $"状态: {(hasHit ? "已击中" : "飞行中")}\n" +
                $"速度: {(rb != null ? rb.linearVelocity.magnitude.ToString("F1") : "0")} m/s");
#endif
        }
    }
}