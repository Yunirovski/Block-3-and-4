// Assets/Scripts/Items/DartGunItem.cs - 修复ScriptableObject状态残留问题
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

    // 运行时状态 - 添加NonSerialized防止状态残留
    [System.NonSerialized] private Camera playerCamera;
    [System.NonSerialized] private AudioSource audioSource;
    [System.NonSerialized] private float nextFireTime = 0f;

    // 防暂停问题：使用实时时间
    [System.NonSerialized] private float lastRealTime = 0f;
    [System.NonSerialized] private float cooldownStartTime = 0f;
    [System.NonSerialized] private bool isCoolingDown = false;
    [System.NonSerialized] private bool isInitialized = false;

    public override void OnSelect(GameObject model)
    {
        // 强制重新初始化
        ForceReinitialize();
        Debug.Log("麻醉枪已选中 - 子弹无限");
    }

    public override void OnReady()
    {
        // 每次Ready时重新检查引用
        EnsureValidReferences();
        UpdateUI();
    }

    public override void OnUse()
    {
        FireDart();
    }

    public override void HandleUpdate()
    {
        // 确保初始化
        if (!isInitialized)
        {
            ForceReinitialize();
        }

        // 每帧都检查引用（性能开销很小）
        EnsureValidReferences();

        // 更新冷却状态（使用实时时间）
        UpdateCooldownState();

        // 更新UI
        UpdateUI();
    }

    /// <summary>
    /// 强制重新初始化所有运行时状态
    /// </summary>
    private void ForceReinitialize()
    {
        // 清空所有引用
        playerCamera = null;
        audioSource = null;

        // 重置冷却状态
        isCoolingDown = false;
        cooldownStartTime = 0f;
        nextFireTime = 0f;

        // 重新获取引用
        EnsureValidReferences();

        isInitialized = true;
        Debug.Log("DartGun: 强制重新初始化完成");
    }

    /// <summary>
    /// 确保所有引用都有效（防暂停问题）
    /// </summary>
    private void EnsureValidReferences()
    {
        // 检查相机引用是否有效
        if (playerCamera == null || !IsValidUnityObject(playerCamera))
        {
            playerCamera = null; // 确保清空无效引用

            // 重新获取相机引用
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                // 尝试通过GameObject.Find寻找
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerCamera = playerObj.GetComponentInChildren<Camera>();
                }
            }

            if (playerCamera != null)
            {
                Debug.Log("DartGun: 重新获取到相机引用");
            }
        }

        // 检查音频源引用是否有效
        if (playerCamera != null && (audioSource == null || !IsValidUnityObject(audioSource)))
        {
            audioSource = null; // 确保清空无效引用

            audioSource = playerCamera.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = playerCamera.gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
            }

            if (audioSource != null)
            {
                Debug.Log("DartGun: 重新获取到音频源引用");
            }
        }
    }

    /// <summary>
    /// 检查Unity对象是否仍然有效（未被销毁）
    /// </summary>
    private bool IsValidUnityObject(UnityEngine.Object obj)
    {
        // Unity的null检查，会检查对象是否被销毁
        return obj != null && obj;
    }

    /// <summary>
    /// 更新冷却状态（使用实时时间，不受Time.timeScale影响）
    /// </summary>
    private void UpdateCooldownState()
    {
        if (isCoolingDown)
        {
            float realTimeElapsed = Time.realtimeSinceStartup - cooldownStartTime;
            if (realTimeElapsed >= cooldownTime)
            {
                isCoolingDown = false;
                Debug.Log("DartGun: 冷却结束");
            }
        }
    }

    /// <summary>
    /// 获取剩余冷却时间
    /// </summary>
    private float GetRemainingCooldown()
    {
        if (!isCoolingDown) return 0f;

        float realTimeElapsed = Time.realtimeSinceStartup - cooldownStartTime;
        return Mathf.Max(0f, cooldownTime - realTimeElapsed);
    }

    /// <summary>
    /// 检查是否在冷却中
    /// </summary>
    private bool IsOnCooldown()
    {
        return isCoolingDown && GetRemainingCooldown() > 0f;
    }

    private void UpdateUI()
    {
        if (IsOnCooldown())
        {
            float remainingTime = GetRemainingCooldown();
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

    /// <summary>
    /// 射击麻醉镖
    /// </summary>
    private void FireDart()
    {
        // 确保初始化
        if (!isInitialized)
        {
            ForceReinitialize();
        }

        // 确保引用有效
        EnsureValidReferences();

        // 详细的错误检查和反馈
        if (playerCamera == null || !IsValidUnityObject(playerCamera))
        {
            Debug.LogError("DartGun: 找不到有效的玩家相机，尝试重新获取...");

            // 强制重新查找相机
            playerCamera = null;
            EnsureValidReferences();

            if (playerCamera == null)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateCameraDebugText("错误：找不到玩家相机");
                }
                return;
            }
            else
            {
                Debug.Log("DartGun: 成功重新获取相机引用");
            }
        }

        // 检查冷却时间
        if (IsOnCooldown())
        {
            float remainingTime = GetRemainingCooldown();
            Debug.Log($"DartGun: 冷却中，剩余时间: {remainingTime:F1}s");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"冷却中: {remainingTime:F1}s");
            }
            return;
        }

        // 检查预制体
        if (dartPrefab == null)
        {
            Debug.LogError("DartGun: 没有设置麻醉镖预制体！请在Inspector中设置dartPrefab");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("错误：未设置麻醉镖预制体");
            }
            return;
        }

        // 生成位置（玩家前方）
        Vector3 spawnPosition = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;

        // 创建麻醉镖
        GameObject thrownDart = Instantiate(dartPrefab, spawnPosition, Quaternion.identity);

        if (thrownDart == null)
        {
            Debug.LogError("DartGun: 无法实例化麻醉镖");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("错误：无法生成麻醉镖");
            }
            return;
        }

        // 设置麻醉镖
        SetupThrownDart(thrownDart);

        // 应用抛掷力
        ApplyThrowForce(thrownDart);

        // 设置冷却时间（使用实时时间）
        StartCooldown();

        // 播放音效
        PlayFireSound();

        // 成功反馈
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText($"发射成功！冷却 {cooldownTime}s");
        }

        Debug.Log($"DartGun: 成功发射麻醉镖到 {spawnPosition}");
    }

    /// <summary>
    /// 开始冷却（使用实时时间）
    /// </summary>
    private void StartCooldown()
    {
        isCoolingDown = true;
        cooldownStartTime = Time.realtimeSinceStartup;
        Debug.Log($"DartGun: 开始冷却 {cooldownTime}s，开始时间: {cooldownStartTime}");
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
        rb.mass = 0.2f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.5f;
        rb.useGravity = true;
        rb.isKinematic = false;

        // 确保有Collider
        Collider col = thrownDart.GetComponent<Collider>();
        if (col == null)
        {
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
        dartScript.lifetime = 30f;
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
        throwDirection.y += 0.1f;
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
        // 确保音频源有效
        EnsureValidReferences();

        if (fireSound != null && audioSource != null && IsValidUnityObject(audioSource))
        {
            audioSource.PlayOneShot(fireSound, soundVolume);
        }
    }

    /// <summary>
    /// 重置状态（供外部调用，比如场景重新加载时）
    /// </summary>
    public void ResetState()
    {
        ForceReinitialize();
        Debug.Log("DartGun: 状态已重置");
    }

    /// <summary>
    /// Unity编辑器中停止播放时调用（仅在编辑器中有效）
    /// </summary>
    void OnDisable()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            // 编辑器中停止播放时清空运行时状态
            ForceReinitialize();
        }
    }
}