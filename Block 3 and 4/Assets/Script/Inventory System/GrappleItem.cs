// Assets/Scripts/Items/GrappleItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/GrappleItem")]
public class GrappleItem : BaseItem
{
    [Header("钩爪参数")]
    [Tooltip("最大射程")]
    public float maxDistance = 40f;
    [Tooltip("拉拽速度")]
    public float pullSpeed = 15f;
    [Tooltip("钩爪发射速度")]
    public float hookSpeed = 50f;

    [Header("发射点设置")]
    [Tooltip("自定义发射点Transform（可选）")]
    public Transform customFirePoint;
    [Tooltip("发射点偏移（相对于发射点Transform）")]
    public Vector3 firePointOffset = Vector3.zero;
    [Tooltip("是否使用玩家朝向而非摄像机朝向")]
    public bool usePlayerOrientation = false;

    [Header("钩爪可视化")]
    [Tooltip("钩爪模型 Prefab")]
    public GameObject hookPrefab;
    [Tooltip("绳索材质")]
    public Material ropeMaterial;
    [Tooltip("钩爪和绳索的颜色")]
    public Color ropeColor = new Color(0.545f, 0.271f, 0.075f);

    [Header("物理设置")]
    [Tooltip("钩爪质量")]
    public float hookMass = 2f;
    [Tooltip("重力强度")]
    public float gravity = 15f;

    [Header("拉拽设置")]
    [Tooltip("拉拽加速度")]
    public float pullAcceleration = 20f;
    [Tooltip("到达目标的距离阈值")]
    public float arrivalDistance = 3f;
    [Tooltip("是否到达后自动释放")]
    public bool autoReleaseOnArrival = true;
    [Tooltip("拉拽时保持的重力")]
    public float pullGravity = 5f;

    [Header("控制设置")]
    [Tooltip("是否允许控制拉拽方向")]
    public bool allowDirectionalControl = true;
    [Tooltip("方向控制力度")]
    public float directionalForce = 8f;

