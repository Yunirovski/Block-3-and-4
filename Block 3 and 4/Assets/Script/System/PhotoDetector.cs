// Assets/Scripts/Systems/PhotoDetector.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �������ÿ�㴰�ڵ����� + ������
/// </summary>
public struct PhotoResult
{
    public int[] windowStars; // ÿ�㴰�ڶ�Ӧ������
    public int totalStars;  // ������
}

/// <summary>
/// MonoBehaviour ������������ Inspector �����ö�㴰�ڵ�������������Ӧ������
/// ���κ�һ��������������󷽣��ǳ���������������Զ������в����ǡ�
/// </summary>
public class PhotoDetector : MonoBehaviour
{
    [Tooltip("����Ļ�ı������ı��� (0~1)����0��ӦΪ0��ȫ����")]
    public List<float> windowMargins = new List<float> { 0f, 0.15f, 0.30f };

    [Tooltip("ÿ�㴰�ڶ�Ӧ������������������������")]
    public List<int> windowStarValues = new List<int> { 1, 2, 3 };

    public static PhotoDetector Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// �������������World BOUNDS������ÿ�㴰�ڵ���������������
    /// </summary>
    public PhotoResult Detect(Camera cam, Bounds bounds)
    {
        int layers = windowMargins.Count;
        if (layers == 0 || layers != windowStarValues.Count)
        {
            Debug.LogError("PhotoDetector ���ô����뱣֤ windowMargins �� windowStarValues ����һ���� �� 1");
            return default;
        }

        float sw = Screen.width, sh = Screen.height;
        // ����ÿ�㴰�ھ���
        Rect[] windows = new Rect[layers];
        for (int i = 0; i < layers; i++)
        {
            float m = Mathf.Clamp01(windowMargins[i]);
            float left = sw * m;
            float bottom = sh * m;
            windows[i] = new Rect(left, bottom, sw - 2 * left, sh - 2 * bottom);
        }

        // ����˸��ǵ���Ļ����
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

        // ������κν�����������󷽣��������ǳ�������Ĭ�ϸ�����
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

        // ���˵�ǰ���ĵ����ڰ�Χ�м���
        var pts = new List<Vector2>();
        foreach (var p in screenPoints)
        {
            if (p.x >= 0f && p.x <= sw && p.y >= 0f && p.y <= sh)
                pts.Add(p);
        }
        // �����ȫ������Ļ�ϣ�ֱ�����
        if (pts.Count == 0)
        {
            return new PhotoResult
            {
                windowStars = new int[layers],
                totalStars = 0
            };
        }

        // ������Щ��� 2D ��Χ��
        float minX = pts[0].x, maxX = pts[0].x;
        float minY = pts[0].y, maxY = pts[0].y;
        foreach (var p in pts)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        // ���ÿ�㴰�ڴ���
        int[] stars = new int[layers];
        int totalStars = 0;
        for (int i = 0; i < layers; i++)
        {
            var w = windows[i];
            // ֻҪ��Χ����ȫ�ڴ����ڣ����ò�����
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
