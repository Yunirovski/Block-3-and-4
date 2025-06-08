// Assets/Scripts/Player/GrappleController.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// 钩爪控制器 - 拉拽版本
/// 玩家抓住物体后会被拉向目标
/// </summary>
public class GrappleController : MonoBehaviour
{
    [Header("钩爪设置")]
    [HideInInspector] public GameObject hookPrefab;
    [HideInInspector] public float hookSpeed = 80f; // 增加飞行速度
    [HideInInspector] public Material ropeMaterial;
    [HideInInspector] public Color ropeColor = new Color(0.545f, 0.271f, 0.075f);

    [Header("钩爪物理")]
    [Tooltip("钩爪质量")]
    public float hookMass = 2f;
    [Tooltip("重力强度")]
    public float gravity = 15f;
    [Tooltip("绳索最大长度")]
    public float maxRopeLength = 40f;

    [Header("拉拽设置")]
    [Tooltip("拉拽速度")]
    public float pullSpeed = 20f; // 增加拉拽速度
    [Tooltip("拉拽加速度")]
    public float pullAcceleration = 25f; // 增加拉拽加速度
    [Tooltip("到达目标的距离阈值")]
    public float arrivalDistance = 3f;
    [Tooltip("是否到达后自动释放")]
    public bool autoReleaseOnArrival = true;
    [Tooltip("拉拽时保持的重力")]
    public float pullGravity = 5f;
    [Tooltip("向上拉拽时的额外力度")]
    public float upwardPullBoost = 1.5f; // 新增：向上拉拽增强

    [Header("控制设置")]
    [Tooltip("是否允许控制拉拽方向")]
    public bool allowDirectionalControl = true;
    [Tooltip("方向控制力度")]
    public float directionalForce = 8f;

    [Header("音效")]
    [Tooltip("钩爪发射音效")]
    public AudioClip hookFireSound;
    [Tooltip("钩爪命中音效")]
    public AudioClip hookHitSound;
    [Tooltip("拉拽开始音效")]
    public AudioClip pullStartSound;
    [Tooltip("钩爪脱落音效")]
    public AudioClip hookDetachSound;

    [Header("视觉效果")]
    [Tooltip("钩爪命中特效")]
    public GameObject hookImpactEffect;
    [Tooltip("拉拽特效")]
    public GameObject pullEffect;
    [Tooltip("到达特效")]
    public GameObject arrivalEffect;

    // 组件引用
    private CharacterController controller;
    private LineRenderer lineRenderer;
    private AudioSource audioSource;

    // 钩爪状态
    private GameObject hookInstance;
    private bool isGrappling = false;
    private bool isHookFlying = false;
    private bool isHookAttached = false;
    private bool isPulling = false;

    // 物理变量
    private Vector3 hookVelocity;
    private Vector3 hookPosition;
    private Vector3 attachPoint;
    private Vector3 pullVelocity;
    private float currentPullSpeed;
    private float currentRopeLength;

    // 计时器和安全限制
    private float grappleTimer;
    private float maxGrappleTime = 20f;

    public void Initialize()
    {
        // 获取组件
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            Debug.Log("GrappleController: 添加了缺失的CharacterController组件");
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }

        // 设置LineRenderer
        SetupLineRenderer();

        Debug.Log("钩爪控制器初始化完成 - 拉拽模式");
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.03f;

        if (ropeMaterial != null)
        {
            lineRenderer.material = ropeMaterial;
        }

