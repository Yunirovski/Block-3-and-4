// Assets/Scripts/Items/DartGunItem.cs - 改进版本，支持发射点高度调节

using UnityEngine;

// This makes a new item you can create in the Unity menu
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

    [Tooltip("发射角度向上偏移（度数）")]
    [Range(-10f, 45f)]
    public float upwardAngle = 2f;

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

    // These variables are not saved in the asset file
    [System.NonSerialized] private Camera playerCamera;
    [System.NonSerialized] private AudioSource audioSource;
    [System.NonSerialized] private float nextFireTime = 0f;
    [System.NonSerialized] private float lastRealTime = 0f;
    [System.NonSerialized] private float cooldownStartTime = 0f;
    [System.NonSerialized] private bool isCoolingDown = false;
    [System.NonSerialized] private bool isInitialized = false;

    public override void OnSelect(GameObject model)
    {
        // Reinitialize when selected
        ForceReinitialize();
        Debug.Log("Dart gun selected - infinite ammo");
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
    }

    // Reinitialize everything
    private void ForceReinitialize()
    {
        playerCamera = null;
        audioSource = null;
        isCoolingDown = false;
        cooldownStartTime = 0f;
        nextFireTime = 0f;

        EnsureValidReferences();
        isInitialized = true;
        Debug.Log("DartGun: Reinitialized");
    }

    // Make sure camera and audio source are set
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

            if (playerCamera != null)
            {
                Debug.Log("DartGun: Camera found");
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

            if (audioSource != null)
            {
                Debug.Log("DartGun: AudioSource found");
            }
        }
    }

    private bool IsValidUnityObject(UnityEngine.Object obj)
    {
        return obj != null && obj;
    }

    private void UpdateCooldownState()
    {
        if (isCoolingDown)
        {
            float realTimeElapsed = Time.realtimeSinceStartup - cooldownStartTime;
            if (realTimeElapsed >= cooldownTime)
            {
                isCoolingDown = false;
                Debug.Log("DartGun: Cooldown ended");
            }
        }
    }

    private float GetRemainingCooldown()
    {
        if (!isCoolingDown) return 0f;
        float realTimeElapsed = Time.realtimeSinceStartup - cooldownStartTime;
        return Mathf.Max(0f, cooldownTime - realTimeElapsed);
    }

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
                UIManager.Instance.UpdateCameraDebugText($"Cooling down: {remainingTime:F1}s");
            }
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"Ready - Left click to shoot (lifetime: {dartLifetime}s)");
            }
        }
    }

    /// <summary>
    /// 计算改进的发射点位置
    /// </summary>
    private Vector3 GetFirePoint()
    {
        if (playerCamera == null) return Vector3.zero;

        Vector3 cameraPos = playerCamera.transform.position;
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraUp = playerCamera.transform.up;
        Vector3 cameraRight = playerCamera.transform.right;

        // 计算发射点：相机位置 + 前方偏移 + 高度偏移
        Vector3 firePoint = cameraPos
                          + cameraForward * firePointForwardOffset
                          + cameraUp * firePointHeightOffset;

        return firePoint;
    }

    /// <summary>
    /// 计算改进的发射方向
    /// </summary>
    private Vector3 GetFireDirection()
    {
        if (playerCamera == null) return Vector3.forward;

        Vector3 baseDirection = playerCamera.transform.forward;

        // 添加向上的角度偏移
        if (upwardAngle > 0f)
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
            Debug.LogError("DartGun: No valid camera");

            playerCamera = null;
            EnsureValidReferences();

            if (playerCamera == null)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateCameraDebugText("Error: No player camera");
                }
                return;
            }
        }

        if (IsOnCooldown())
        {
            float remainingTime = GetRemainingCooldown();
            Debug.Log($"DartGun: Cooling down: {remainingTime:F1}s");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"Cooling down: {remainingTime:F1}s");
            }
            return;
        }

        if (dartPrefab == null)
        {
            Debug.LogError("DartGun: dartPrefab not set!");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("Error: Dart prefab not set");
            }
            return;
        }

        // 使用改进的发射点计算
        Vector3 spawnPosition = GetFirePoint();
        GameObject thrownDart = Instantiate(dartPrefab, spawnPosition, Quaternion.identity);

        if (thrownDart == null)
        {
            Debug.LogError("DartGun: Cannot create dart");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("Error: Cannot create dart");
            }
            return;
        }

        SetupThrownDart(thrownDart);
        ApplyThrowForce(thrownDart);
        StartCooldown();
        PlayFireSound();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText($"Fired! Cooldown: {cooldownTime}s, Dart lifetime: {dartLifetime}s");
        }

        Debug.Log($"DartGun: Dart fired at {spawnPosition} with {dartLifetime}s lifetime");
    }

    private void StartCooldown()
    {
        isCoolingDown = true;
        cooldownStartTime = Time.realtimeSinceStartup;
        Debug.Log($"DartGun: Cooldown started: {cooldownTime}s");
    }

    private void SetupThrownDart(GameObject thrownDart)
    {
        Rigidbody rb = thrownDart.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = thrownDart.AddComponent<Rigidbody>();
            Debug.Log("DartGun: Rigidbody added to dart");
        }

        rb.mass = 0.2f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.5f;
        rb.useGravity = true;
        rb.isKinematic = false;

        Collider col = thrownDart.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsuleCol = thrownDart.AddComponent<CapsuleCollider>();
            capsuleCol.radius = 0.1f;
            capsuleCol.height = 0.5f;
            capsuleCol.direction = 2;
            capsuleCol.isTrigger = false;
            Debug.Log("DartGun: CapsuleCollider added");
        }
        else
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        DartProjectile dartScript = thrownDart.GetComponent<DartProjectile>();
        if (dartScript == null)
        {
            dartScript = thrownDart.AddComponent<DartProjectile>();
            Debug.Log("DartGun: DartProjectile script added");
        }

        // 设置dart参数，包括新的生命周期
        dartScript.stunDuration = stunDuration;
        dartScript.lifetime = dartLifetime; // 使用新的7秒生命周期
    }

    private void ApplyThrowForce(GameObject thrownDart)
    {
        Rigidbody rb = thrownDart.GetComponent<Rigidbody>();
        if (rb == null) return;

        // 使用改进的发射方向
        Vector3 throwDirection = GetFireDirection();

        rb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);

        Vector3 randomTorque = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f)
        );
        rb.AddTorque(randomTorque, ForceMode.VelocityChange);

        thrownDart.transform.rotation = Quaternion.LookRotation(throwDirection);

        Debug.Log($"DartGun: Dart thrown with improved trajectory - Direction: {throwDirection}, Angle: {upwardAngle}°");
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
        Debug.Log("DartGun: State reset");
    }

    void OnDisable()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            ForceReinitialize();
        }
    }

    /// <summary>
    /// 获取当前发射点位置（用于调试可视化）
    /// </summary>
    public Vector3 GetDebugFirePoint()
    {
        return GetFirePoint();
    }

    /// <summary>
    /// 获取当前发射方向（用于调试可视化）
    /// </summary>
    public Vector3 GetDebugFireDirection()
    {
        return GetFireDirection();
    }

    // 调试可视化
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || playerCamera == null) return;

        // 显示发射点
        Vector3 firePoint = GetFirePoint();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(firePoint, 0.1f);

        // 显示发射方向和轨迹预测
        Vector3 fireDirection = GetFireDirection();
        Gizmos.color = Color.red;
        Gizmos.DrawRay(firePoint, fireDirection * 5f);

        // 绘制轨迹预测（考虑重力）
        Vector3 pos = firePoint;
        Vector3 velocity = fireDirection * throwForce;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < 20; i++)
        {
            Vector3 nextPos = pos + velocity * 0.1f;
            velocity.y -= 9.81f * 0.1f; // 重力

            Gizmos.DrawLine(pos, nextPos);
            pos = nextPos;

            if (pos.y < playerCamera.transform.position.y - 10f) break;
        }

        // 显示原来的发射点作为对比
        Vector3 oldFirePoint = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(oldFirePoint, 0.05f);
        Gizmos.DrawLine(firePoint, oldFirePoint);
    }
}