// Assets/Scripts/Player/GrappleController.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// 真实钩爪控制器 - 模拟现实中钩爪的物理效果
/// 特性：
/// - 真实抛物线轨迹
/// - 钟摆式摆动
/// - 绳索张力和长度限制
/// - 智能边缘检测
/// - 动量保持
/// </summary>
public class GrappleController : MonoBehaviour
{
    [Header("钩爪设置")]
    [HideInInspector] public GameObject hookPrefab;
    [HideInInspector] public float hookSpeed = 50f;
    [HideInInspector] public Material ropeMaterial;
    [HideInInspector] public Color ropeColor = new Color(0.545f, 0.271f, 0.075f);

    [Header("钩爪物理")]
    [Tooltip("钩爪质量（影响抛物线轨迹）")]
    public float hookMass = 2f;
    [Tooltip("重力强度")]
    public float gravity = 15f;
    [Tooltip("空气阻力系数")]
    public float airResistance = 0.1f;
    [Tooltip("钩爪碰撞检测半径")]
    public float hookCollisionRadius = 0.3f;

    [Header("绳索物理")]
    [Tooltip("绳索最大长度")]
    public float maxRopeLength = 40f;
    [Tooltip("绳索弹性系数")]
    public float ropeElasticity = 0.2f;
    [Tooltip("绳索阻尼")]
    public float ropeDamping = 0.95f;
    [Tooltip("绳索分段数（影响绳索弯曲效果）")]
    public int ropeSegments = 20;

    [Header("摆动物理")]
    [Tooltip("摆动强度")]
    public float swingForce = 15f;
    [Tooltip("玩家在绳索上的质量")]
    public float playerMass = 70f;
    [Tooltip("摆动阻尼")]
    public float swingDamping = 0.98f;
    [Tooltip("最大摆动速度")]
    public float maxSwingSpeed = 20f;

    [Header("附着检测")]
    [Tooltip("可钩住的表面标签")]
    public string[] grappableTagsرا = { "Grappable", "Wall", "Rock", "Building" };
    [Tooltip("边缘检测距离")]
    public float edgeDetectionDistance = 2f;
    [Tooltip("钩爪穿透深度")]
    public float hookPenetration = 0.5f;
    [Tooltip("附着强度（防止钩爪脱落）")]
    public float attachmentStrength = 100f;

    [Header("玩家控制")]
    [Tooltip("攀爬速度")]
    public float climbSpeed = 8f;
    [Tooltip("释放钩爪时的动量保持")]
    public float momentumRetention = 0.8f;
    [Tooltip("最小释放高度（防止玩家在地面释放）")]
    public float minReleaseHeight = 2f;

    [Header("视觉效果")]
    [Tooltip("钩爪命中特效")]
    public GameObject hookImpactEffect;
    [Tooltip("绳索拉紧特效")]
    public GameObject ropeTensionEffect;
    [Tooltip("火花特效")]
    public GameObject sparkEffect;

    [Header("音效")]
    [Tooltip("钩爪发射音效")]
    public AudioClip hookFireSound;
    [Tooltip("钩爪命中音效")]
    public AudioClip hookHitSound;
    [Tooltip("绳索拉紧音效")]
    public AudioClip ropeTightSound;
    [Tooltip("钩爪脱落音效")]
    public AudioClip hookDetachSound;

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

    // 摆动变量
    private Vector3 pendulumCenter;
    private float pendulumAngle;
    private float pendulumAngularVelocity;
    private Vector3 lastPlayerPosition;

    // 绳索分段
    private Vector3[] ropePoints;
    private Vector3[] ropeVelocities;

    // 附着表面信息
    private IGrappable currentGrappable;
    private Collider attachedSurface;

    // 计时器
    private float grappleTimer;
    private float maxGrappleTime = 15f;

    public void Initialize()
    {
        // 获取组件
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 设置LineRenderer
        SetupLineRenderer();

        // 初始化绳索分段
        InitializeRopeSegments();

        Debug.Log("真实钩爪控制器初始化完成");
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = ropeSegments + 1;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.03f;
        lineRenderer.material = ropeMaterial;
        lineRenderer.startColor = ropeColor;
        lineRenderer.endColor = ropeColor;
        lineRenderer.enabled = false;

        // 设置绳索曲线
        lineRenderer.useWorldSpace = true;
        lineRenderer.textureMode = LineTextureMode.Tile;
    }

    private void InitializeRopeSegments()
    {
        ropePoints = new Vector3[ropeSegments + 1];
        ropeVelocities = new Vector3[ropeSegments + 1];
    }

