// Assets/Scripts/Systems/PhotoDetector.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 检测结果：每层窗口的星数 + 总星数
/// </summary>
public struct PhotoResult
{
    public int[] windowStars; // 每层窗口对应的星数
    public int totalStars;  // 总星数
}

/// <summary>
/// MonoBehaviour 单例，用于在 Inspector 中配置多层窗口的收缩比例及对应星数。
/// 若任何一个角落在摄像机后方（非常近的情况），会自动给所有层满星。
/// </summary>
public class PhotoDetector : MonoBehaviour
{
    [Tooltip("对屏幕四边收缩的比例 (0~1)，第0项应为0（全屏）")]
    public List<float> windowMargins = new List<float> { 0f, 0.15f, 0.30f };

    [Tooltip("每层窗口对应的星数，加起来就是总星数")]
    public List<int> windowStarValues = new List<int> { 1, 2, 3 };

    public static PhotoDetector Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 给定相机和物体World BOUNDS，返回每层窗口的星数和总星数。
    /// </summary>
    public PhotoResult Detect(Camera cam, Bounds bounds)
    {
        int layers = windowMargins.Count;
        if (layers == 0 || layers != windowStarValues.Count)
        {
            Debug.LogError("PhotoDetector 配置错误：请保证 windowMargins 与 windowStarValues 长度一致且 ≥ 1");
            return default;
        }

        float sw = Screen.width, sh = Screen.height;
        // 构建每层窗口矩形
        Rect[] windows = new Rect[layers];
        for (int i = 0; i < layers; i++)
        {
            float m = Mathf.Clamp01(windowMargins[i]);
            float left = sw * m;
            float bottom = sh * m;
            windows[i] = new Rect(left, bottom, sw - 2 * left, sh - 2 * bottom);
        }

        // 计算八个角的屏幕坐标
        Vector2[] screenPoints = new Vector2[8];
        bool anyBehind = false;
        for (int i = 0; i < 8; i++)
        {
            Vector3 sign = new Vector3(
                (i & 1) == 0 ? -1 : 1,
                (i & 2) == 0 ? -1 : 1,
                (i & 4) == 0 ? -1 : 1);
            Vector3 worldCorner = bounds.center + Vector3.Scale(bounds.extents, sign);
            Vector3 sp = cam.WorldToScreenPoint(worldCorner);
            if (sp.z < 0f)
            {
                anyBehind = true;
            }
            screenPoints[i] = new Vector2(sp.x, sp.y);
        }

        // 如果有任何角落在摄像机后方，视作“非常近”，默认给满星
        if (anyBehind)
        {
            int[] fullStars = new int[layers];
            int total = 0;
            for (int i = 0; i < layers; i++)
            {
                fullStars[i] = windowStarValues[i];
                total += fullStars[i];
            }
            return new PhotoResult { windowStars = fullStars, totalStars = total };
        }

        // 过滤到前方的点用于包围盒计算
        var pts = new List<Vector2>();
        foreach (var p in screenPoints)
        {
            if (p.x >= 0f && p.x <= sw && p.y >= 0f && p.y <= sh)
                pts.Add(p);
        }
        // 如果完全不在屏幕上，直接零分
        if (pts.Count == 0)
        {
            return new PhotoResult
            {
                windowStars = new int[layers],
                totalStars = 0
            };
        }

        // 计算这些点的 2D 包围盒
        float minX = pts[0].x, maxX = pts[0].x;
        float minY = pts[0].y, maxY = pts[0].y;
        foreach (var p in pts)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        // 针对每层窗口打星
        int[] stars = new int[layers];
        int totalStars = 0;
        for (int i = 0; i < layers; i++)
        {
            var w = windows[i];
            // 只要包围盒完全在窗口内，给该层星数
            if (minX >= w.xMin && maxX <= w.xMax &&
                minY >= w.yMin && maxY <= w.yMax)
            {
                stars[i] = windowStarValues[i];
                totalStars += stars[i];
            }
            else
            {
                stars[i] = 0;
            }
        }

        return new PhotoResult
        {
            windowStars = stars,
            totalStars = totalStars
        };
    }
}
