// Assets/Scripts/Items/DartGunItem.cs - 简化版本，像扔苹果一样
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/DartGunItem")]
public class DartGunItem : BaseItem
{
    [Header("Dart Settings")]
    [Tooltip("Dart prefab")]
    public GameObject dartPrefab;

    [Tooltip("投掷距离")]
    public float spawnDistance = 2f;

    [Tooltip("投掷力度")]
    public float throwForce = 25f;

    [Tooltip("冷却时间")]
    public float fireCooldown = 1f;

    [Tooltip("昏迷持续时间")]
    public float stunDuration = 10f;

    [Header("Audio")]
    [Tooltip("射击音效")]
    public AudioClip fireSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    // 运行时状态
    private Camera playerCamera;
    private Transform playerTransform;
    private AudioSource audioSource;
    private float nextFireTime = 0f;

    // 管理地上的dart数量
    private static List<GameObject> activeDarts = new List<GameObject>();
    private static int maxActiveDarts = 3;

    public override void OnSelect(GameObject model)
    {
        // 获取玩家相机和位置
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerTransform = playerCamera.transform;
        }

        // 设置音频源
        audioSource = playerCamera.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = playerCamera.gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }

        Debug.Log($"DartGun 已选中");
    }

    public override void OnReady()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText("左键射击麻醉镖");
        }
    }

    public override void OnUse()
    {
        FireDart();
    }

    public override void HandleUpdate()
    {
        // 更新冷却状态
        if (Time.time < nextFireTime)
        {
            float cooldown = nextFireTime - Time.time;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"冷却中: {cooldown:F1}s");
            }
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("麻醉枪就绪 - 左键射击");
            }
        }
    }

    /// <summary>
    /// 射击麻醉镖
    /// </summary>
    private void FireDart()
    {
        if (playerCamera == null || playerTransform == null)
        {
            Debug.LogError("DartGun: 找不到玩家相机或位置");
            return;
        }

        // 检查冷却
        if (Time.time < nextFireTime)
        {
            float cooldown = nextFireTime - Time.time;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"冷却中: {cooldown:F1}s");
            }
            return;
        }

        // 确定要使用的预制体
        if (dartPrefab == null)
        {
            Debug.LogError("DartGun: 没有设置麻醉镖预制体");
            return;
        }

        // 生成位置（玩家前方）
        Vector3 spawnPosition = playerTransform.position + playerTransform.forward * spawnDistance;

        // 创建麻醉镖
        GameObject thrownDart = Instantiate(dartPrefab, spawnPosition, Quaternion.identity);

        // 设置麻醉镖
        SetupThrownDart(thrownDart);

        // 应用抛掷力
        ApplyThrowForce(thrownDart);

        // 管理最大数量
        ManageActiveDarts(thrownDart);

        // 设置冷却
        nextFireTime = Time.time + fireCooldown;

        // 播放音效
        PlayFireSound();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText($"射击! 冷却 {fireCooldown}s");
        }

        Debug.Log($"DartGun: 射击麻醉镖到 {spawnPosition}");
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
        Vector3 throwDirection = playerTransform.forward;
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
    /// 管理活跃的麻醉镖数量
    /// </summary>
    private void ManageActiveDarts(GameObject newDart)
    {
        // 清理无效引用
        activeDarts.RemoveAll(dart => dart == null);

        // 添加新的镖
        activeDarts.Add(newDart);

        // 如果超过最大数量，销毁最老的
        while (activeDarts.Count > maxActiveDarts)
        {
            if (activeDarts[0] != null)
            {
                Destroy(activeDarts[0]);
                Debug.Log("DartGun: 销毁了最老的麻醉镖以保持数量限制");
            }
            activeDarts.RemoveAt(0);
        }

        Debug.Log($"DartGun: 当前活跃麻醉镖数量: {activeDarts.Count}/{maxActiveDarts}");
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

    /// <summary>
    /// 从活跃列表中移除麻醉镖（当镖被销毁时调用）
    /// </summary>
    public static void RemoveDartFromActiveList(GameObject dart)
    {
        activeDarts.Remove(dart);
    }

    /// <summary>
    /// 获取当前活跃镖的数量
    /// </summary>
    public static int GetActiveDartCount()
    {
        activeDarts.RemoveAll(dart => dart == null);
        return activeDarts.Count;
    }
}