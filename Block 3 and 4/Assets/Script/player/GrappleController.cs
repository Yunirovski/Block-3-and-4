// Assets/Scripts/Player/GrappleController.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// 简化且稳定的钩爪控制器
/// 修复了物体检测和轨迹计算问题
/// </summary>
public class GrappleController : MonoBehaviour
{
    [Header("钩爪设置")]
    [HideInInspector] public GameObject hookPrefab;
    [HideInInspector] public float hookSpeed = 50f;
    [HideInInspector] public Material ropeMaterial;
    [HideInInspector] public Color ropeColor = new Color(0.545f, 0.271f, 0.075f);

    [Header("钩爪物理")]
    [Tooltip("钩爪质量")]
    public float hookMass = 2f;
    [Tooltip("重力强度")]
    public float gravity = 15f;
    [Tooltip("绳索最大长度")]
    public float maxRopeLength = 40f;
    [Tooltip("绳索弹性")]
    public float ropeElasticity = 0.2f;

    [Header("摆动设置")]
    [Tooltip("摆动力度")]
    public float swingForce = 15f;
    [Tooltip("攀爬速度")]
    public float climbSpeed = 8f;
    [Tooltip("动量保持率")]
    public float momentumRetention = 0.8f;

    [Header("音效")]
    [Tooltip("钩爪发射音效")]
    public AudioClip hookFireSound;
    [Tooltip("钩爪命中音效")]
    public AudioClip hookHitSound;
    [Tooltip("绳索拉紧音效")]
    public AudioClip ropeTightSound;
    [Tooltip("钩爪脱落音效")]
    public AudioClip hookDetachSound;

    [Header("视觉效果")]
    [Tooltip("钩爪命中特效")]
    public GameObject hookImpactEffect;
    [Tooltip("火花特效")]
    public GameObject sparkEffect;
    [Tooltip("绳索拉紧特效")]
    public GameObject ropeTensionEffect;

    // 组件引用
    private CharacterController controller;
    private LineRenderer lineRenderer;
    private AudioSource audioSource;

    // 钩爪状态
    private GameObject hookInstance;
    private bool isGrappling = false;
    private bool isHookFlying = false;
    private bool isHookAttached = false;

    // 物理变量
    private Vector3 hookVelocity;
    private Vector3 hookPosition;
    private Vector3 attachPoint;
    private Vector3 playerVelocity;
    private float currentRopeLength;
    private float restRopeLength;

