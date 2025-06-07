// Assets/Scripts/Player/GrappableSurface.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 可钩住表面组件 - 附加到可以被钩爪钩住的物体上
/// </summary>
public class GrappableSurface : MonoBehaviour, IGrappable
{
    [Header("钩住属性")]
    [Tooltip("是否可以被钩住")]
    public bool canBeGrappled = true;

    [Tooltip("表面强度（越高越难脱落）")]
    [Range(0.1f, 10f)]
    public float surfaceStrength = 1f;

    [Tooltip("表面类型")]
    public SurfaceType surfaceType = SurfaceType.Rock;

    [Header("钩住点设置")]
    [Tooltip("预定义的钩住点（为空则自动计算）")]
    public Transform[] predefinedGrapplePoints;

    [Tooltip("边缘检测精度")]
    [Range(0.1f, 2f)]
    public float edgeDetectionPrecision = 0.5f;

    [Tooltip("最小钩住高度")]
    public float minimumGrappleHeight = 1f;

    [Header("视觉反馈")]
    [Tooltip("钩爪接近时的高亮材质")]
    public Material highlightMaterial;

    [Tooltip("被钩住时的特效")]
    public GameObject attachedEffect;

    [Header("音效")]
    [Tooltip("被钩住时的音效")]
    public AudioClip attachSound;

    [Tooltip("钩爪脱落时的音效")]
    public AudioClip detachSound;

    // 运行时状态
    private bool isCurrentlyGrappled = false;
    private Vector3 lastAttachPoint;
    private GameObject currentEffect;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private AudioSource audioSource;

    [System.Serializable]
    public enum SurfaceType
    {
        Rock,      // 岩石 - 非常稳固
        Metal,     // 金属 - 稳固但可能有噪音
        Wood,      // 木头 - 中等强度
        Concrete,  // 混凝土 - 稳固
        Ice,       // 冰 - 容易滑脱
        Fabric,    // 布料 - 不稳定
        Glass      // 玻璃 - 脆弱，可能破碎
    }

