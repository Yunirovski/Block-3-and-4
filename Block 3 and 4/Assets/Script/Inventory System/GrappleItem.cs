// Assets/Scripts/Items/GrappleItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/GrappleItem")]
public class GrappleItem : BaseItem
{
    [Header("抓钩参数")]
    public float maxDistance = 40f;  // 增加了最大距离
    public float pullSpeed = 25f;    // 增加了拉力

    [Header("钩爪可视化")]
    [Tooltip("抓钩模型 Prefab，由美术提供")]
    public GameObject hookPrefab;
    [Tooltip("钩爪飞行速度 (m/s)")]
    public float hookTravelSpeed = 50f;  // 调整了飞行速度，配合物理系统
    [Tooltip("绳索材质，用于 LineRenderer")]
    public Material ropeMaterial;
    [Tooltip("钩爪和绳索的颜色")]
    public Color ropeColor = new Color(0.545f, 0.271f, 0.075f);  // 棕色

    [Header("枪口设置")]
    [Tooltip("枪口位置的Transform名称（在模型中查找）")]
    public string muzzlePointName = "MuzzlePoint";
    [Tooltip("如果找不到指定名称，是否使用模型根部作为枪口")]
    public bool useFallbackMuzzle = true;

    [Header("音效")]
    [Tooltip("抓钩开铅音效")]
    public AudioClip grappleFireSound;
    [Tooltip("音效音量")]
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    // 运行时缓存
    Camera _cam;
    GrappleController _grappler;
    AudioSource _audioSource;
    Transform _muzzlePoint;

    public override void OnSelect(GameObject model)
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            Debug.LogError("找不到主相机");
            return;
        }

        // 假设 GrappleController 挂在相机的父对象上（玩家身上）
        _grappler = _cam.GetComponentInParent<GrappleController>();
        if (_grappler == null)
        {
            Debug.LogError("玩家物体上缺少 GrappleController 组件");
            UIManager.Instance.UpdateCameraDebugText("抓钩控制器未找到");
            return;
        }

        // 查找枪口位置
        _muzzlePoint = FindMuzzlePoint(model);
        if (_muzzlePoint == null)
        {
            Debug.LogWarning($"未找到枪口位置 '{muzzlePointName}'，将使用默认位置");
        }
        else
        {
            Debug.Log($"找到枪口位置: {_muzzlePoint.name} at {_muzzlePoint.position}");
        }

        // 创建音频源，如果不存在
        _audioSource = _cam.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = _cam.gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 全局音效
        }

        // 注入钩爪可视化资源和枪口位置
        _grappler.hookPrefab = hookPrefab;
        _grappler.hookSpeed = hookTravelSpeed;
        _grappler.ropeMaterial = ropeMaterial;
        _grappler.ropeColor = ropeColor;
        _grappler.muzzlePoint = _muzzlePoint; // 设置枪口位置
        _grappler.Initialize();

        UIManager.Instance.UpdateCameraDebugText($"抓钩就绪，左键发射 (枪口: {(_muzzlePoint != null ? "已找到" : "使用默认")})");
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

        Vector3 targetPoint;

        // 尝试命中物体
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            // 命中了物体，使用命中点作为目标
            targetPoint = hit.point;

            if (hit.collider.gameObject.isStatic)
            {
                UIManager.Instance.UpdateCameraDebugText("瞄准静态物体，发射抓钩");
            }
            else
            {
                UIManager.Instance.UpdateCameraDebugText("瞄准动态物体，抓钩将尝试穿过");
            }
        }
        else
        {
            // 没有命中物体，向最大射程方向发射
            targetPoint = ray.origin + ray.direction * maxDistance;
            UIManager.Instance.UpdateCameraDebugText("向空中发射抓钩");
        }

        // 发射抓钩（新的物理系统会处理重力和碰撞）
        _grappler.StartGrapple(targetPoint, pullSpeed);
    }

    public override void OnDeselect()
    {
        // 确保抓钩在切换物品时被取消，防止钩爪残留
        if (_grappler != null)
        {
            _grappler.StopGrapple();
        }
        _muzzlePoint = null; // 清理引用
    }

    /// <summary>
    /// 在模型中查找枪口位置
    /// </summary>
    private Transform FindMuzzlePoint(GameObject model)
    {
        if (model == null) return null;

        // 1. 首先尝试按名称查找
        Transform muzzle = FindChildByName(model.transform, muzzlePointName);
        if (muzzle != null)
        {
            Debug.Log($"通过名称找到枪口: {muzzle.name}");
            return muzzle;
        }

        // 2. 尝试查找包含常见枪口名称的子对象
        string[] commonMuzzleNames = { "muzzle", "barrel", "tip", "end", "gun_tip", "weapon_tip" };
        foreach (string name in commonMuzzleNames)
        {
            muzzle = FindChildByNameContains(model.transform, name);
            if (muzzle != null)
            {
                Debug.Log($"通过模糊匹配找到枪口: {muzzle.name}");
                return muzzle;
            }
        }

        // 3. 如果允许fallback，使用模型最前端的子对象
        if (useFallbackMuzzle)
        {
            Transform farthest = FindFarthestChild(model.transform);
            if (farthest != null)
            {
                Debug.Log($"使用最远子对象作为枪口: {farthest.name}");
                return farthest;
            }

            // 4. 最后的fallback：使用模型根部
            Debug.Log("使用模型根部作为枪口位置");
            return model.transform;
        }

        return null;
    }

    /// <summary>
    /// 递归查找指定名称的子对象
    /// </summary>
    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }

    /// <summary>
    /// 递归查找名称包含指定字符串的子对象
    /// </summary>
    private Transform FindChildByNameContains(Transform parent, string nameContains)
    {
        if (parent.name.ToLower().Contains(nameContains.ToLower()))
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildByNameContains(child, nameContains);
            if (result != null) return result;
        }
        return null;
    }

    /// <summary>
    /// 找到距离模型根部最远的子对象（通常是枪口）
    /// </summary>
    private Transform FindFarthestChild(Transform root)
    {
        Transform farthest = null;
        float maxDistance = 0f;

        // 递归检查所有子对象
        CheckFarthestRecursive(root, root, ref farthest, ref maxDistance);

        return farthest != root ? farthest : null; // 不返回根对象本身
    }

    private void CheckFarthestRecursive(Transform root, Transform current, ref Transform farthest, ref float maxDistance)
    {
        if (current != root) // 跳过根对象
        {
            float distance = Vector3.Distance(root.position, current.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = current;
            }
        }

        foreach (Transform child in current)
        {
            CheckFarthestRecursive(root, child, ref farthest, ref maxDistance);
        }
    }
}