    [Header("音效")]
    [Tooltip("钩爪发射音效")]
    public AudioClip grappleFireSound;
    [Tooltip("钩爪命中音效")]
    public AudioClip grappleHitSound;
    [Tooltip("拉拽开始音效")]
    public AudioClip pullStartSound;
    [Tooltip("钩爪脱落音效")]
    public AudioClip detachSound;
    [Tooltip("音效音量")]
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("视觉效果")]
    [Tooltip("钩爪命中特效")]
    public GameObject hookImpactEffect;
    [Tooltip("拉拽特效")]
    public GameObject pullEffect;
    [Tooltip("到达特效")]
    public GameObject arrivalEffect;

    [Header("瞄准辅助")]
    [Tooltip("显示瞄准轨迹")]
    public bool showAimTrajectory = true;
    [Tooltip("轨迹预测时间")]
    public float trajectoryTime = 2f;
    [Tooltip("轨迹分辨率")]
    public int trajectoryResolution = 30;

    // 运行时缓存
    private Camera _cam;
    private GrappleController _grappler;
    private AudioSource _audioSource;
    private LineRenderer _trajectoryRenderer;
    private bool _isAiming = false;

    public override void OnSelect(GameObject model)
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            Debug.LogError("找不到主相机");
            return;
        }

        // 获取或创建钩爪控制器
        _grappler = _cam.GetComponentInParent<GrappleController>();
        if (_grappler == null)
        {
            GameObject player = _cam.transform.parent?.gameObject;
            if (player != null)
            {
                _grappler = player.AddComponent<GrappleController>();
                Debug.Log("自动添加钩爪控制器组件");
            }
            else
            {
                Debug.LogError("玩家物体上缺少 GrappleController 组件且无法自动添加");
                UIManager.Instance?.UpdateCameraDebugText("钩爪控制器未找到");
                return;
            }
        }

        // 创建音频源
        _audioSource = _cam.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = _cam.gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 全局音效
        }

        // 创建轨迹渲染器
        SetupTrajectoryRenderer();

        // 注入参数到钩爪控制器
        ConfigureGrappleController();

        // 初始化控制器
        _grappler.Initialize();

        UIManager.Instance?.UpdateCameraDebugText("钩爪就绪 - 左键发射，Q键/右键释放");
    }

    private void SetupTrajectoryRenderer()
    {
        if (!showAimTrajectory) return;

        GameObject trajectoryObject = new GameObject("GrappleTrajectory");
        trajectoryObject.transform.SetParent(_cam.transform);

        _trajectoryRenderer = trajectoryObject.AddComponent<LineRenderer>();
        _trajectoryRenderer.positionCount = trajectoryResolution;
        _trajectoryRenderer.startWidth = 0.02f;
        _trajectoryRenderer.endWidth = 0.01f;
        _trajectoryRenderer.material = ropeMaterial;
        _trajectoryRenderer.startColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 0.5f);
        _trajectoryRenderer.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 0.1f);
        _trajectoryRenderer.enabled = false;
        _trajectoryRenderer.useWorldSpace = true;

        Debug.Log("轨迹渲染器设置完成");
    }

    private void ConfigureGrappleController()
    {
        // 注入资源
        _grappler.hookPrefab = hookPrefab;
        _grappler.hookSpeed = hookSpeed;
        _grappler.ropeMaterial = ropeMaterial;
        _grappler.ropeColor = ropeColor;

        // 设置发射点配置
        _grappler.customFirePoint = customFirePoint;
        _grappler.firePointOffset = firePointOffset;
        _grappler.usePlayerOrientation = usePlayerOrientation;

        // 设置物理参数
        _grappler.hookMass = hookMass;
        _grappler.gravity = gravity;
        _grappler.maxRopeLength = maxDistance;

        // 设置拉拽参数
        _grappler.pullSpeed = pullSpeed;
        _grappler.pullAcceleration = pullAcceleration;
        _grappler.arrivalDistance = arrivalDistance;
        _grappler.autoReleaseOnArrival = autoReleaseOnArrival;
        _grappler.pullGravity = pullGravity;

        // 设置控制参数
        _grappler.allowDirectionalControl = allowDirectionalControl;
        _grappler.directionalForce = directionalForce;

        // 设置特效
        _grappler.hookImpactEffect = hookImpactEffect;
        _grappler.pullEffect = pullEffect;
        _grappler.arrivalEffect = arrivalEffect;

        // 设置音效
        _grappler.hookFireSound = grappleFireSound;
        _grappler.hookHitSound = grappleHitSound;
        _grappler.pullStartSound = pullStartSound;
        _grappler.hookDetachSound = detachSound;
    }

    /// <summary>
    /// 设置自定义发射点
    /// </summary>
    /// <param name="firePoint">发射点Transform</param>
    /// <param name="offset">相对偏移</param>
    public void SetFirePoint(Transform firePoint, Vector3 offset = default)
    {
        customFirePoint = firePoint;
        firePointOffset = offset;

        // 如果钩爪控制器已初始化，同步设置
        if (_grappler != null)
        {
            _grappler.SetFirePoint(firePoint, offset);
        }

        Debug.Log($"GrappleItem: 设置发射点: {(firePoint != null ? firePoint.name : "摄像机")}, 偏移: {offset}");
    }

    /// <summary>
    /// 清除自定义发射点
    /// </summary>
    public void ClearCustomFirePoint()
    {
        customFirePoint = null;
        firePointOffset = Vector3.zero;

        if (_grappler != null)
        {
            _grappler.ClearCustomFirePoint();
        }

        Debug.Log("GrappleItem: 清除自定义发射点");
    }

    public override void OnUse()
    {
        if (_grappler == null || _cam == null)
        {
            UIManager.Instance?.UpdateCameraDebugText("钩爪系统未准备好");
            return;
        }

        // 如果已经在使用钩爪，则释放
        if (_grappler.IsGrappling())
        {
            _grappler.StopGrapple();
            UIManager.Instance?.UpdateCameraDebugText("钩爪已释放");
            return;
        }

        // 计算目标点
        Vector3 targetPoint = CalculateTargetPoint();

        // 播放发射音效
        if (grappleFireSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(grappleFireSound, soundVolume);
        }

        // 发射钩爪
        _grappler.StartGrapple(targetPoint, pullSpeed);

        UIManager.Instance?.UpdateCameraDebugText("钩爪已发射");
    }

    public override void HandleUpdate()
    {
        if (_grappler == null || _cam == null) return;

        // 更新瞄准状态
        UpdateAiming();

        // 显示钩爪状态信息
        UpdateStatusDisplay();

        // 处理快捷键
        HandleHotkeys();
    }

    private void UpdateAiming()
    {
        if (!showAimTrajectory || _trajectoryRenderer == null) return;

        bool shouldShowTrajectory = !_grappler.IsGrappling() &&
                                   (Input.GetMouseButton(0) || Input.GetKey(KeyCode.LeftAlt));

        if (shouldShowTrajectory && !_isAiming)
        {
            _isAiming = true;
            _trajectoryRenderer.enabled = true;
        }
        else if (!shouldShowTrajectory && _isAiming)
        {
            _isAiming = false;
            _trajectoryRenderer.enabled = false;
        }

        if (_isAiming)
        {
            UpdateTrajectoryVisual();
        }
    }

    private void UpdateTrajectoryVisual()
    {
        // 使用钩爪控制器的发射点
        Vector3 firePoint = _grappler.GetFirePoint();
        Vector3 targetPoint = CalculateTargetPoint();

        // 计算初始速度
        Vector3 direction = (targetPoint - firePoint).normalized;
        Vector3 velocity = direction * hookSpeed;

        // 预测轨迹
        Vector3[] points = new Vector3[trajectoryResolution];
        Vector3 currentPos = firePoint;
        Vector3 currentVel = velocity;

        float timeStep = trajectoryTime / trajectoryResolution;

        for (int i = 0; i < trajectoryResolution; i++)
        {
            points[i] = currentPos;

            // 应用物理
            currentVel.y -= gravity * timeStep;
            currentPos += currentVel * timeStep;

            // 检查碰撞
            RaycastHit hit;
            if (Physics.Raycast(points[i], (currentPos - points[i]).normalized,
                               out hit, Vector3.Distance(points[i], currentPos)))
            {
                // 在碰撞点截断轨迹
                points[i] = hit.point;
                _trajectoryRenderer.positionCount = i + 1;
                _trajectoryRenderer.SetPositions(points);
                return;
            }
        }

        _trajectoryRenderer.positionCount = trajectoryResolution;
        _trajectoryRenderer.SetPositions(points);
    }

    private Vector3 CalculateTargetPoint()
    {
        // 获取发射起点和方向
        Vector3 firePoint = _grappler != null ? _grappler.GetFirePoint() : _cam.transform.position;
        Vector3 fireDirection = _grappler != null ? _grappler.GetFireDirection() : _cam.transform.forward;

        // 创建射线
        Ray ray = new Ray(firePoint, fireDirection);

        // 如果使用摄像机瞄准，则从屏幕中心发射射线
        if (!usePlayerOrientation && _grappler.customFirePoint == null)
        {
            ray = _cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        }

        // 尝试命中目标
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            return hit.point;
        }
        else
        {
            // 如果没有命中，向射程最大距离发射
            return ray.origin + ray.direction * maxDistance;
        }
    }

    private void UpdateStatusDisplay()
    {
        if (UIManager.Instance == null) return;

        string status = "";

        if (_grappler.IsGrappling())
        {
            if (_grappler.IsPulling())
            {
                float ropeLength = _grappler.GetRopeLength();
                float pullSpeed = _grappler.GetPullSpeed();
                status = $"拉拽中 - 距离: {ropeLength:F1}m, 速度: {pullSpeed:F1}m/s (WASD控制, Q/右键释放)";
            }
            else
            {
                status = "钩爪飞行中...";
            }
        }
        else
        {
            // 检查瞄准目标
            Vector3 targetPoint = CalculateTargetPoint();
            Vector3 firePoint = _grappler.GetFirePoint();
            float distance = Vector3.Distance(firePoint, targetPoint);

            string firePointInfo = "";
            if (_grappler.customFirePoint != null)
            {
                firePointInfo = $" [从 {_grappler.customFirePoint.name} 发射]";
            }

            if (distance <= maxDistance)
            {
                status = $"瞄准目标 - 距离: {distance:F1}m{firePointInfo} (左键发射)";
            }
            else
            {
                status = $"目标过远 - 距离: {distance:F1}m (最大: {maxDistance}m){firePointInfo}";
            }
        }

        UIManager.Instance.UpdateCameraDebugText(status);
    }

    private void HandleHotkeys()
    {
        // R键快速释放
        if (Input.GetKeyDown(KeyCode.R) && _grappler.IsGrappling())
        {
            _grappler.StopGrapple();
            UIManager.Instance?.UpdateCameraDebugText("强制释放钩爪");
        }

        // T键切换轨迹显示
        if (Input.GetKeyDown(KeyCode.T))
        {
            showAimTrajectory = !showAimTrajectory;
            if (_trajectoryRenderer != null)
            {
                _trajectoryRenderer.enabled = showAimTrajectory && _isAiming;
            }
            UIManager.Instance?.UpdateCameraDebugText($"轨迹显示: {(showAimTrajectory ? "开启" : "关闭")}");
        }

        // F键切换发射点模式（仅用于调试）
        if (Input.GetKeyDown(KeyCode.F))
        {
            usePlayerOrientation = !usePlayerOrientation;
            if (_grappler != null)
            {
                _grappler.usePlayerOrientation = usePlayerOrientation;
            }
            UIManager.Instance?.UpdateCameraDebugText($"发射方向: {(usePlayerOrientation ? "玩家朝向" : "摄像机朝向")}");
        }
    }

    public override void OnDeselect()
    {
        // 确保钩爪在切换道具时被释放
        if (_grappler != null)
        {
            _grappler.StopGrapple();
        }

        // 清理轨迹渲染器
        if (_trajectoryRenderer != null)
        {
            if (_trajectoryRenderer.gameObject != null)
            {
                Object.DestroyImmediate(_trajectoryRenderer.gameObject);
            }
            _trajectoryRenderer = null;
        }

        _isAiming = false;
    }

    public override void OnUnready()
    {
        OnDeselect();
    }

    // 获取钩爪状态（供其他系统查询）
    public bool IsCurrentlyGrappling()
    {
        return _grappler != null && _grappler.IsGrappling();
    }

    public bool IsCurrentlyPulling()
    {
        return _grappler != null && _grappler.IsPulling();
    }

    public float GetCurrentRopeLength()
    {
        return _grappler != null ? _grappler.GetRopeLength() : 0f;
    }

    public Vector3 GetCurrentAttachPoint()
    {
        return _grappler != null ? _grappler.GetAttachPoint() : Vector3.zero;
    }

    public Vector3 GetCurrentFirePoint()
    {
        return _grappler != null ? _grappler.GetFirePoint() : Vector3.zero;
    }
}