    void Update()
    {
        if (isGrappling || isHookFlying)
        {
            grappleTimer += Time.deltaTime;

            if (grappleTimer > maxGrappleTime)
            {
                StopGrapple();
                return;
            }
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

        if (isGrappling)
        {
            UpdateRopePhysics();
            UpdateRopeVisuals();
        }
    }

    public void StartGrapple(Vector3 targetPoint, float pullSpeed)
    {
        if (isGrappling || isHookFlying)
        {
            StopGrapple();
        }

        // 计算发射点（从玩家胸部位置）
        Vector3 firePoint = transform.position + Vector3.up * 1.5f;

        // 创建钩爪
        if (hookPrefab != null)
        {
            hookInstance = Instantiate(hookPrefab, firePoint, Quaternion.identity);
            hookPosition = firePoint;
        }

        // 计算发射速度（考虑重力补偿）
        hookVelocity = CalculateLaunchVelocity(firePoint, targetPoint);

        // 初始化状态
        isHookFlying = true;
        isGrappling = true;
        grappleTimer = 0f;
        lastPlayerPosition = transform.position;

        // 启用绳索渲染
        lineRenderer.enabled = true;

        // 播放发射音效
        PlaySound(hookFireSound);

        Debug.Log($"发射钩爪: 目标 {targetPoint}, 速度 {hookVelocity.magnitude}");
    }

    private Vector3 CalculateLaunchVelocity(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;

        // 水平方向
        Vector3 horizontal = new Vector3(direction.x, 0, direction.z);
        float horizontalDistance = horizontal.magnitude;

        // 垂直方向（考虑重力补偿）
        float verticalDistance = direction.y;

        // 计算需要的初始速度
        float angle = 25f * Mathf.Deg2Rad; // 最优抛射角度
        float speed = hookSpeed;

        // 基于物理公式计算
        float vx = horizontalDistance / (distance / speed);
        float vy = verticalDistance / (distance / speed) + 0.5f * gravity * (distance / speed);

        Vector3 velocity = horizontal.normalized * vx + Vector3.up * vy;

        // 确保速度不超过最大值
        if (velocity.magnitude > hookSpeed)
        {
            velocity = velocity.normalized * hookSpeed;
        }

        return velocity;
    }

    private void UpdateHookFlight()
    {
        if (hookInstance == null) return;

        Vector3 oldPosition = hookPosition;

        // 应用重力和空气阻力
        hookVelocity.y -= gravity * Time.deltaTime;
        hookVelocity *= (1f - airResistance * Time.deltaTime);

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
        if (Physics.SphereCast(from, hookCollisionRadius, direction.normalized, out hit, distance))
        {
            // 优先检查IGrappable接口
            IGrappable grappable = hit.collider.GetComponent<IGrappable>();
            if (grappable != null && grappable.CanBeGrappled())
            {
                // 使用IGrappable的智能附着点选择
                Vector3 bestAttachPoint = grappable.GetBestGrapplePoint(hookPosition);
                AttachHook(bestAttachPoint, hit.collider, grappable);
                return true;
            }
            // 回退到传统检测方法
            else if (IsGrappableSurface(hit.collider))
            {
                // 寻找最佳附着点（边缘检测）
                Vector3 bestAttachPoint = FindBestAttachPoint(hit);
                AttachHook(bestAttachPoint, hit.collider, null);
                return true;
            }
            else
            {
                // 钩爪弹开
                Vector3 reflection = Vector3.Reflect(hookVelocity.normalized, hit.normal);
                hookVelocity = reflection * hookVelocity.magnitude * 0.6f;
                hookPosition = hit.point + hit.normal * hookCollisionRadius;

                // 播放弹开特效
                CreateImpactEffect(hit.point, false);

                Debug.Log($"钩爪无法附着到 {hit.collider.name} - 表面不可钩住");
            }
        }

        return false;
    }

    private bool IsGrappableSurface(Collider collider)
    {
        // 首先检查特定标签
        foreach (string tag in grappableTagsرا)
        {
            if (collider.CompareTag(tag))
                return true;
        }

        // 检查是否是静态物体（但排除地面）
        if (collider.gameObject.isStatic)
        {
            // 排除水平的地面
            Vector3 normal = GetSurfaceNormal(collider, hookPosition);
            float angle = Vector3.Angle(normal, Vector3.up);
            return angle > 30f; // 只有倾斜超过30度的表面才能被钩住
        }

        return false;
    }

    private Vector3 GetSurfaceNormal(Collider collider, Vector3 position)
    {
        Vector3 closestPoint = collider.ClosestPoint(position);
        Vector3 direction = (position - closestPoint).normalized;

        // 如果无法计算方向，使用向上的法线
        if (direction.magnitude < 0.1f)
        {
            return Vector3.up;
        }

        return direction;
    }

    private Vector3 FindBestAttachPoint(RaycastHit hit)
    {
        Vector3 hitPoint = hit.point;
        Vector3 hitNormal = hit.normal;

        // 尝试找到边缘或突出部分
        Vector3[] searchDirections = {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back,
            // 添加对角线方向搜索
            (Vector3.up + Vector3.forward).normalized,
            (Vector3.up + Vector3.back).normalized,
            (Vector3.up + Vector3.left).normalized,
            (Vector3.up + Vector3.right).normalized
        };

        Vector3 bestPoint = hitPoint;
        float bestScore = 0f;

        foreach (Vector3 searchDir in searchDirections)
        {
            Vector3 searchPoint = hitPoint + searchDir * edgeDetectionDistance;

            RaycastHit edgeHit;
            if (!Physics.Raycast(searchPoint, -searchDir, out edgeHit, edgeDetectionDistance * 2f))
            {
                // 找到边缘，计算评分
                float heightScore = Vector3.Dot(searchDir, Vector3.up) * 2f; // 高度加分
                float visibilityScore = Vector3.Dot(searchDir, (transform.position - searchPoint).normalized); // 朝向玩家加分
                float score = heightScore + visibilityScore;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = searchPoint;
                }
            }
        }

        // 确保附着点在表面上
        Collider collider = hit.collider;
        bestPoint = collider.ClosestPoint(bestPoint);

        return bestPoint;
    }

