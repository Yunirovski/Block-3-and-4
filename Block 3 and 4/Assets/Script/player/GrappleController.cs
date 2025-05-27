// Assets/Scripts/Player/GrappleController.cs
using System.Collections;
using UnityEngine;

public class GrappleController : MonoBehaviour
{
    // 公开属性（从GrappleItem设置）
    [HideInInspector] public GameObject hookPrefab;
    [HideInInspector] public float hookSpeed = 70f;
    [HideInInspector] public Material ropeMaterial;
    [HideInInspector] public Color ropeColor = new Color(0.545f, 0.271f, 0.075f);
    [HideInInspector] public Transform muzzlePoint; // 新增：枪口位置

    // 物理参数
    [Header("Physics Settings")]
    [Tooltip("重力加速度")]
    public float gravity = 9.81f;
    [Tooltip("钩爪碰撞检测半径")]
    public float hookCollisionRadius = 0.3f;
    [Tooltip("钩爪最大飞行时间（秒）")]
    public float maxFlightTime = 5f;
    [Tooltip("钩爪发射角度调整（度数，正值向上）")]
    public float launchAngleOffset = 0f;
    [Tooltip("发射前偏移距离，避免卡在枪口")]
    public float launchOffset = 0.5f;

    // 组件引用
    private CharacterController controller;
    private LineRenderer lineRenderer;
    private GameObject hookInstance;

    // 抓钩状态
    private bool isGrappling = false;
    private bool isHookFlying = false;
    private Vector3 grapplePoint;
    private float currentPullSpeed = 10f;
    private float grappleTimeLimit = 15f;
    private float grappleTimer = 0f;

    // 钩爪物理状态
    private Vector3 hookVelocity;
    private Vector3 hookStartPosition;
    private float flightTimer = 0f;
    private bool hookConnected = false;

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
        grapplePoint = hitPoint;
        currentPullSpeed = pullSpeed;
        grappleTimer = 0f;
        flightTimer = 0f;
        hookConnected = false;

        // 确定发射起点
        hookStartPosition = GetLaunchPosition();

        // 创建钩子实例
        if (hookPrefab != null)
        {
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

            // 计算发射方向和初始速度
            CalculateHookLaunchVelocity();

            // 开始钩子飞行
            isHookFlying = true;

            // 启用绳索
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
            }

