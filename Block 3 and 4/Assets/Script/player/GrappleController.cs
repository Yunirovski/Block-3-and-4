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

    // 组件引用
    private CharacterController controller;
    private LineRenderer lineRenderer;
    private GameObject hookInstance;

    // 抓钩状态
    private bool isGrappling = false;
    private bool isHookFlying = false;
    private Vector3 grapplePoint;
    private float currentPullSpeed = 10f;
    private float grappleTimeLimit = 15f; // 安全限制：最长抓钩时间
    private float grappleTimer = 0f;

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
            MoveHookTowardsTarget();
        }
        // 如果正在抓取
        else if (isGrappling)
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

        // 创建钩子实例
        if (hookPrefab != null)
        {
            // 从相机位置发射钩子
            Vector3 startPosition = Camera.main.transform.position;
            hookInstance = Instantiate(hookPrefab, startPosition, Quaternion.identity);

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

            // 开始钩子飞行
            isHookFlying = true;

            // 启用绳索
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
            }

            Debug.Log("创建了钩子，开始飞行");
        }
        else
        {
            Debug.LogError("钩子预制体为空，无法创建钩子");
        }
    }

    // 停止抓钩
    public void StopGrapple()
    {
        isGrappling = false;
        isHookFlying = false;

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

    // 移动钩子到目标点
    private void MoveHookTowardsTarget()
    {
        if (hookInstance == null) return;

        // 计算方向和距离
        Vector3 direction = (grapplePoint - hookInstance.transform.position).normalized;
        float distanceThisFrame = hookSpeed * Time.deltaTime;
        float distanceToTarget = Vector3.Distance(hookInstance.transform.position, grapplePoint);

        // 如果到达目标
        if (distanceToTarget <= distanceThisFrame)
        {
            // 到达目标点
            hookInstance.transform.position = grapplePoint;
            isHookFlying = false;
            isGrappling = true;
            Debug.Log("钩子到达目标，开始拉动玩家");
        }
        else
        {
            // 继续移动钩子
            hookInstance.transform.position += direction * distanceThisFrame;

            // 旋转钩子
            if (direction != Vector3.zero)
            {
                hookInstance.transform.rotation = Quaternion.LookRotation(direction);
            }
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
}