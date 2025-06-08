// Assets/Scripts/Items/DartGunItem.cs - 简化版本，类似苹果逻辑
using UnityEngine;

[CreateAssetMenu(menuName = "Items/DartGunItem")]
public class DartGunItem : BaseItem
{
    [Header("Dart Settings")]
    [Tooltip("麻醉镖预制体")]
    public GameObject dartPrefab;

    [Tooltip("发射距离")]
    public float spawnDistance = 2f;

    [Tooltip("发射力度")]
    public float throwForce = 25f;

    [Tooltip("昏迷持续时间")]
    public float stunDuration = 10f;

    [Header("Cooldown Settings")]
    [Tooltip("冷却时间（秒）")]
    public float cooldownTime = 1f;

    [Header("Audio")]
    [Tooltip("射击音效")]
    public AudioClip fireSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    // 运行时状态
    private Camera playerCamera;
    private AudioSource audioSource;
    private float nextFireTime = 0f;

    public override void OnSelect(GameObject model)
    {
        // 获取玩家相机和位置
        playerCamera = Camera.main;

        // 设置音频源
        if (playerCamera != null)
        {
            audioSource = playerCamera.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = playerCamera.gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
            }
        }

        Debug.Log("麻醉枪已选中 - 子弹无限");
    }

    public override void OnReady()
    {
        // OnReady时立即检查状态
        if (Time.time < nextFireTime)
        {
            float remainingTime = nextFireTime - Time.time;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"麻醉枪冷却中: {remainingTime:F1}s");
            }
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("麻醉枪就绪 - 左键射击 (子弹无限)");
            }
        }
    }

    public override void OnUse()
    {
        FireDart();
    }

    public override void HandleUpdate()
    {
        // 检查冷却状态并更新UI
        if (Time.time < nextFireTime)
        {
            // 在冷却中
            float remainingTime = nextFireTime - Time.time;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"麻醉枪冷却中: {remainingTime:F1}s");
            }
        }
        else
        {
            // 可以射击
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("麻醉枪就绪 - 左键射击 (子弹无限)");
            }
        }
    }

    /// <summary>
    /// 射击麻醉镖
    /// </summary>
    private void FireDart()
    {
        if (playerCamera == null)
        {
            Debug.LogError("DartGun: 找不到玩家相机");
            return;
        }

        // 检查冷却时间
        if (Time.time < nextFireTime)
        {
            // 在冷却中，不显示任何信息
            return;
        }

        // 确定要使用的预制体
        if (dartPrefab == null)
        {
            Debug.LogError("DartGun: 没有设置麻醉镖预制体");
            return;
        }

        // 生成位置（玩家前方）
        Vector3 spawnPosition = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;

        // 创建麻醉镖
        GameObject thrownDart = Instantiate(dartPrefab, spawnPosition, Quaternion.identity);

        // 设置麻醉镖
        SetupThrownDart(thrownDart);

        // 应用抛掷力
        ApplyThrowForce(thrownDart);

        // 设置冷却时间
        nextFireTime = Time.time + cooldownTime;

        // 播放音效
        PlayFireSound();

        Debug.Log($"DartGun: 射击麻醉镖到 {spawnPosition}，下次可射击时间: {nextFireTime}");
    }

    /// <summary>
    /// 设置抛出的麻醉镖
    /// </summary>
    private void SetupThrownDart(GameObject thrownDart)
    {
        // 确保有Rigidbody
        Rigidbody rb = thrownDart.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = thrownDart.AddComponent<Rigidbody>();
            Debug.Log("DartGun: 添加了Rigidbody到麻醉镖");
        }

        // 设置Rigidbody属性
        rb.mass = 0.2f; // 轻一些
        rb.linearDamping = 0.1f; // 少量阻力
        rb.angularDamping = 0.5f;
        rb.useGravity = true;
        rb.isKinematic = false;

        // 确保有Collider
        Collider col = thrownDart.GetComponent<Collider>();
        if (col == null)
        {
            // 添加胶囊碰撞体（更像镖的形状）
            CapsuleCollider capsuleCol = thrownDart.AddComponent<CapsuleCollider>();
            capsuleCol.radius = 0.1f;
            capsuleCol.height = 0.5f;
            capsuleCol.direction = 2; // Z轴方向
            capsuleCol.isTrigger = false;
            Debug.Log("DartGun: 添加了CapsuleCollider到麻醉镖");
        }
        else
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        // 添加或获取DartProjectile脚本
        DartProjectile dartScript = thrownDart.GetComponent<DartProjectile>();
        if (dartScript == null)
        {
            dartScript = thrownDart.AddComponent<DartProjectile>();
            Debug.Log("DartGun: 添加了DartProjectile脚本");
        }

        // 设置麻醉镖属性
        dartScript.stunDuration = stunDuration;
        dartScript.lifetime = 30f; // 30秒后自动消失
    }

    /// <summary>
    /// 应用抛掷力
    /// </summary>
    private void ApplyThrowForce(GameObject thrownDart)
    {
        Rigidbody rb = thrownDart.GetComponent<Rigidbody>();
        if (rb == null) return;

        // 抛掷方向（玩家看的方向，稍微向上）
        Vector3 throwDirection = playerCamera.transform.forward;
        throwDirection.y += 0.1f; // 轻微向上
        throwDirection = throwDirection.normalized;

        // 应用力
        rb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);

        // 添加轻微旋转
        Vector3 randomTorque = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f)
        );
        rb.AddTorque(randomTorque, ForceMode.VelocityChange);

        // 让镖朝向飞行方向
        thrownDart.transform.rotation = Quaternion.LookRotation(throwDirection);

        Debug.Log($"DartGun: 麻醉镖以力度 {throwDirection * throwForce} 发射");
    }

    /// <summary>
    /// 播放射击音效
    /// </summary>
    private void PlayFireSound()
    {
        if (fireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fireSound, soundVolume);
        }
    }
}