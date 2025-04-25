// Assets/Scripts/Systems/PhotoDetector.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 检测结果：每层框的星数 + 总星数
/// </summary>
public struct PhotoResult
{
    public int[] windowStars; // 长度 = 配置窗口数
    public int totalStars;
}

/// <summary>
/// Inspector 可调的拍照评分系统：
///   * windowMargins  : 每个框对四边收缩百分比 (0.0 = 全屏)
///   * windowStars    : 每个框对应的加星数
/// 两个列表长度必须一致 (≥1)；从大框到小框顺序填写。
/// </summary>
public class PhotoDetector : MonoBehaviour
{
    // ───────── Configurable ─────────
    [Tooltip("对四边收缩的百分比(0-1)。第 0 个应为 0 表示全屏框")]
    public List<float> windowMargins = new List<float> { 0f, 0.15f, 0.30f };

    [Tooltip("每个窗口对应的星数，加起来就是总星数")]
    public List<int> windowStarValues = new List<int> { 1, 2, 3 };

    // ───────── Singleton setup ─────────
    public static PhotoDetector Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ───────── Public API ─────────
    /// <summary>
    /// 由 CameraItem 调用：返回 PhotoResult
    /// </summary>
    public PhotoResult Detect(Camera cam, Bounds bounds)
    {
        if (windowMargins.Count == 0 ||
            windowMargins.Count != windowStarValues.Count)
        {
            Debug.LogError("PhotoDetector 配置错误：windowMargins / windowStarValues 长度不一致！");
            return default;
        }

        int layers = windowMargins.Count;
        Rect[] windows = new Rect[layers];
        float sw = Screen.width, sh = Screen.height;

        // 根据百分比生成每层矩形
        for (int i = 0; i < layers; i++)
        {
            float m = Mathf.Clamp01(windowMargins[i]);
            float left = sw * m;
            float top = sh * m;
            windows[i] = new Rect(left, top, sw - 2 * left, sh - 2 * top);
        }

        // 取 8 角屏幕点
        Vector3[] corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 sign = new Vector3(
                (i & 1) == 0 ? -1 : 1,
                (i & 2) == 0 ? -1 : 1,
                (i & 4) == 0 ? -1 : 1);
            corners[i] = cam.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, sign));
        }

        int[] starPerWin = new int[layers];
        int total = 0;

        // 判断每一层
        for (int w = 0; w < layers; w++)
        {
            bool allIn = true;
            foreach (var sp in corners)
            {
                if (sp.z < 0f || !windows[w].Contains(new Vector2(sp.x, sp.y)))
                {
                    allIn = false;
                    break;
                }
            }
            if (allIn)
            {
                starPerWin[w] = windowStarValues[w];
                total += starPerWin[w];
            }
        }

        return new PhotoResult { windowStars = starPerWin, totalStars = total };
    }
}
