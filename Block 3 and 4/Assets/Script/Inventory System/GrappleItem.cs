using UnityEngine;

[CreateAssetMenu(menuName = "Items/GrappleItem")]
public class GrappleItem : BaseItem
{
    [Header("抓钩参数")]
    public float maxDistance = 20f;
    public float pullSpeed = 5f;

    [Header("钩爪可视化")]
    [Tooltip("抓钩模型Prefab，由美术提供")]
    public GameObject hookPrefab;
    [Tooltip("钩爪飞行速度(m/s)")]
    public float hookTravelSpeed = 50f;
    [Tooltip("绳索材质，用于LineRenderer")]
    public Material ropeMaterial;

    [Header("音效")]
    [Tooltip("抓钩开枪音效")]
    public AudioClip grappleFireSound;
    [Tooltip("音效音量")]
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    // 运行时缓存
    Camera _cam;
    GrappleController _grappler;
    AudioSource _audioSource;

    // 添加一个标志来追踪当前是否可以发射抓钩
    private bool _canFire = true;

    public override void OnSelect(GameObject model)
    {
        _cam = Camera.main;
        if (_cam == null) { Debug.LogError("找不到主相机"); return; }

        // 假设GrappleController挂在相机的父对象上（玩家身上）
        _grappler = _cam.GetComponentInParent<GrappleController>();
        if (_grappler == null)
        {
            Debug.LogError("玩家物体上缺少GrappleController组件");
            return;
        }

        // 创建音频源，如果不存在
        _audioSource = _cam.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = _cam.gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 全局音效
        }

        // 注入钩爪可视化资源
        _grappler.InitializeHook(hookPrefab, hookTravelSpeed, ropeMaterial);

        // 添加到抓钩目标点到达事件的监听
        _grappler.onGrappleComplete += OnGrappleComplete;

        // 初始化为可发射状态
        _canFire = true;
    }

    // 添加一个新的方法，当抓钩到达目标点时调用
    private void OnGrappleComplete()
    {
        // 抓钩已到达目标点，现在可以发射下一次了
        _canFire = true;
        Debug.Log("抓钩已到达目标点，可以发射下一次了");
    }

    public override void OnUse()
    {
        if (_grappler == null || _cam == null) return;

        // 检查是否可以发射
        if (!_canFire)
        {
            Debug.Log("抓钩尚未到达目标点，无法发射");
            return;
        }

        // 播放开枪音效
        if (grappleFireSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(grappleFireSound, soundVolume);
        }

        Ray ray = _cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f)
        );
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.isStatic)
            {
                // 发射抓钩，并将发射状态设为false
                _grappler.StartGrapple(hit.point, pullSpeed);
                _canFire = false;
                Debug.Log("抓钩已发射，等待到达目标点");
            }
            else
            {
                Debug.Log("命中目标非静态，不可附着");
            }
        }
        else
        {
            Debug.Log("射程内未命中任何表面");
        }
    }

    public override void OnDeselect()
    {
        // 取消监听事件
        if (_grappler != null)
        {
            _grappler.onGrappleComplete -= OnGrappleComplete;
            // 结束当前的抓钩状态
            _grappler.EndGrapple();
        }

        // 重置状态
        _canFire = true;
    }
}