    // 计时器和安全限制
    private float grappleTimer;
    private float maxGrappleTime = 15f;

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
            audioSource.spatialBlend = 0f; // 全局音效
        }

        // 设置LineRenderer
        SetupLineRenderer();

        Debug.Log("钩爪控制器初始化完成");
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
            UpdateSwinging();
            HandlePlayerInput();
        }
    }

    public void StartGrapple(Vector3 targetPoint, float pullSpeed)
    {
        // 如果已经在抓钩，先停止
        if (isGrappling || isHookFlying)
        {
            StopGrapple();
        }

        Debug.Log($"开始发射钩爪到: {targetPoint}");

        // 计算发射起点
        Vector3 firePoint = Camera.main.transform.position;

        // 创建钩爪实例
        if (hookPrefab != null)
        {
            hookInstance = Instantiate(hookPrefab, firePoint, Quaternion.identity);
            hookPosition = firePoint;
        }

        // 简化的速度计算 - 直接朝目标方向发射
        Vector3 direction = (targetPoint - firePoint).normalized;
        hookVelocity = direction * hookSpeed;

        // 设置状态
        isHookFlying = true;
        isGrappling = true;
        grappleTimer = 0f;

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

        // 简化的物理：只应用重力，不考虑空气阻力
        hookVelocity.y -= gravity * Time.deltaTime;

        // 更新位置
        hookPosition += hookVelocity * Time.deltaTime;

        // 检测碰撞
        if (CheckHookCollision(oldPosition, hookPosition))
        {
            return; // 钩爪已附着
        }

        // 检查是否超出最大距离
        float distanceFromPlayer = Vector3.Distance(transform.position, hookPosition);
        if (distanceFromPlayer > maxRopeLength)
        {
            Debug.Log("钩爪飞行距离超出限制");
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

        // 使用射线检测碰撞
        RaycastHit hit;
        if (Physics.Raycast(from, direction.normalized, out hit, distance))
        {
            // 简化的表面检测 - 更宽松的条件
            if (CanAttachToSurface(hit.collider))
            {
                AttachHook(hit.point, hit.collider);
                return true;
            }
            else
            {
                // 钩爪弹开
                Vector3 reflection = Vector3.Reflect(hookVelocity.normalized, hit.normal);
                hookVelocity = reflection * hookVelocity.magnitude * 0.5f; // 减少反弹力度
                hookPosition = hit.point + hit.normal * 0.5f;

                Debug.Log($"钩爪从 {hit.collider.name} 弹开");

                // 播放弹开特效
                CreateEffect(hit.point, sparkEffect);
            }
        }

        return false;
    }

    private bool CanAttachToSurface(Collider collider)
    {
        // 大幅简化的检测逻辑

        // 1. 检查特定标签
        if (collider.CompareTag("Grappable") ||
            collider.CompareTag("Wall") ||
            collider.CompareTag("Rock") ||
            collider.CompareTag("Building"))
        {
            return true;
        }

        // 2. 检查是否是静态物体（排除玩家和动物）
        if (collider.gameObject.isStatic)
        {
            // 排除玩家
            if (collider.GetComponent<CharacterController>() != null)
                return false;

            // 排除动物
            if (collider.GetComponent<AnimalBehavior>() != null)
                return false;

            return true;
        }

        // 3. 检查是否有Rigidbody且质量足够大（重物体）
        Rigidbody rb = collider.GetComponent<Rigidbody>();
        if (rb != null && rb.mass > 50f && !rb.isKinematic)
        {
            return true;
        }

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
        restRopeLength = Vector3.Distance(transform.position, attachPoint);
        currentRopeLength = restRopeLength;

        // 确保绳索长度在合理范围内
        restRopeLength = Mathf.Clamp(restRopeLength, 2f, maxRopeLength);

        // 状态切换
        isHookFlying = false;
        isHookAttached = true;

        // 初始化摆动速度
        playerVelocity = Vector3.zero;

        Debug.Log($"钩爪成功附着到 {surface.name}，绳索长度: {restRopeLength:F1}m");

        // 播放附着音效和特效
        PlaySound(hookHitSound);
        CreateEffect(attachPoint, hookImpactEffect);
    }

    private void UpdateSwinging()
    {
        Vector3 playerPos = transform.position;
        Vector3 toAttachPoint = attachPoint - playerPos;
        currentRopeLength = toAttachPoint.magnitude;

        // 约束玩家到绳索长度内
        if (currentRopeLength > restRopeLength)
        {
            // 强制保持绳索长度
            Vector3 constrainedPos = attachPoint - toAttachPoint.normalized * restRopeLength;
            Vector3 correction = constrainedPos - playerPos;

            // 应用钟摆物理
            ApplyPendulumPhysics(toAttachPoint);

            // 移动玩家
            Vector3 movement = playerVelocity * Time.deltaTime + correction * 0.5f;
            controller.Move(movement);
        }
        else
        {
            // 绳索松弛状态 - 应用重力
            playerVelocity.y -= gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }
    }

    private void ApplyPendulumPhysics(Vector3 toAttachPoint)
    {
        // 计算径向和切线方向
        Vector3 radialDirection = toAttachPoint.normalized;
        Vector3 tangentDirection = Vector3.Cross(radialDirection, Vector3.Cross(radialDirection, Vector3.down));

        // 应用重力的切线分量（摆动力）
        float gravityComponent = Vector3.Dot(Vector3.down * gravity, tangentDirection);
        playerVelocity += tangentDirection * gravityComponent * Time.deltaTime;

        // 移除径向速度分量（防止拉伸绳索）
        float radialVelocity = Vector3.Dot(playerVelocity, radialDirection);
        if (radialVelocity > 0) // 只移除远离钩点的速度
        {
            playerVelocity -= radialDirection * radialVelocity;
        }

        // 应用阻尼
        playerVelocity *= 0.98f;

        // 限制最大速度
        if (playerVelocity.magnitude > 20f)
        {
            playerVelocity = playerVelocity.normalized * 20f;
        }
    }

    private void HandlePlayerInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 左右摆动控制
        if (Mathf.Abs(horizontal) > 0.1f)
        {
            Vector3 swingDirection = Vector3.Cross(Vector3.up, attachPoint - transform.position).normalized;
            playerVelocity += swingDirection * horizontal * swingForce * Time.deltaTime;
        }

        // 攀爬控制
        if (Mathf.Abs(vertical) > 0.1f)
        {
            float climbInput = vertical * climbSpeed * Time.deltaTime;

            // 向上攀爬
            if (climbInput > 0 && restRopeLength > 2f)
            {
                restRopeLength = Mathf.Max(2f, restRopeLength - climbInput);
            }
            // 向下放绳
            else if (climbInput < 0 && restRopeLength < maxRopeLength)
            {
                restRopeLength = Mathf.Min(maxRopeLength, restRopeLength - climbInput);
            }
        }

        // 释放钩爪 (Q键或右键)
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            ReleaseGrapple();
        }
    }

    private void UpdateRopeVisuals()
    {
        if (lineRenderer == null) return;

        Vector3 playerPos = transform.position + Vector3.up * 1.5f; // 胸部位置
        Vector3 hookPos = isHookAttached ? attachPoint : hookPosition;

        lineRenderer.SetPosition(0, playerPos);
        lineRenderer.SetPosition(1, hookPos);
    }

    private void ReleaseGrapple()
    {
        if (!isHookAttached) return;

        Debug.Log("主动释放钩爪");

        // 计算释放时的动量
        Vector3 releaseVelocity = playerVelocity * momentumRetention;

        // 这里可以将速度应用到玩家移动系统
        // 由于CharacterController的限制，我们只能在下一帧应用这个速度

        StopGrapple();
        PlaySound(hookDetachSound);
    }

    public void StopGrapple()
    {
        isGrappling = false;
        isHookFlying = false;
        isHookAttached = false;
        grappleTimer = 0f;

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
        playerVelocity = Vector3.zero;
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
    public bool IsSwinging() => isHookAttached;
    public float GetRopeLength() => currentRopeLength;
    public Vector3 GetAttachPoint() => attachPoint;

    // 可视化调试
    private void OnDrawGizmos()
    {
        if (isHookAttached)
        {
            // 绘制附着点
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(attachPoint, 0.5f);

            // 绘制绳索
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, attachPoint);

            // 绘制摆动半径
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attachPoint, restRopeLength);
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

                if (pos.y < transform.position.y - 20f) break; // 避免画太长
            }
        }
    }
}