        lineRenderer.startColor = ropeColor;
        lineRenderer.endColor = ropeColor;
        lineRenderer.enabled = false;
        lineRenderer.useWorldSpace = true;
    }

    void Update()
    {
        if (isGrappling || isHookFlying)
        {
            grappleTimer += Time.deltaTime;

            // 安全超时检查
            if (grappleTimer > maxGrappleTime)
            {
                Debug.Log("钩爪超时，自动停止");
                StopGrapple();
                return;
            }

            // 更新绳索显示
            UpdateRopeVisuals();
        }

        if (isHookFlying)
        {
            UpdateHookFlight();
        }
        else if (isHookAttached)
        {
            if (isPulling)
            {
                UpdatePulling();
            }
            HandlePlayerInput();
        }
    }

    public void StartGrapple(Vector3 targetPoint, float pullSpeedOverride = 0f)
    {
        // 如果已经在抓钩，先停止
        if (isGrappling || isHookFlying)
        {
            StopGrapple();
        }

        Debug.Log($"开始发射钩爪到: {targetPoint}");

        // 使用覆盖的拉拽速度（如果提供）
        if (pullSpeedOverride > 0)
        {
            pullSpeed = pullSpeedOverride;
        }

        // 计算发射起点
        Vector3 firePoint = Camera.main.transform.position;

        // 创建钩爪实例
        if (hookPrefab != null)
        {
            hookInstance = Instantiate(hookPrefab, firePoint, Quaternion.identity);
            hookPosition = firePoint;
        }

        // 发射方向 - 允许射向任何方向，包括空中
        Vector3 direction = (targetPoint - firePoint).normalized;
        hookVelocity = direction * hookSpeed;

        // 设置状态
        isHookFlying = true;
        isGrappling = true;
        isPulling = false;
        grappleTimer = 0f;
        currentPullSpeed = 0f;

        // 启用绳索渲染
        lineRenderer.enabled = true;

        // 播放发射音效
        PlaySound(hookFireSound);

        Debug.Log($"钩爪发射：方向 {direction}, 速度 {hookVelocity.magnitude}");
    }

    private void UpdateHookFlight()
    {
        if (hookInstance == null) return;

        Vector3 oldPosition = hookPosition;

        // 应用重力和速度
        hookVelocity.y -= gravity * Time.deltaTime;
        hookPosition += hookVelocity * Time.deltaTime;

        // 检测碰撞
        if (CheckHookCollision(oldPosition, hookPosition))
        {
            return; // 钩爪已附着或已停止
        }

        // 检查是否超出最大距离
        float distanceFromPlayer = Vector3.Distance(transform.position, hookPosition);
        if (distanceFromPlayer > maxRopeLength)
        {
            Debug.Log("钩爪飞行距离超出限制");
            StopGrapple();
            return;
        }

        // 检查钩爪是否在空中飞行太久（允许更长时间）
        if (grappleTimer > 8f && !isHookAttached)
        {
            Debug.Log("钩爪飞行时间过长，自动停止");
            StopGrapple();
            return;
        }

        // 更新钩爪位置和朝向
        hookInstance.transform.position = hookPosition;
        if (hookVelocity.magnitude > 0.1f)
        {
            hookInstance.transform.rotation = Quaternion.LookRotation(hookVelocity.normalized);
        }
    }

    private bool CheckHookCollision(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;

        RaycastHit hit;
        if (Physics.Raycast(from, direction.normalized, out hit, distance))
        {
            if (CanAttachToSurface(hit.collider))
            {
                AttachHook(hit.point, hit.collider);
                return true;
            }
            else
            {
                // 钩爪弹开 - 不能附着的表面，但继续飞行
                Vector3 reflection = Vector3.Reflect(hookVelocity.normalized, hit.normal);
                hookVelocity = reflection * hookVelocity.magnitude * 0.7f; // 减少反弹损失
                hookPosition = hit.point + hit.normal * 0.5f;

                Debug.Log($"钩爪从 {hit.collider.name} 弹开 - 继续飞行");
                CreateEffect(hit.point, pullEffect);
            }
        }

        return false;
    }

    private bool CanAttachToSurface(Collider collider)
    {
        // 明确排除玩家自己
        if (collider.GetComponent<CharacterController>() != null ||
            collider.GetComponent<player_move2>() != null)
        {
            return false;
        }

        // 排除动物
        if (collider.GetComponent<AnimalBehavior>() != null)
        {
            return false;
        }

        // 检查特定可抓取标签
        if (collider.CompareTag("Grappable") ||
            collider.CompareTag("Wall") ||
            collider.CompareTag("Rock") ||
            collider.CompareTag("Building") ||
            collider.CompareTag("Ground"))
        {
            Debug.Log($"钩爪可以附着到标签物体: {collider.name} (标签: {collider.tag})");
            return true;
        }

        // 检查静态物体（地形、建筑等）
        if (collider.gameObject.isStatic)
        {
            Debug.Log($"钩爪可以附着到静态物体: {collider.name}");
            return true;
        }

        // 检查大质量刚体（重物体）
        Rigidbody rb = collider.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic && rb.mass > 100f)
        {
            Debug.Log($"钩爪可以附着到重物体: {collider.name} (质量: {rb.mass})");
            return true;
        }

        // 其他情况都不能附着
        Debug.Log($"钩爪无法附着到: {collider.name} (非有效目标)");
        return false;
    }

    private void AttachHook(Vector3 attachPoint, Collider surface)
    {
        this.attachPoint = attachPoint;
        hookPosition = attachPoint;

        if (hookInstance != null)
        {
            hookInstance.transform.position = attachPoint;
        }

        // 计算绳索长度
        currentRopeLength = Vector3.Distance(transform.position, attachPoint);

        // 状态切换
        isHookFlying = false;
        isHookAttached = true;
        isPulling = true;
        currentPullSpeed = 0f; // 从0开始加速
        pullVelocity = Vector3.zero;

        Debug.Log($"钩爪成功附着到 {surface.name}，距离: {currentRopeLength:F1}m，开始拉拽");

        // 播放音效和特效
        PlaySound(hookHitSound);
        CreateEffect(attachPoint, hookImpactEffect);

        // 延迟播放拉拽音效
        Invoke(nameof(PlayPullStartSound), 0.2f);
    }

    private void PlayPullStartSound()
    {
        PlaySound(pullStartSound);
    }

    private void UpdatePulling()
    {
        Vector3 playerPos = transform.position;
        Vector3 toTarget = attachPoint - playerPos;
        float distanceToTarget = toTarget.magnitude;

        // 检查是否到达目标
        if (distanceToTarget <= arrivalDistance)
        {
            if (autoReleaseOnArrival)
            {
                Debug.Log("到达目标，自动释放钩爪");
                CreateEffect(playerPos, arrivalEffect);
                StopGrapple();
                return;
            }
            else
            {
                // 停止拉拽但保持连接
                isPulling = false;
                pullVelocity = Vector3.zero;
                return;
            }
        }

        // 计算拉拽方向
        Vector3 pullDirection = toTarget.normalized;

        // 加速拉拽
        currentPullSpeed += pullAcceleration * Time.deltaTime;
        currentPullSpeed = Mathf.Min(currentPullSpeed, pullSpeed);

        // 基础拉拽速度
        pullVelocity = pullDirection * currentPullSpeed;

        // 向上拉拽时增加额外力度
        if (pullDirection.y > 0.3f) // 如果拉拽方向主要向上
        {
            float upwardComponent = pullDirection.y;
            pullVelocity += Vector3.up * (upwardComponent * upwardPullBoost * currentPullSpeed);
            Debug.Log($"向上拉拽增强: {upwardComponent:F2} x {upwardPullBoost}");
        }

        // 应用方向控制
        if (allowDirectionalControl)
        {
            ApplyDirectionalControl(ref pullVelocity);
        }

        // 应用重力（减弱版本）
        pullVelocity.y -= pullGravity * Time.deltaTime;

        // 移动玩家
        controller.Move(pullVelocity * Time.deltaTime);

        // 更新绳索长度
        currentRopeLength = Vector3.Distance(transform.position, attachPoint);

        // 显示拉拽状态
        if (UIManager.Instance != null)
        {
            string heightInfo = attachPoint.y > transform.position.y + 5f ? " [向上拉拽]" : "";
            UIManager.Instance.UpdateCameraDebugText(
                $"拉拽中 - 距离: {distanceToTarget:F1}m, 速度: {currentPullSpeed:F1}m/s{heightInfo} (Q/右键释放)");
        }
    }

    private void ApplyDirectionalControl(ref Vector3 velocity)
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            // 获取相对于相机的方向
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 计算输入方向
            Vector3 inputDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

            // 添加到拉拽速度
            velocity += inputDirection * directionalForce;
        }
    }

    private void HandlePlayerInput()
    {
        // 释放钩爪 (Q键或右键)
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            ReleaseGrapple();
        }

        // 手动重新开始拉拽（如果停止了拉拽但仍然连接）
        if (isHookAttached && !isPulling && Input.GetKeyDown(KeyCode.Space))
        {
            isPulling = true;
            currentPullSpeed = 0f;
            Debug.Log("重新开始拉拽");
            PlaySound(pullStartSound);
        }
    }

    private void UpdateRopeVisuals()
    {
        if (lineRenderer == null) return;

        Vector3 playerPos = transform.position + Vector3.up * 1.5f;
        Vector3 hookPos = isHookAttached ? attachPoint : hookPosition;

        lineRenderer.SetPosition(0, playerPos);
        lineRenderer.SetPosition(1, hookPos);

        // 根据拉拽状态调整绳索颜色
        if (isPulling)
        {
            lineRenderer.startColor = Color.Lerp(ropeColor, Color.cyan, 0.5f);
            lineRenderer.endColor = Color.Lerp(ropeColor, Color.cyan, 0.5f);
        }
        else
        {
            lineRenderer.startColor = ropeColor;
            lineRenderer.endColor = ropeColor;
        }
    }

    private void ReleaseGrapple()
    {
        if (!isHookAttached) return;

        Debug.Log("主动释放钩爪");

        // 保持一些前进动量
        if (isPulling && pullVelocity.magnitude > 0)
        {
            Vector3 forwardDirection = transform.forward;
            float forwardMomentum = Vector3.Dot(pullVelocity, forwardDirection);

            // 这里可以将动量应用到玩家移动系统
            // 由于CharacterController的限制，我们通过其他方式处理
        }

        StopGrapple();
        PlaySound(hookDetachSound);
    }

    public void StopGrapple()
    {
        isGrappling = false;
        isHookFlying = false;
        isHookAttached = false;
        isPulling = false;
        grappleTimer = 0f;
        currentPullSpeed = 0f;

        // 隐藏绳索
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // 销毁钩爪
        if (hookInstance != null)
        {
            Destroy(hookInstance);
            hookInstance = null;
        }

        // 重置物理变量
        pullVelocity = Vector3.zero;
        hookVelocity = Vector3.zero;

        Debug.Log("钩爪系统已停止");
    }

    private void CreateEffect(Vector3 position, GameObject effectPrefab)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // 公共接口
    public bool IsGrappling() => isGrappling;
    public bool IsSwinging() => isHookAttached; // 兼容旧接口名
    public bool IsPulling() => isPulling;
    public float GetRopeLength() => currentRopeLength;
    public Vector3 GetAttachPoint() => attachPoint;
    public float GetPullSpeed() => currentPullSpeed;

    // 可视化调试
    private void OnDrawGizmos()
    {
        if (isHookAttached)
        {
            // 绘制附着点
            Gizmos.color = isPulling ? Color.cyan : Color.green;
            Gizmos.DrawWireSphere(attachPoint, 0.5f);

            // 绘制绳索
            Gizmos.color = isPulling ? Color.yellow : Color.gray;
            Gizmos.DrawLine(transform.position, attachPoint);

            // 绘制到达距离
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attachPoint, arrivalDistance);

            // 显示拉拽方向
            if (isPulling)
            {
                Vector3 pullDir = (attachPoint - transform.position).normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, pullDir * 5f);
            }
        }

        if (isHookFlying)
        {
            // 绘制钩爪飞行轨迹预测
            Gizmos.color = Color.red;
            Vector3 pos = hookPosition;
            Vector3 vel = hookVelocity;

            for (int i = 0; i < 10; i++)
            {
                Vector3 nextPos = pos + vel * 0.2f;
                vel.y -= gravity * 0.2f;

                Gizmos.DrawLine(pos, nextPos);
                pos = nextPos;

                if (pos.y < transform.position.y - 20f) break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制最大射程
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, maxRopeLength);

        // 绘制拉拽设置信息
        if (isHookAttached)
        {
            Vector3 labelPos = attachPoint + Vector3.up * 2f;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, 
                $"距离: {currentRopeLength:F1}m\n" +
                $"拉拽速度: {currentPullSpeed:F1}m/s\n" +
                $"状态: {(isPulling ? "拉拽中" : "已停止")}");
#endif
        }
    }
}