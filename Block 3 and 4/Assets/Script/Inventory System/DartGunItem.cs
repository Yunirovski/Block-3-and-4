// Assets/Scripts/Items/DartGunItem.cs - 绝对精准版本

using UnityEngine;

[CreateAssetMenu(menuName = "Items/DartGunItem")]
public class DartGunItem : BaseItem
{
    [Header("Dart Settings")]
    [Tooltip("Dart prefab")]
    public GameObject dartPrefab;

    [Tooltip("How far the dart is shot")]
    public float spawnDistance = 2f;

    [Tooltip("How strong the throw is")]
    public float throwForce = 25f;

    [Tooltip("How long the target is stunned")]
    public float stunDuration = 10f;

    [Header("Fire Point Settings")]
    [Tooltip("发射点高度偏移（相对于相机）")]
    public float firePointHeightOffset = 0.2f;

    [Tooltip("发射点前方偏移")]
    public float firePointForwardOffset = 0.5f;

    [Tooltip("发射角度向上偏移（度数） - 设为0确保绝对精准")]
    [Range(-10f, 45f)]
    public float upwardAngle = 0f; // 改为0，确保精准

    [Header("Dart Lifetime")]
    [Tooltip("Dart存在时间（秒）")]
    public float dartLifetime = 7f;

    [Header("Cooldown Settings")]
    [Tooltip("Time before shooting again (seconds)")]
    public float cooldownTime = 1f;

    [Header("Audio")]
    [Tooltip("Sound when shooting")]
    public AudioClip fireSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Precision Settings")]
    [Tooltip("启用绝对精准模式（移除所有随机因素）")]
    public bool absolutePrecision = true;

    [Tooltip("显示弹道预测线")]
    public bool showTrajectoryPreview = true;

    [Tooltip("固定物理设置（确保一致性）")]
    public bool useFixedPhysics = true;

    // These variables are not saved in the asset file
    [System.NonSerialized] private Camera playerCamera;
    [System.NonSerialized] private AudioSource audioSource;
    [System.NonSerialized] private float nextFireTime = 0f;
    [System.NonSerialized] private bool isInitialized = false;

    // 精准射击相关变量
    [System.NonSerialized] private Vector3 lastKnownFirePoint;
    [System.NonSerialized] private Vector3 lastKnownFireDirection;

    public override void OnSelect(GameObject model)
    {
        ForceReinitialize();
        Debug.Log("精准麻醉镖已装备 - 绝对精准模式");
    }

    public override void OnReady()
    {
        EnsureValidReferences();
        UpdateUI();
    }

    public override void OnUse()
    {
        FireDart();
    }

    public override void HandleUpdate()
    {
        if (!isInitialized)
        {
            ForceReinitialize();
        }

        EnsureValidReferences();
        UpdateCooldownState();
        UpdateUI();

        // 记录当前瞄准状态（可选，主要用于调试）
        if (playerCamera != null)
        {
            UpdateAimCache();
        }
    }

    private void ForceReinitialize()
    {
        playerCamera = null;
        audioSource = null;
        nextFireTime = 0f;
        lastKnownFirePoint = Vector3.zero;
        lastKnownFireDirection = Vector3.forward;

        EnsureValidReferences();
        isInitialized = true;
        Debug.Log("精准镖枪: 重新初始化完成");
    }

