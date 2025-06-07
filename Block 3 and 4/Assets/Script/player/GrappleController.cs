// Assets/Scripts/Player/GrappleController.cs
using System.Collections;
using UnityEngine;

public class GrappleController : MonoBehaviour
{
    // 公开属性（从GrappleItem设置）
    [HideInInspector] public GameObject hookPrefab;
    [HideInInspector] public float hookSpeed = 50f;
    [HideInInspector] public Material ropeMaterial;
    [HideInInspector] public Color ropeColor = new Color(0.545f, 0.271f, 0.075f);

    [Header("Physics Settings")]
    [Tooltip("重力加速度")]
    public float gravity = 15f;
    [Tooltip("钩爪碰撞检测半径")]
    public float hookCollisionRadius = 0.5f;
    [Tooltip("钩爪最大飞行时间（秒）")]
    public float maxFlightTime = 6f;
    [Tooltip("发射角度偏移（度数，正值向上）")]
    public float launchAngleOffset = 15f;

    [Header("Smart Landing")]
    [Tooltip("地面检测距离")]
    public float groundDetectionDistance = 50f;
    [Tooltip("最低飞行高度（防止钩爪飞得太低）")]
    public float minFlightHeight = 2f;
    [Tooltip("强制落地检测（钩爪飞行时持续检测下方地面）")]
    public bool enableGroundMagnet = true;

    [Header("Pull Settings")]
    [Tooltip("玩家拉拽速度")]
    public float playerPullSpeed = 15f;
    [Tooltip("停止距离（距离目标多少米时停止）")]
    public float stopDistance = 2.5f;
    [Tooltip("最大拉拽距离")]
    public float maxPullDistance = 100f;

    // 组件引用
    private CharacterController controller;
    private LineRenderer lineRenderer;
    private GameObject hookInstance;

    // 抓钩状态
    private bool isGrappling = false;
    private bool isHookFlying = false;
    private Vector3 grapplePoint;
    private float grappleTimeLimit = 10f;
    private float grappleTimer = 0f;

    // 钩爪物理状态
    private Vector3 hookVelocity;
    private Vector3 hookStartPosition;
    private Vector3 originalTarget;
    private float flightTimer = 0f;
    private bool hookConnected = false;

    // 智能着陆
    private LayerMask groundLayers = -1; // 所有层都算作可能的着陆点

    // 初始化方法
    public void Initialize()
    {
        // 确保有CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            Debug.Log("GrappleController: 添加了缺失的CharacterController组件");
        }

