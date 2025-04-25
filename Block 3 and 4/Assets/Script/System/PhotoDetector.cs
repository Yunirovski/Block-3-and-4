// Assets/Scripts/Systems/PhotoDetector.cs
using System.Collections.Generic;
using UnityEngine;

public struct PhotoResult { public int stars; }

public class PhotoDetector : MonoBehaviour
{
    [Header("Size score (屏幕面积百分比)")]
    [Range(0, 1)] public float minSizePct = 0.15f;   //  ≤ 给 0/1★
    [Range(0, 1)] public float idealSizePct = 0.45f;   //  ≤ 给 2★

    [Header("Position score (距黄金分割点)")]
    [Range(0, 1)] public float centerTolerance = 0.20f; // ≤ 给 1★

    [Header("Corner bonus (可见角点命中比例)")]
    [Range(0, 1)] public float cornerPctNeeded = 0.75f; // ≥ +1★

    [Header("Multi-target penalty (由 CameraItem 使用)")]
    public int multiTargetPenalty = 1;                  // 每多 1 只 −1★

    public static PhotoDetector Instance { get; private set; }
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    /// <summary>给单只动物打分（0-4★）</summary>
    public int ScoreSingle(Camera cam, Bounds b)
    {
        // —— 投影 8 角，仅保留 z>0 —— //
        List<Vector3> vis = new();
        for (int i = 0; i < 8; i++)
        {
            Vector3 sign = new(((i & 1) == 0 ? -1 : 1),
                               ((i & 2) == 0 ? -1 : 1),
                               ((i & 4) == 0 ? -1 : 1));
            Vector3 sp = cam.WorldToScreenPoint(b.center + Vector3.Scale(b.extents, sign));
            if (sp.z > 0) vis.Add(sp);
        }
        if (vis.Count == 0) return 0; // 全在镜后

        float sw = Screen.width, sh = Screen.height;

        // —— 可见包围盒 & 面积 —— //
        Vector2 min = vis[0], max = vis[0];
        foreach (var p in vis) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        Rect r = new(min, max - min);
        float areaPct = r.width * r.height / (sw * sh);

        int sizeScore =
            areaPct >= minSizePct && areaPct <= idealSizePct ? 2 :
            areaPct >= minSizePct * 0.5f && areaPct <= idealSizePct * 1.2f ? 1 : 0;

        // —— 位置分：离最近黄金分割点距离 —— //
        Vector2 c = r.center;
        Vector2[] sweet =
        {
            new(sw * 0.333f, sh * 0.333f),
            new(sw * 0.667f, sh * 0.333f),
            new(sw * 0.333f, sh * 0.667f),
            new(sw * 0.667f, sh * 0.667f)
        };
        float best = float.MaxValue;
        foreach (var p in sweet) best = Mathf.Min(best, Vector2.Distance(c, p));
        float norm = best / Mathf.Sqrt(sw * sw + sh * sh);          // 0-~0.71
        int posScore = norm <= centerTolerance ? 1 : 0;

        // —— Corner bonus —— //
        int inside = 0;
        Rect screen = new(0, 0, sw, sh);
        foreach (var p in vis) if (screen.Contains(p)) inside++;
        int cornerBonus = inside / (float)vis.Count >= cornerPctNeeded ? 1 : 0;

        return sizeScore + posScore + cornerBonus;                  // 0-4★
    }
}