    void Start()
    {
        // 获取渲染器组件
        renderers = GetComponentsInChildren<Renderer>();

        // 保存原始材质
        List<Material> origMats = new List<Material>();
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.materials != null)
            {
                origMats.AddRange(renderer.materials);
            }
        }
        originalMaterials = origMats.ToArray();

        // 获取或创建音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D音效
            audioSource.playOnAwake = false;
        }

        // 根据表面类型调整默认强度
        AdjustStrengthBySurfaceType();
    }

    private void AdjustStrengthBySurfaceType()
    {
        switch (surfaceType)
        {
            case SurfaceType.Rock:
                surfaceStrength = Mathf.Max(surfaceStrength, 2f);
                break;
            case SurfaceType.Metal:
                surfaceStrength = Mathf.Max(surfaceStrength, 1.8f);
                break;
            case SurfaceType.Concrete:
                surfaceStrength = Mathf.Max(surfaceStrength, 1.5f);
                break;
            case SurfaceType.Wood:
                surfaceStrength = Mathf.Max(surfaceStrength, 1f);
                break;
            case SurfaceType.Ice:
                surfaceStrength = Mathf.Min(surfaceStrength, 0.5f);
                break;
            case SurfaceType.Fabric:
                surfaceStrength = Mathf.Min(surfaceStrength, 0.3f);
                break;
            case SurfaceType.Glass:
                surfaceStrength = Mathf.Min(surfaceStrength, 0.2f);
                break;
        }
    }

    public bool CanBeGrappled()
    {
        return canBeGrappled && gameObject.activeInHierarchy;
    }

    public Vector3 GetBestGrapplePoint(Vector3 hookPosition)
    {
        // 优先使用预定义的钩住点
        if (predefinedGrapplePoints != null && predefinedGrapplePoints.Length > 0)
        {
            Vector3 bestPoint = predefinedGrapplePoints[0].position;
            float bestDistance = Vector3.Distance(hookPosition, bestPoint);

            foreach (Transform point in predefinedGrapplePoints)
            {
                if (point == null) continue;

                float distance = Vector3.Distance(hookPosition, point.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = point.position;
                }
            }

            return bestPoint;
        }

        // 自动寻找最佳钩住点
        return FindBestEdgePoint(hookPosition);
    }

    private Vector3 FindBestEdgePoint(Vector3 hookPosition)
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return transform.position;

        Vector3 closestPoint = col.ClosestPoint(hookPosition);
        Vector3 boundsCenter = col.bounds.center;
        Vector3 boundsSize = col.bounds.size;

        // 尝试找到边缘或角落
        Vector3[] candidatePoints = {
            boundsCenter + new Vector3(boundsSize.x/2, boundsSize.y/2, boundsSize.z/2),   // 右上前
            boundsCenter + new Vector3(-boundsSize.x/2, boundsSize.y/2, boundsSize.z/2),  // 左上前
            boundsCenter + new Vector3(boundsSize.x/2, boundsSize.y/2, -boundsSize.z/2),  // 右上后
            boundsCenter + new Vector3(-boundsSize.x/2, boundsSize.y/2, -boundsSize.z/2), // 左上后
            boundsCenter + new Vector3(0, boundsSize.y/2, 0),                             // 顶部中心
        };

        Vector3 bestPoint = closestPoint;
        float bestScore = CalculateGrappleScore(closestPoint, hookPosition);

        foreach (Vector3 candidate in candidatePoints)
        {
            // 确保点在物体表面上
            Vector3 surfacePoint = col.ClosestPoint(candidate);
            float score = CalculateGrappleScore(surfacePoint, hookPosition);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = surfacePoint;
            }
        }

        return bestPoint;
    }

    private float CalculateGrappleScore(Vector3 grapplePoint, Vector3 hookPosition)
    {
        float score = 0f;

        // 高度加分（越高越好）
        score += grapplePoint.y * 0.5f;

        // 距离扣分（越远越不好）
        float distance = Vector3.Distance(grapplePoint, hookPosition);
        score -= distance * 0.1f;

        // 边缘加分
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 centerToPoint = grapplePoint - col.bounds.center;
            float edgeDistance = centerToPoint.magnitude / (col.bounds.size.magnitude * 0.5f);
            score += edgeDistance * 2f; // 越靠近边缘越好
        }

        return score;
    }

    public void OnGrappleAttach(Vector3 attachPoint)
    {
        isCurrentlyGrappled = true;
        lastAttachPoint = attachPoint;

        // 播放附着音效
        if (attachSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attachSound);
        }

        // 创建附着特效
        if (attachedEffect != null)
        {
            currentEffect = Instantiate(attachedEffect, attachPoint, Quaternion.identity);
            currentEffect.transform.SetParent(transform);
        }

        // 应用高亮材质
        ApplyHighlight(true);

        Debug.Log($"钩爪附着到 {gameObject.name} 的 {surfaceType} 表面，强度: {surfaceStrength}");
    }

    public void OnGrappleDetach()
    {
        isCurrentlyGrappled = false;

        // 播放脱离音效
        if (detachSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(detachSound);
        }

        // 移除特效
        if (currentEffect != null)
        {
            Destroy(currentEffect);
            currentEffect = null;
        }

        // 移除高亮
        ApplyHighlight(false);

        Debug.Log($"钩爪从 {gameObject.name} 脱离");
    }

    public float GetSurfaceStrength()
    {
        return surfaceStrength;
    }

    private void ApplyHighlight(bool highlight)
    {
        if (highlightMaterial == null || renderers == null) return;

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            if (highlight)
            {
                // 应用高亮材质
                Material[] highlightMats = new Material[renderer.materials.Length];
                for (int i = 0; i < highlightMats.Length; i++)
                {
                    highlightMats[i] = highlightMaterial;
                }
                renderer.materials = highlightMats;
            }
            else if (originalMaterials != null && originalMaterials.Length > 0)
            {
                // 恢复原始材质
                Material[] originalMats = new Material[renderer.materials.Length];
                int materialIndex = 0;
                for (int i = 0; i < originalMats.Length && materialIndex < originalMaterials.Length; i++)
                {
                    originalMats[i] = originalMaterials[materialIndex];
                    materialIndex++;
                }
                renderer.materials = originalMats;
            }
        }
    }

    public Color GetSurfaceTypeColor()
    {
        switch (surfaceType)
        {
            case SurfaceType.Rock: return Color.gray;
            case SurfaceType.Metal: return Color.cyan;
            case SurfaceType.Wood: return new Color(0.6f, 0.3f, 0.1f);
            case SurfaceType.Concrete: return Color.white;
            case SurfaceType.Ice: return Color.blue;
            case SurfaceType.Fabric: return Color.magenta;
            case SurfaceType.Glass: return Color.clear;
            default: return Color.yellow;
        }
    }

    // 可视化调试
    void OnDrawGizmos()
    {
        if (!canBeGrappled) return;

        // 绘制可钩住区域
        Gizmos.color = isCurrentlyGrappled ? Color.green : Color.yellow;

        if (predefinedGrapplePoints != null)
        {
            foreach (Transform point in predefinedGrapplePoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.3f);
                }
            }
        }

        // 绘制表面类型图标
        Gizmos.color = GetSurfaceTypeColor();
        Vector3 center = GetComponent<Collider>()?.bounds.center ?? transform.position;
        Gizmos.DrawWireCube(center, Vector3.one * 0.5f);
    }

    // 编辑器辅助方法
    [ContextMenu("Auto Setup Grapple Points")]
    private void AutoSetupGrapplePoints()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        // 创建钩住点的父对象
        GameObject pointsParent = new GameObject("GrapplePoints");
        pointsParent.transform.SetParent(transform);
        pointsParent.transform.localPosition = Vector3.zero;

        Vector3 boundsCenter = col.bounds.center;
        Vector3 boundsSize = col.bounds.size;

        // 在物体的四个上角创建钩住点
        Vector3[] positions = {
            boundsCenter + new Vector3(boundsSize.x/2, boundsSize.y/2, boundsSize.z/2),
            boundsCenter + new Vector3(-boundsSize.x/2, boundsSize.y/2, boundsSize.z/2),
            boundsCenter + new Vector3(boundsSize.x/2, boundsSize.y/2, -boundsSize.z/2),
            boundsCenter + new Vector3(-boundsSize.x/2, boundsSize.y/2, -boundsSize.z/2),
        };

        List<Transform> points = new List<Transform>();

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject point = new GameObject($"GrapplePoint_{i}");
            point.transform.SetParent(pointsParent.transform);
            point.transform.position = positions[i];
            points.Add(point.transform);
        }

        predefinedGrapplePoints = points.ToArray();

        Debug.Log($"为 {gameObject.name} 自动创建了 {points.Count} 个钩住点");
    }
}