        // 确保有LineRenderer
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.material = ropeMaterial;
            lineRenderer.startColor = ropeColor;
            lineRenderer.endColor = ropeColor;
            lineRenderer.enabled = false;
            Debug.Log("GrappleController: 设置了LineRenderer");
        }
        else if (ropeMaterial != null)
        {
            lineRenderer.material = ropeMaterial;
            lineRenderer.startColor = ropeColor;
            lineRenderer.endColor = ropeColor;
        }

        Debug.Log("GrappleController: 初始化完成");
    }

    void Update()
    {
        // 如果正在抓钩中，更新计时器
        if (isGrappling || isHookFlying)
        {
            grappleTimer += Time.deltaTime;

            // 如果超时，停止抓钩
            if (grappleTimer > grappleTimeLimit)
            {
                Debug.Log("GrappleController: 抓钩超时，自动停止");
                StopGrapple();
                return;
            }

            // 更新绳索位置
            UpdateRopePositions();
        }

        // 如果钩子正在飞行
        if (isHookFlying && hookInstance != null)
        {
            UpdateHookPhysics();
        }
        // 如果正在抓取
        else if (isGrappling && hookConnected)
        {
            PullPlayerTowardsTarget();
        }
    }

    // 开始抓钩
    public void StartGrapple(Vector3 hitPoint, float pullSpeed)
    {
        // 如果已经在抓钩，先停止
        if (isGrappling || isHookFlying)
        {
            StopGrapple();
        }

        Debug.Log($"开始抓钩到位置: {hitPoint}, 拉力: {pullSpeed}");

        // 设置参数
        originalTarget = hitPoint;
        grapplePoint = hitPoint;
        playerPullSpeed = pullSpeed;
        grappleTimer = 0f;
        flightTimer = 0f;
        hookConnected = false;

        // 创建钩子实例
        if (hookPrefab != null)
        {
            // 从相机位置发射钩子
            hookStartPosition = Camera.main.transform.position;
            hookInstance = Instantiate(hookPrefab, hookStartPosition, Quaternion.identity);

            // 设置钩子颜色
            Renderer hookRenderer = hookInstance.GetComponent<Renderer>();
            if (hookRenderer != null)
            {
                Material[] mats = hookRenderer.materials;
                foreach (Material mat in mats)
                {
                    mat.color = ropeColor;
                }
                hookRenderer.materials = mats;
            }

            // 计算发射速度
            CalculateLaunchVelocity();

            // 开始钩子飞行
            isHookFlying = true;

            // 启用绳索
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
            }

            Debug.Log($"创建了钩子，开始智能飞行。初始速度: {hookVelocity}");
        }
        else
        {
            Debug.LogError("钩子预制体为空，无法创建钩子");
        }
    }

    // 计算发射速度（朝目标方向，带角度偏移）
    private void CalculateLaunchVelocity()
    {
        Vector3 direction = (originalTarget - hookStartPosition).normalized;

        // 应用角度偏移（向上一点，让轨迹更自然）
        Vector3 right = Camera.main.transform.right;
        direction = Quaternion.AngleAxis(launchAngleOffset, right) * direction;

        // 设置初始速度
        hookVelocity = direction * hookSpeed;

        Debug.Log($"发射方向: {direction}, 速度: {hookSpeed}, 角度偏移: {launchAngleOffset}度");
    }

    // 更新钩爪物理（真实重力模拟 + 智能着陆）
    private void UpdateHookPhysics()
    {
        if (hookInstance == null) return;

        flightTimer += Time.deltaTime;

        // 检查是否超时
        if (flightTimer > maxFlightTime)
        {
            Debug.Log("钩爪飞行超时，强制着陆");
            ForceHookLanding();
            return;
        }

        Vector3 oldPosition = hookInstance.transform.position;

        // 应用重力
        hookVelocity.y -= gravity * Time.deltaTime;

        // 计算新位置
        Vector3 newPosition = oldPosition + hookVelocity * Time.deltaTime;

        // 智能地面检测
        if (enableGroundMagnet)
        {
            CheckSmartLanding(oldPosition, newPosition);
        }

        // 普通碰撞检测
        if (hookInstance != null) // 确保在智能着陆检查后钩爪还存在
        {
            CheckHookCollision(oldPosition, newPosition);
        }

        // 如果钩爪还在飞行，更新位置
        if (hookInstance != null && isHookFlying)
        {
            hookInstance.transform.position = newPosition;

            // 更新旋转朝向运动方向
            if (hookVelocity.sqrMagnitude > 0.01f)
            {
                hookInstance.transform.rotation = Quaternion.LookRotation(hookVelocity.normalized);
            }

            // 检查是否飞得太远
            float distanceFromStart = Vector3.Distance(hookStartPosition, newPosition);
            if (distanceFromStart > maxPullDistance)
            {
                Debug.Log("钩爪飞行距离过远，强制着陆");
                ForceHookLanding();
                return;
            }
        }
    }

    // 智能着陆检测
    private void CheckSmartLanding(Vector3 fromPos, Vector3 toPos)
    {
        // 如果钩爪在下降，检测下方是否有可着陆的表面
        if (hookVelocity.y < -2f) // 下降速度足够快时
        {
            RaycastHit hit;
            // 从当前位置向下检测
            if (Physics.Raycast(fromPos, Vector3.down, out hit, groundDetectionDistance, groundLayers))
            {
                // 计算钩爪按当前轨迹什么时候会到达这个高度
                float timeToReachHeight = Mathf.Abs((fromPos.y - hit.point.y) / hookVelocity.y);
                Vector3 horizontalMovement = new Vector3(hookVelocity.x, 0, hookVelocity.z) * timeToReachHeight;
                Vector3 predictedLandingPoint = fromPos + horizontalMovement;
                predictedLandingPoint.y = hit.point.y;

                // 检查预测着陆点附近是否有可附着的表面
                if (Physics.CheckSphere(predictedLandingPoint, hookCollisionRadius * 2f, groundLayers))
                {
                    // 如果检测到合适的着陆点，并且钩爪已经够接近了
                    float distanceToLanding = Vector3.Distance(fromPos, predictedLandingPoint);
                    if (distanceToLanding < hookSpeed * Time.deltaTime * 3f) // 3帧内会到达
                    {
                        // 直接让钩爪着陆到这个点
                        if (Physics.Raycast(predictedLandingPoint + Vector3.up * 2f, Vector3.down, out RaycastHit landingHit, 5f, groundLayers))
                        {
                            AttachHook(landingHit.point, landingHit.collider.gameObject);
                            return;
                        }
                    }
                }
            }
        }
    }

    // 强制钩爪着陆（时间到了或距离太远时）
    private void ForceHookLanding()
    {
        if (hookInstance == null) return;

        Vector3 hookPos = hookInstance.transform.position;
        RaycastHit hit;

        // 向下检测地面
        if (Physics.Raycast(hookPos, Vector3.down, out hit, groundDetectionDistance, groundLayers))
        {
            AttachHook(hit.point, hit.collider.gameObject);
            Debug.Log($"强制着陆到: {hit.collider.name}");
        }
        else
        {
            // 如果向下检测不到，向四周检测最近的表面
            Vector3[] directions = {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                Vector3.down, new Vector3(1,1,1).normalized, new Vector3(-1,1,1).normalized,
                new Vector3(1,1,-1).normalized, new Vector3(-1,1,-1).normalized
            };

            float closestDistance = float.MaxValue;
            RaycastHit closestHit = new RaycastHit();
            bool foundSurface = false;

            foreach (Vector3 dir in directions)
            {
                if (Physics.Raycast(hookPos, dir, out hit, groundDetectionDistance, groundLayers))
                {
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        closestHit = hit;
                        foundSurface = true;
                    }
                }
            }

            if (foundSurface)
            {
                AttachHook(closestHit.point, closestHit.collider.gameObject);
                Debug.Log($"强制着陆到最近表面: {closestHit.collider.name}");
            }
            else
            {
                // 实在找不到就停止
                Debug.Log("找不到任何可着陆的表面，停止抓钩");
                StopGrapple();
            }
        }
    }

    // 检查钩爪碰撞
    private void CheckHookCollision(Vector3 fromPos, Vector3 toPos)
    {
        Vector3 direction = toPos - fromPos;
        float distance = direction.magnitude;

        if (distance > 0.01f)
        {
            RaycastHit hit;
            if (Physics.Raycast(fromPos, direction.normalized, out hit, distance + hookCollisionRadius, groundLayers))
            {
                AttachHook(hit.point, hit.collider.gameObject);
            }
        }
    }

    // 钩爪附着
    private void AttachHook(Vector3 attachPoint, GameObject attachedObject)
    {
        if (hookInstance != null)
        {
            hookInstance.transform.position = attachPoint;
        }

        grapplePoint = attachPoint;
        isHookFlying = false;
        isGrappling = true;
        hookConnected = true;

        Debug.Log($"钩爪附着到: {attachedObject.name} at {attachPoint}");
    }

    // 拉动玩家到钩子位置
    private void PullPlayerTowardsTarget()
    {
        if (controller == null) return;

        Vector3 playerPosition = transform.position;
        Vector3 direction = (grapplePoint - playerPosition);
        float distanceToTarget = direction.magnitude;

        // 检查是否应该停止
        if (distanceToTarget <= stopDistance)
        {
            Debug.Log("玩家到达目标点附近，停止抓钩");
            StopGrapple();
            return;
        }

        // 检查距离是否过远
        if (distanceToTarget > maxPullDistance)
        {
            Debug.Log("距离目标过远，停止抓钩");
            StopGrapple();
            return;
        }

        // 归一化方向
        direction = direction.normalized;

        // 计算拉拽速度，距离越远速度越快
        float speedMultiplier = Mathf.Clamp(distanceToTarget / 10f, 0.5f, 2f);
        Vector3 pullVelocity = direction * playerPullSpeed * speedMultiplier;

        // 增强垂直分量以便更好地上升
        if (pullVelocity.y > 0)
        {
            pullVelocity.y *= 1.3f;
        }

        // 应用移动
        controller.Move(pullVelocity * Time.deltaTime);

        // 调试信息
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"拉拽中 - 距离: {distanceToTarget:F1}m, 速度倍率: {speedMultiplier:F2}");
        }
    }

    // 停止抓钩
    public void StopGrapple()
    {
        isGrappling = false;
        isHookFlying = false;
        hookConnected = false;
        flightTimer = 0f;

        // 禁用绳索
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // 销毁钩子
        if (hookInstance != null)
        {
            Destroy(hookInstance);
            hookInstance = null;
            Debug.Log("销毁了钩子实例");
        }

        Debug.Log("停止抓钩");
    }

    // 更新绳索位置
    private void UpdateRopePositions()
    {
        if (lineRenderer == null || Camera.main == null) return;

        // 设置绳索起点（玩家手部位置）
        Vector3 handPosition = Camera.main.transform.position +
                              Camera.main.transform.right * 0.2f -
                              Camera.main.transform.up * 0.1f;

        lineRenderer.SetPosition(0, handPosition);

        // 设置绳索终点（钩子位置）
        if (hookInstance != null)
        {
            lineRenderer.SetPosition(1, hookInstance.transform.position);
        }
        else
        {
            lineRenderer.SetPosition(1, grapplePoint);
        }
    }

    // 在Scene视图中绘制调试信息
    private void OnDrawGizmos()
    {
        if (isHookFlying && hookInstance != null)
        {
            Vector3 hookPos = hookInstance.transform.position;

            // 绘制钩爪碰撞球体
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hookPos, hookCollisionRadius);

            // 绘制速度向量
            Gizmos.color = Color.red;
            Gizmos.DrawRay(hookPos, hookVelocity.normalized * 3f);

            // 绘制地面检测
            Gizmos.color = Color.green;
            Gizmos.DrawRay(hookPos, Vector3.down * groundDetectionDistance);

            // 绘制预测轨迹
            Gizmos.color = Color.cyan;
            Vector3 pos = hookPos;
            Vector3 vel = hookVelocity;

            for (int i = 0; i < 30; i++)
            {
                Vector3 nextPos = pos + vel * 0.1f;
                vel.y -= gravity * 0.1f;

                Gizmos.DrawLine(pos, nextPos);
                pos = nextPos;

                // 如果轨迹太低就停止绘制
                if (pos.y < hookStartPosition.y - 30f) break;
            }
        }

        if (isGrappling && hookConnected)
        {
            // 绘制连接点
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(grapplePoint, 0.5f);

            // 绘制停止距离
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(grapplePoint, stopDistance);
        }
    }
}