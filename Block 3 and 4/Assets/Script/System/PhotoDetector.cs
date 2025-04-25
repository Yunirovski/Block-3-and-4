// Assets/Scripts/Systems/PhotoDetector.cs
using System.Collections.Generic;
using UnityEngine;

public struct PhotoResult
{
    public int[] layerStars;   // 每层星数
    public int totalStars;   // 累计
}

/// <summary>
/// 三层矩形评分系统（可在 Inspector 调参）：<br/>
/// - windowMargins   ：每层对屏幕四边收缩的百分比（0 = 全屏）<br/>
/// - windowStarValues：每层对应加星数（长度需相等）<br/><br/>
/// 规则：<br/>
/// ① 可见角点 ≥ cornerPercentNeeded 时才判定该框<br/>
/// ② 包围盒面积需在 minAreaPct – maxAreaPct 范围内<br/>
/// ③ Compare 时允许 edgeTolerancePx 像素的浮动（解决浮点误差）<br/>
/// </summary>
public class PhotoDetector : MonoBehaviour
{
    [Header("Window Config (Lengths must match)")]
    public List<float> windowMargins = new() { 0.00f, 0.15f, 0.30f };
    public List<int> windowStarValues = new() { 1, 1, 2 };

    [Header("Rule-1  Corner percent needed (更宽松 = 更容易得星)")]
    [Range(0f, 1f)] public float cornerPercentNeeded = 0.25f;     // ↓ 25 %

    [Header("Rule-2  Screen-area limits")]
    [Range(0f, 1f)] public float minAreaPct = 0.01f;               // ↑ 1 %
    [Range(0f, 1f)] public float maxAreaPct = 1.00f;               // ↑ 100 %

    [Header("Rule-3  Edge tolerance (px)")]
    public float edgeTolerancePx = 6f;                            // ↑ 6 px

    [Header("Rule-4  Multi-target penalty")]
    public int multiTargetPenalty = 1;                            // 每多 1 只 -1★

    // -------------- Singleton --------------
    public static PhotoDetector Instance { get; private set; }
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    // -------------- Public API --------------
    public PhotoResult DetectSingle(Camera cam, Bounds bounds)
    {
        int L = Mathf.Min(windowMargins.Count, windowStarValues.Count);
        if (L == 0) return default;

        float sw = Screen.width, sh = Screen.height;

        // 1) 生成每层窗口 (已加容差)
        Rect[] wins = new Rect[L];
        for (int i = 0; i < L; i++)
        {
            float m = Mathf.Clamp01(windowMargins[i]);
            float l = sw * m - edgeTolerancePx;
            float b = sh * m - edgeTolerancePx;
            wins[i] = new Rect(l,
                               b,
                               sw - 2 * l + edgeTolerancePx * 2,
                               sh - 2 * b + edgeTolerancePx * 2);
        }

        // 2) 投影 8 角，仅存 z>0
        List<Vector2> vis = new();
        for (int i = 0; i < 8; i++)
        {
            Vector3 s = new(((i & 1) == 0 ? -1 : 1),
                            ((i & 2) == 0 ? -1 : 1),
                            ((i & 4) == 0 ? -1 : 1));
            Vector3 sp = cam.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, s));
            if (sp.z > 0f) vis.Add(new Vector2(sp.x, sp.y));
        }
        if (vis.Count == 0) return default;            // 完全背对

        // 3) 可见包围盒 + 面积过滤
        Vector2 min = vis[0], max = vis[0];
        foreach (var p in vis) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        Rect obj = new(min, max - min);
        float areaPct = obj.width * obj.height / (sw * sh);
        if (areaPct < minAreaPct || areaPct > maxAreaPct) return default;

        // 4) 逐层打星
        int[] stars = new int[L];
        int total = 0;
        for (int w = 0; w < L; w++)
        {
            int inside = 0;
            foreach (var p in vis) if (wins[w].Contains(p)) inside++;
            if (inside / (float)vis.Count >= cornerPercentNeeded &&
                wins[w].Contains(obj.min) && wins[w].Contains(obj.max))
            {
                stars[w] = windowStarValues[w];
                total += stars[w];
            }
        }
        return new PhotoResult { layerStars = stars, totalStars = total };
    }
}