            Debug.Log($"创建了钩子，从{(muzzlePoint != null ? "枪口" : "相机")}位置发射。初始速度: {hookVelocity}");
        }
        else
        {
            Debug.LogError("钩子预制体为空，无法创建钩子");
        }
    }

    // 获取发射位置
    private Vector3 GetLaunchPosition()
    {
        if (muzzlePoint != null)
        {
            // 从枪口位置发射，稍微向前偏移避免卡在枪口
            Vector3 launchPos = muzzlePoint.position;
            Vector3 forwardOffset = muzzlePoint.forward * launchOffset;
            return launchPos + forwardOffset;
        }
        else
        {
            // fallback到相机位置
            Vector3 camPos = Camera.main.transform.position;
            Vector3 forwardOffset = Camera.main.transform.forward * launchOffset;
            return camPos + forwardOffset;
        }
    }

    // 获取发射方向
    private Vector3 GetLaunchDirection()
    {
        if (muzzlePoint != null)
        {
            return muzzlePoint.forward;
        }
        else
        {
            return Camera.main.transform.forward;
        }
    }

    // 计算钩爪发射速度
    private void CalculateHookLaunchVelocity()
    {
        Vector3 direction = (grapplePoint - hookStartPosition).normalized;
        float distance = Vector3.Distance(hookStartPosition, grapplePoint);

        // 应用角度偏移
        if (launchAngleOffset != 0f)
        {
            Vector3 right = muzzlePoint != null ? muzzlePoint.right : Camera.main.transform.right;
            direction = Quaternion.AngleAxis(launchAngleOffset, right) * direction;
        }

        // 简化速度计算 - 直接朝目标方向发射
        hookVelocity = direction * hookSpeed;

        Debug.Log($"计算发射参数 - 距离: {distance:F2}, 方向: {direction}");
    }

    // 更新钩爪物理
    private void UpdateHookPhysics()
    {
        if (hookInstance == null) return;

        flightTimer += Time.deltaTime;

        // 应用重力
        hookVelocity.y -= gravity * Time.deltaTime;

        // 计算这一帧的移动
        Vector3 deltaMove = hookVelocity * Time.deltaTime;
        Vector3 newPosition = hookInstance.transform.position + deltaMove;

        // 使用Raycast检测碰撞，避免穿墙
        RaycastHit hit;
        if (Physics.Raycast(
            hookInstance.transform.position,
            deltaMove.normalized,
            out hit,
            deltaMove.magnitude + hookCollisionRadius,
            ~0, // 检测所有层
            QueryTriggerInteraction.Ignore // 忽略触发器
        ))
        {
            // 检测到碰撞
            HandleHookCollision(hit);
        }
        else
        {
            // 没有碰撞，更新位置
            hookInstance.transform.position = newPosition;

            // 旋转钩爪面向运动方向
            if (hookVelocity.sqrMagnitude > 0.1f)
            {
                hookInstance.transform.rotation = Quaternion.LookRotation(hookVelocity.normalized);
            }

            // 检查是否超时或飞得太远
            if (flightTimer > maxFlightTime ||
                Vector3.Distance(hookStartPosition, hookInstance.transform.position) > hookSpeed * maxFlightTime)
            {
                Debug.Log("钩爪飞行超时或距离过远");
                StopGrapple();
            }
        }
    }

    // 处理钩爪碰撞
    private void HandleHookCollision(RaycastHit hit)
    {
        // 检查碰撞的物体是否可以附着
        if (hit.collider.gameObject.isStatic || hit.collider.CompareTag("Grappable"))
        {
            // 连接成功 - 将钩子放在碰撞点稍微外面一点
            Vector3 connectionPoint = hit.point + hit.normal * 0.1f;
            hookInstance.transform.position = connectionPoint;
            grapplePoint = connectionPoint;
            isHookFlying = false;
            isGrappling = true;
            hookConnected = true;

            Debug.Log($"钩爪成功附着到: {hit.collider.name} at {connectionPoint}");
        }
        else
        {
            // 碰撞到不可附着的物体
            Debug.Log($"钩爪碰撞到不可附着物体: {hit.collider.name}");
            StopGrapple();
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
        if (lineRenderer == null) return;

        // 设置绳索起点
        Vector3 ropeStart;
        if (muzzlePoint != null)
        {
            ropeStart = muzzlePoint.position;
        }
        else
        {
            // fallback到相机附近的手部位置
            ropeStart = Camera.main.transform.position +
                       Camera.main.transform.right * 0.2f -
                       Camera.main.transform.up * 0.1f;
        }

        lineRenderer.SetPosition(0, ropeStart);

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

    // 拉动玩家到钩子位置
    private void PullPlayerTowardsTarget()
    {
        if (controller == null) return;

        // 计算方向
        Vector3 playerPosition = transform.position;
        Vector3 direction = (grapplePoint - playerPosition).normalized;

        // 增强垂直拉力
        Vector3 pullDirection = direction * currentPullSpeed;
        if (pullDirection.y > 0)
        {
            pullDirection.y *= 1.5f; // 增强向上拉力
        }

        // 移动玩家
        controller.Move(pullDirection * Time.deltaTime);

        // 如果到达目标点附近，停止抓钩
        float distanceToTarget = Vector3.Distance(transform.position, grapplePoint);
        if (distanceToTarget < 2.0f)
        {
            Debug.Log("玩家到达目标点附近，停止抓钩");
            StopGrapple();
        }
    }

    // 在Scene视图中绘制调试信息
    private void OnDrawGizmos()
    {
        // 绘制枪口位置
        if (muzzlePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(muzzlePoint.position, 0.2f);
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * 2f);
        }

        if (isHookFlying && hookInstance != null)
        {
            // 绘制钩爪碰撞检测
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hookInstance.transform.position, hookCollisionRadius);

            // 绘制速度向量
            Gizmos.color = Color.red;
            Gizmos.DrawRay(hookInstance.transform.position, hookVelocity.normalized * 2f);
        }

        if (isGrappling && hookConnected)
        {
            // 绘制连接点
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(grapplePoint, 0.5f);
        }
    }
}