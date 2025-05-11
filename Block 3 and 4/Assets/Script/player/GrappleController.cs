using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GrappleController : MonoBehaviour
{
    [Header("抓钩移动设置")]
    [Tooltip("将玩家拉向挂点的速度 (m/s)")]
    public float defaultPullSpeed = 5f;
    [Tooltip("距离挂点多近时停止拉拽 (m)")]
    public float stopDistance = 1f;

    [Header("钩爪可视化（外部可赋值）")]
    [Tooltip("抓钩实体模型 Prefab，由 UI 或道具传入")]
    public GameObject hookPrefab;
    [Tooltip("钩爪飞行速度 (m/s)")]
    public float hookTravelSpeed = 50f;
    [Tooltip("绳索材质，用于 LineRenderer")]
    public Material ropeMaterial;

    // 内部状态
    CharacterController _cc;
    GameObject _currentHook;
    LineRenderer _ropeRenderer;
    bool _hookFlying;
    bool _isGrappling;
    Vector3 _hookStartPoint;
    Vector3 _hookTargetPoint;
    Vector3 _grapplePoint;
    float _pullSpeed;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // —— 钩爪抛出阶段 —— 
        if (_hookFlying)
        {
            // 钩爪向目标点飞行
            _currentHook.transform.position = Vector3.MoveTowards(
                _currentHook.transform.position,
                _hookTargetPoint,
                hookTravelSpeed * Time.deltaTime);

            // 更新绳索两端
            _ropeRenderer.SetPosition(0, _hookStartPoint);
            _ropeRenderer.SetPosition(1, _currentHook.transform.position);

            // 到达挂点后切换到拉动阶段
            if (Vector3.Distance(_currentHook.transform.position, _hookTargetPoint) < 0.1f)
            {
                _hookFlying = false;
                _isGrappling = true;
            }
            return;
        }

        // —— 拉拽玩家阶段 —— 
        if (_isGrappling)
        {
            Vector3 delta = _grapplePoint - transform.position;
            delta.y = 0; // 只水平拉
            float dist = delta.magnitude;

            // 到达足够近时结束抓钩
            if (dist <= stopDistance)
            {
                EndGrapple();
                return;
            }

            // 拉玩家
            Vector3 move = delta.normalized * _pullSpeed * Time.deltaTime;
            _cc.Move(move);

            // 绳索始点随玩家更新
            _ropeRenderer.SetPosition(0, transform.position + Vector3.up * _cc.height * 0.5f);
            // 绳索终点保持钩爪位置
            _ropeRenderer.SetPosition(1, _currentHook.transform.position);
        }
    }

    /// <summary>
    /// 外部调用：配置钩爪模型、速度、绳索材质
    /// </summary>
    public void InitializeHook(GameObject hookPrefab, float hookSpeed, Material ropeMat)
    {
        this.hookPrefab = hookPrefab;
        this.hookTravelSpeed = hookSpeed;
        this.ropeMaterial = ropeMat;
    }

    /// <summary>
    /// 外部调用：开始一次抓钩逻辑
    /// </summary>
    public void StartGrapple(Vector3 hitPoint, float pullSpeed)
    {
        _grapplePoint = hitPoint;
        _pullSpeed = pullSpeed > 0f ? pullSpeed : defaultPullSpeed;

        // 计算钩爪从玩家“手”位置抛出的起点
        _hookStartPoint = transform.position + Vector3.up * (_cc.height * 0.5f);

        // 实例化钩爪模型
        if (hookPrefab != null)
        {
            _currentHook = Instantiate(hookPrefab, _hookStartPoint, Quaternion.identity);
        }
        else
        {
            // 如果没指定模型，用一个小球代替
            _currentHook = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _currentHook.transform.position = _hookStartPoint;
            _currentHook.transform.localScale = Vector3.one * 0.2f;
        }

        // 给钩爪挂线
        _ropeRenderer = _currentHook.AddComponent<LineRenderer>();
        _ropeRenderer.positionCount = 2;
        _ropeRenderer.material = ropeMaterial;
        _ropeRenderer.startWidth = 0.05f;
        _ropeRenderer.endWidth = 0.05f;

        // 进入抛出阶段
        _hookTargetPoint = hitPoint;
        _hookFlying = true;
        _isGrappling = false;
    }

    /// <summary>
    /// 外部或内部调用：结束抓钩并清理
    /// </summary>
    public void EndGrapple()
    {
        _hookFlying = false;
        _isGrappling = false;
        if (_currentHook != null) Destroy(_currentHook);
        if (_ropeRenderer != null) Destroy(_ropeRenderer);
    }
}