    private void EnsureValidReferences()
    {
        if (playerCamera == null || !IsValidUnityObject(playerCamera))
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerCamera = playerObj.GetComponentInChildren<Camera>();
                }
            }
        }

        if (playerCamera != null && (audioSource == null || !IsValidUnityObject(audioSource)))
        {
            audioSource = playerCamera.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = playerCamera.gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
            }
        }
    }

    private bool IsValidUnityObject(UnityEngine.Object obj)
    {
        return obj != null && obj;
    }

    private void UpdateCooldownState()
    {
        // 使用更精确的时间计算
        // 简化冷却逻辑，避免时间不一致
    }

    private bool IsOnCooldown()
    {
        return Time.time < nextFireTime;
    }

    private float GetRemainingCooldown()
    {
        return Mathf.Max(0f, nextFireTime - Time.time);
    }

    private void UpdateUI()
    {
        if (UIManager.Instance == null) return;

        if (IsOnCooldown())
        {
            float remainingTime = GetRemainingCooldown();
            UIManager.Instance.UpdateCameraDebugText($"精准模式冷却: {remainingTime:F1}s");
        }
        else
        {
            UIManager.Instance.UpdateCameraDebugText($"精准麻醉镖就绪 - 左键射击 (生命周期: {dartLifetime}s)");
        }
    }

    /// <summary>
    /// 记录当前瞄准状态（用于调试和状态跟踪）
    /// </summary>
    private void UpdateAimCache()
    {
        if (playerCamera == null) return;

        // 记录当前状态，但始终使用实时计算
        lastKnownFirePoint = GetPreciseFirePoint();
        lastKnownFireDirection = GetPreciseFireDirection();
    }

    /// <summary>
    /// 获取绝对精确的发射点
    /// </summary>
    private Vector3 GetPreciseFirePoint()
    {
        if (playerCamera == null) return Vector3.zero;

        Vector3 cameraPos = playerCamera.transform.position;
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraUp = playerCamera.transform.up;

        // 使用固定的偏移计算，确保每次都完全一致
        Vector3 firePoint = cameraPos
                          + cameraForward * firePointForwardOffset
                          + cameraUp * firePointHeightOffset;

        return firePoint;
    }

    /// <summary>
    /// 获取绝对精确的发射方向
    /// </summary>
    private Vector3 GetPreciseFireDirection()
    {
        if (playerCamera == null) return Vector3.forward;

        Vector3 baseDirection = playerCamera.transform.forward;

        // 如果需要向上偏移，使用精确计算
        if (Mathf.Abs(upwardAngle) > 0.001f)
        {
            Vector3 cameraRight = playerCamera.transform.right;
            Quaternion upwardRotation = Quaternion.AngleAxis(upwardAngle, cameraRight);
            baseDirection = upwardRotation * baseDirection;
        }

        return baseDirection.normalized;
    }

    private void FireDart()
    {
        if (!isInitialized)
        {
            ForceReinitialize();
        }

        EnsureValidReferences();

        if (playerCamera == null || !IsValidUnityObject(playerCamera))
        {
            Debug.LogError("精准镖枪: 无法找到有效相机");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("错误: 找不到相机");
            }
            return;
        }

        if (IsOnCooldown())
        {
            float remainingTime = GetRemainingCooldown();
            Debug.Log($"精准镖枪: 冷却中: {remainingTime:F1}s");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"冷却中: {remainingTime:F1}s");
            }
            return;
        }

        if (dartPrefab == null)
        {
            Debug.LogError("精准镖枪: dartPrefab 未设置!");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("错误: 镖预制体未设置");
            }
            return;
        }

        // 实时计算精确位置和方向，确保跟随玩家
        Vector3 spawnPosition = GetPreciseFirePoint();
        GameObject thrownDart = Instantiate(dartPrefab, spawnPosition, Quaternion.identity);

        if (thrownDart == null)
        {
            Debug.LogError("精准镖枪: 无法创建镖");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("错误: 无法创建镖");
            }
            return;
        }

        SetupPreciseDart(thrownDart);
        ApplyPreciseThrowForce(thrownDart);
        StartCooldown();
        PlayFireSound();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText($"精准射击! 冷却: {cooldownTime}s, 镖生命: {dartLifetime}s");
        }

        Debug.Log($"精准镖枪: 精确射击于 {spawnPosition}, 方向 {GetPreciseFireDirection()}, 生命周期 {dartLifetime}s");
    }

    private void StartCooldown()
    {
        nextFireTime = Time.time + cooldownTime;
        Debug.Log($"精准镖枪: 开始冷却: {cooldownTime}s");
    }

    /// <summary>
    /// 设置绝对精确的镖属性
    /// </summary>
    private void SetupPreciseDart(GameObject thrownDart)
    {
        Rigidbody rb = thrownDart.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = thrownDart.AddComponent<Rigidbody>();
            Debug.Log("精准镖枪: 添加了Rigidbody到镖");
        }

        if (useFixedPhysics)
        {
            // 使用完全固定的物理设置，确保绝对一致性
            rb.mass = 0.2f;                    // 固定质量
            rb.linearDamping = 0.0f;           // 完全移除空气阻力
            rb.angularDamping = 1.0f;          // 高角阻力防止旋转
            rb.useGravity = true;              // 启用重力
            rb.isKinematic = false;

            // 重要：重置所有物理状态，确保干净的初始状态
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            // 原有设置
            rb.mass = 0.2f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.5f;
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        // 设置精确的碰撞体
        Collider col = thrownDart.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsuleCol = thrownDart.AddComponent<CapsuleCollider>();
            capsuleCol.radius = 0.1f;
            capsuleCol.height = 0.5f;
            capsuleCol.direction = 2;  // Z轴方向
            capsuleCol.isTrigger = false;
            Debug.Log("精准镖枪: 添加了精确碰撞体");
        }
        else
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        // 设置镖脚本
        DartProjectile dartScript = thrownDart.GetComponent<DartProjectile>();
        if (dartScript == null)
        {
            dartScript = thrownDart.AddComponent<DartProjectile>();
            Debug.Log("精准镖枪: 添加了DartProjectile脚本");
        }

        dartScript.stunDuration = stunDuration;
        dartScript.lifetime = dartLifetime;
    }

    /// <summary>
    /// 应用绝对精确的投掷力
    /// </summary>
    private void ApplyPreciseThrowForce(GameObject thrownDart)
    {
        Rigidbody rb = thrownDart.GetComponent<Rigidbody>();
        if (rb == null) return;

        // 实时计算精确方向，确保跟随当前瞄准
        Vector3 throwDirection = GetPreciseFireDirection();

        // 应用精确的力，使用VelocityChange确保质量不影响速度
        rb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);

        if (absolutePrecision)
        {
            // 绝对精准模式：完全移除随机旋转
            Debug.Log("精准镖枪: 绝对精准模式 - 无随机因素");
        }
        else
        {
            // 如果不是绝对精准模式，添加极小的随机性
            Vector3 minimalRandomTorque = new Vector3(
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f)
            );
            rb.AddTorque(minimalRandomTorque, ForceMode.VelocityChange);
        }

        // 设置精确的初始朝向
        thrownDart.transform.rotation = Quaternion.LookRotation(throwDirection);

        Debug.Log($"精准镖枪: 精确投掷 - 方向: {throwDirection}, 角度: {upwardAngle}°, 力度: {throwForce}");
    }

    private void PlayFireSound()
    {
        EnsureValidReferences();

        if (fireSound != null && audioSource != null && IsValidUnityObject(audioSource))
        {
            audioSource.PlayOneShot(fireSound, soundVolume);
        }
    }

    public void ResetState()
    {
        ForceReinitialize();
        Debug.Log("精准镖枪: 状态重置");
    }

    void OnDisable()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            ForceReinitialize();
        }
    }

    /// <summary>
    /// 获取当前精确发射点位置（实时计算，用于调试可视化）
    /// </summary>
    public Vector3 GetDebugFirePoint()
    {
        return GetPreciseFirePoint(); // 总是实时计算
    }

    /// <summary>
    /// 获取当前精确发射方向（实时计算，用于调试可视化）
    /// </summary>
    public Vector3 GetDebugFireDirection()
    {
        return GetPreciseFireDirection(); // 总是实时计算
    }

    // 可视化调试 - 显示精确瞄准线（实时跟随玩家）
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 确保相机引用有效
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        // 实时显示精确发射点（跟随玩家移动和视角）
        Vector3 firePoint = GetDebugFirePoint();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(firePoint, 0.15f);

        // 实时显示精确发射方向（跟随玩家视角）
        Vector3 fireDirection = GetDebugFireDirection();
        Gizmos.color = Color.red;
        Gizmos.DrawRay(firePoint, fireDirection * 10f);

        if (showTrajectoryPreview)
        {
            // 绘制实时弹道轨迹预测（跟随当前瞄准）
            DrawPreciseTrajectory(firePoint, fireDirection * throwForce);
        }

        // 显示与默认发射点的对比
        Vector3 defaultFirePoint = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(defaultFirePoint, 0.05f);

        // 连线显示偏移差异
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(firePoint, defaultFirePoint);
    }

    /// <summary>
    /// 绘制精确的弹道轨迹
    /// </summary>
    private void DrawPreciseTrajectory(Vector3 startPos, Vector3 initialVelocity)
    {
        Vector3 pos = startPos;
        Vector3 velocity = initialVelocity;

        Gizmos.color = Color.yellow;

        float timeStep = 0.1f;
        int maxSteps = 50;

        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 nextPos = pos + velocity * timeStep;
            velocity.y -= 9.81f * timeStep; // 重力加速度

            Gizmos.DrawLine(pos, nextPos);
            pos = nextPos;

            // 如果镖落地了就停止绘制
            if (pos.y < playerCamera.transform.position.y - 20f)
                break;
        }
    }
}