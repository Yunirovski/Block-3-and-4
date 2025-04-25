// Assets/Scripts/Systems/PhotoDetector.cs
using UnityEngine;
using System.Collections.Generic;

public struct PhotoResult { public int stars; }

public class PhotoDetector : MonoBehaviour
{
    [Header("Size score")]
    [Tooltip("低于该百分比判 0★")][Range(0, 1)] public float minSizePct = 0.15f; // 15 %
    [Tooltip("理想区间上界")][Range(0, 1)] public float idealSizePct = 0.45f; // 45 %

    [Header("Position score (黄金分割)")]
    [Range(0, 1)] public float centerTolerance = 0.20f;   // 越小越严格

    [Header("Corner bonus")]
    [Range(0, 1)] public float cornerPctNeeded = 0.75f;   // ≥75 % 角在屏内 +1★

    [Header("Multi-target penalty")]
    public int multiTargetPenalty = 1;                   // 每多 1 只 −1★

    public static PhotoDetector Instance { get; private set; }
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    /* —— 单个动物打分 —— */
    public int ScoreSingle(Camera cam, Bounds b)
    {
        // 1) 屏幕投影角点
        List<Vector3> vis = new();
        for (int i = 0; i < 8; i++)
        {
            Vector3 s = new(((i & 1) == 0 ? -1 : 1), ((i & 2) == 0 ? -1 : 1), ((i & 4) == 0 ? -1 : 1));
            Vector3 p = cam.WorldToScreenPoint(b.center + Vector3.Scale(b.extents, s));
            if (p.z > 0) vis.Add(p);
        }
        if (vis.Count == 0) return 0;

        // 2) 包围盒
        Vector2 min = vis[0], max = vis[0];
        foreach (var p in vis) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        float sw = Screen.width, sh = Screen.height;
        Rect rect = new(min, max - min);
        float areaPct = rect.width * rect.height / (sw * sh);

        // ---- 面积得分 ----
        int sizeScore = 0;
        if (areaPct >= minSizePct && areaPct <= idealSizePct) sizeScore = 2;
        else if (areaPct >= minSizePct * 0.5f && areaPct <= idealSizePct * 1.2f) sizeScore = 1;

        // ---- 位置得分 (离最近黄金分割点距离) ----
        Vector2 center = rect.center;
        Vector2[] sweet = {
            new(sw*0.333f, sh*0.333f),
            new(sw*0.667f, sh*0.333f),
            new(sw*0.333f, sh*0.667f),
            new(sw*0.667f, sh*0.667f)
        };
        float best = float.MaxValue;
        foreach (var p in sweet) best = Mathf.Min(best, Vector2.Distance(center, p));
        float norm = best / Mathf.Sqrt(sw * sw + sh * sh); // 0~0.707
        int posScore = norm <= centerTolerance ? 1 : 0;

        // ---- 角点 bonus ----
        int inside = 0;
        Rect screen = new(0, 0, sw, sh);
        foreach (var p in vis) if (screen.Contains(p)) inside++;
        int cornerBonus = (inside / (float)vis.Count >= cornerPctNeeded) ? 1 : 0;

        return sizeScore + posScore + cornerBonus;  // 0~4
    }
}