    private void AttachHook(Vector3 attachPoint, Collider surface, IGrappable grappable = null)
    {
        this.attachPoint = attachPoint;
        hookPosition = attachPoint;
        currentGrappable = grappable;
        attachedSurface = surface;

        if (hookInstance != null)
        {
            hookInstance.transform.position = attachPoint;
        }

        // 计算绳索长度
        restRopeLength = Vector3.Distance(transform.position, attachPoint);
        currentRopeLength = restRopeLength;

        // 确保绳索长度在合理范围内
        restRopeLength = Mathf.Clamp(restRopeLength, 2f, maxRopeLength);

        // 初始化摆动
        pendulumCenter = attachPoint;
        Vector3 toPlayer = transform.position - attachPoint;
        pendulumAngle = Mathf.Atan2(toPlayer.x, -toPlayer.y);
        pendulumAngularVelocity = 0f;

        // 状态切换
        isHookFlying = false;
        isHookAttached = true;

        // 通知可钩住表面
        if (grappable != null)
        {
            grappable.OnGrappleAttach(attachPoint);

            // 根据表面强度调整附着牢固度
            float strength = grappable.GetSurfaceStrength();
            attachmentStrength = Mathf.Max(attachmentStrength, strength * 50f);

            Debug.Log($"钩爪附着到可钩住表面: {surface.name}, 强度: {strength}");
        }
        else
        {
            Debug.Log($"钩爪附着到普通表面: {surface.name}");
        }

        // 播放附着音效和特效
        PlaySound(hookHitSound);
        CreateImpactEffect(attachPoint, true);
    }

    private void UpdateSwinging()
    {
        Vector3 playerPos = transform.position;
        Vector3 toAttachPoint = attachPoint - playerPos;
        currentRopeLength = toAttachPoint.magnitude;

        // 如果绳索拉紧，应用摆动物理
        if (currentRopeLength >= restRopeLength * 0.95f)
        {
            ApplyPendulumPhysics();
        }
        else
        {
            // 自由落体（绳索松弛）
            ApplyFreeFall();
        }

        // 应用位置
        controller.Move(playerVelocity * Time.deltaTime);

        // 更新玩家速度记录
        Vector3 currentPosition = transform.position;
        Vector3 frameVelocity = (currentPosition - lastPlayerPosition) / Time.deltaTime;
        lastPlayerPosition = currentPosition;

        // 平滑速度变化
        playerVelocity = Vector3.Lerp(playerVelocity, frameVelocity, 0.3f);
    }

    private void ApplyPendulumPhysics()
    {
        Vector3 playerPos = transform.position;
        Vector3 toAttachPoint = attachPoint - playerPos;

        // 强制保持绳索长度
        Vector3 constrainedPos = attachPoint - toAttachPoint.normalized * restRopeLength;
        Vector3 correction = constrainedPos - playerPos;

        // 计算切线方向（摆动方向）
        Vector3 radialDirection = toAttachPoint.normalized;
        Vector3 tangentDirection = Vector3.Cross(radialDirection, Vector3.Cross(radialDirection, playerVelocity));

        // 应用重力的切线分量
        float gravityTangent = Vector3.Dot(Vector3.down * gravity, tangentDirection);
        Vector3 gravityForce = tangentDirection * gravityTangent;

        // 应用摆动阻尼
        playerVelocity *= swingDamping;

        // 更新速度
        playerVelocity += gravityForce * Time.deltaTime;
        playerVelocity = Vector3.ClampMagnitude(playerVelocity, maxSwingSpeed);

        // 应用约束修正
        playerVelocity += correction / Time.deltaTime * 0.5f;

        // 移除径向速度分量（防止拉伸绳索）
        float radialVelocity = Vector3.Dot(playerVelocity, radialDirection);
        if (radialVelocity > 0) // 只移除远离钩点的速度
        {
            playerVelocity -= radialDirection * radialVelocity;
        }
    }

