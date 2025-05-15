// Assets/Scripts/Items/GrappleItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/GrappleItem")]
public class GrappleItem : BaseItem
{
    [Header("抓钩参数")]
    public float maxDistance = 20f;
    public float pullSpeed = 5f;

    [Header("钩爪可视化")]
    [Tooltip("抓钩模型 Prefab，由美术提供")]
    public GameObject hookPrefab;
    [Tooltip("钩爪飞行速度 (m/s)")]
    public float hookTravelSpeed = 50f;
    [Tooltip("绳索材质，用于 LineRenderer")]
    public Material ropeMaterial;

    [Header("音效")]
    [Tooltip("抓钩开铅音效")]
    public AudioClip grappleFireSound;
    [Tooltip("音效音量")]
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    // 运行时缓存
    Camera _cam;
    GrappleController _grappler;
    AudioSource _audioSource;

    public override void OnSelect(GameObject model)
    {
        _cam = Camera.main;
        if (_cam == null) { Debug.LogError("找不到主相机"); return; }

        // 假设 GrappleController 挂在相机的父对象上（玩家身上）
        _grappler = _cam.GetComponentInParent<GrappleController>();
        if (_grappler == null)
        {
            Debug.LogError("玩家物体上缺少 GrappleController 组件");
            UIManager.Instance.UpdateCameraDebugText("抓钩控制器未找到");
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

        UIManager.Instance.UpdateCameraDebugText("抓钩就绪，点击发射");
    }

    public override void OnUse()
    {
        if (_grappler == null || _cam == null)
        {
            UIManager.Instance.UpdateCameraDebugText("抓钩系统未准备好");
            return;
        }

        // 播放开铅音效
        if (grappleFireSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(grappleFireSound, soundVolume);
        }

        // 从屏幕中心发射射线
        Ray ray = _cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f)
        );

        // 尝试命中
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            // 检查是否命中静态物体（可抓取表面）
            if (hit.collider.gameObject.isStatic)
            {
                _grappler.StartGrapple(hit.point, pullSpeed);
                UIManager.Instance.UpdateCameraDebugText("抓钩附着成功");
            }
            else
            {
                UIManager.Instance.UpdateCameraDebugText("命中目标非静态，不可附着");
            }
        }
        else
        {
            UIManager.Instance.UpdateCameraDebugText("射程内未命中任何表面");
        }
    }
}