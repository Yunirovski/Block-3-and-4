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

    // 运行时缓存
    Camera _cam;
    GrappleController _grappler;

    public override void OnSelect(GameObject model)
    {
        _cam = Camera.main;
        if (_cam == null) { Debug.LogError("找不到主相机"); return; }

        // 假设 GrappleController 挂在相机的父对象上（玩家身上）
        _grappler = _cam.GetComponentInParent<GrappleController>();
        if (_grappler == null)
        {
            Debug.LogError("玩家物体上缺少 GrappleController 组件");
            return;
        }

        // 注入钩爪可视化资源
        _grappler.InitializeHook(hookPrefab, hookTravelSpeed, ropeMaterial);
    }

    public override void OnUse()
    {
        if (_grappler == null || _cam == null) return;

        Ray ray = _cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f)
        );
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.isStatic)
            {
                _grappler.StartGrapple(hit.point, pullSpeed);
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
}