    private void ApplyFreeFall()
    {
        // 简单的重力和空气阻力
        playerVelocity.y -= gravity * Time.deltaTime;
        playerVelocity *= (1f - airResistance * 0.1f * Time.deltaTime);
    }

    private void HandlePlayerInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 摆动控制
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
            if (climbInput > 0 && currentRopeLength > 2f)
            {
                Vector3 climbDirection = (attachPoint - transform.position).normalized;
                playerVelocity += climbDirection * climbInput;
                restRopeLength = Mathf.Max(2f, restRopeLength - climbInput);
            }
            // 向下放绳
            else if (climbInput < 0 && restRopeLength < maxRopeLength)
            {
                restRopeLength = Mathf.Min(maxRopeLength, restRopeLength - climbInput);
            }
        }

        // 释放钩爪
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            ReleaseGrapple();
        }
    }

    private void UpdateRopePhysics()
    {
        if (!isGrappling) return;

        Vector3 playerPos = transform.position + Vector3.up * 1.5f; // 胸部位置
        Vector3 hookPos = isHookAttached ? attachPoint : hookPosition;

        // 计算绳索分段位置
        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = (float)i / ropeSegments;
            Vector3 targetPos = Vector3.Lerp(playerPos, hookPos, t);

            if (i == 0)
            {
                ropePoints[i] = playerPos;
            }
            else if (i == ropeSegments)
            {
                ropePoints[i] = hookPos;
            }
            else
            {
                // 模拟重力对绳索的影响
                float sag = Mathf.Sin(t * Mathf.PI) * currentRopeLength * 0.1f;
                targetPos.y -= sag;

                // 平滑到目标位置
                ropePoints[i] = Vector3.Lerp(ropePoints[i], targetPos, Time.deltaTime * 5f);
            }
        }
    }

    private void UpdateRopeVisuals()
    {
        if (lineRenderer != null && ropePoints != null)
        {
            lineRenderer.positionCount = ropePoints.Length;
            lineRenderer.SetPositions(ropePoints);
        }
    }

    private void ReleaseGrapple()
    {
        if (!isHookAttached) return;

        // 保持动量
        Vector3 currentVelocity = (transform.position - lastPlayerPosition) / Time.deltaTime;

        // 添加释放冲力
        if (currentVelocity.magnitude > 3f)
        {
            Vector3 releaseBoost = currentVelocity.normalized * currentVelocity.magnitude * momentumRetention;
            // 这里可以应用到玩家的移动组件或者保存这个速度供后续使用
        }

        StopGrapple();

        PlaySound(hookDetachSound);
        Debug.Log($"释放钩爪，保持动量: {currentVelocity.magnitude}");
    }

    public void StopGrapple()
    {
        // 通知可钩住表面钩爪已脱离
        if (currentGrappable != null)
        {
            currentGrappable.OnGrappleDetach();
            currentGrappable = null;
        }

        isGrappling = false;
        isHookFlying = false;
        isHookAttached = false;
        grappleTimer = 0f;
        attachedSurface = null;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

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

    private void CreateImpactEffect(Vector3 position, bool successful)
    {
        GameObject effect = successful ? hookImpactEffect : sparkEffect;
        if (effect != null)
        {
            GameObject instance = Instantiate(effect, position, Quaternion.identity);
            Destroy(instance, 2f);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // 可视化调试
    private void OnDrawGizmos()
    {
        if (isHookAttached)
        {
            // 绘制附着点
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(attachPoint, 0.5f);

            // 绘制绳索长度
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, attachPoint);

            // 绘制摆动半径
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attachPoint, restRopeLength);
        }

        if (isHookFlying && hookInstance != null)
        {
            // 绘制飞行轨迹预测
            Gizmos.color = Color.red;
            Vector3 pos = hookPosition;
            Vector3 vel = hookVelocity;

            for (int i = 0; i < 20; i++)
            {
                Vector3 nextPos = pos + vel * 0.1f;
                vel.y -= gravity * 0.1f;

                Gizmos.DrawLine(pos, nextPos);
                pos = nextPos;
            }
        }
    }

    // 公共接口
    public bool IsGrappling() => isGrappling;
    public bool IsSwinging() => isHookAttached;
    public float GetRopeLength() => currentRopeLength;
    public Vector3 GetAttachPoint() => attachPoint;
}