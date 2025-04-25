// Assets/Scripts/Systems/PhotoDetector.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �������ÿ�������� + ������
/// </summary>
public struct PhotoResult
{
    public int[] windowStars; // ���� = ���ô�����
    public int totalStars;
}

/// <summary>
/// Inspector �ɵ�����������ϵͳ��
///   * windowMargins  : ÿ������ı������ٷֱ� (0.0 = ȫ��)
///   * windowStars    : ÿ�����Ӧ�ļ�����
/// �����б��ȱ���һ�� (��1)���Ӵ��С��˳����д��
/// </summary>
public class PhotoDetector : MonoBehaviour
{
    // ������������������ Configurable ������������������
    [Tooltip("���ı������İٷֱ�(0-1)���� 0 ��ӦΪ 0 ��ʾȫ����")]
    public List<float> windowMargins = new List<float> { 0f, 0.15f, 0.30f };

    [Tooltip("ÿ�����ڶ�Ӧ������������������������")]
    public List<int> windowStarValues = new List<int> { 1, 2, 3 };

    // ������������������ Singleton setup ������������������
    public static PhotoDetector Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ������������������ Public API ������������������
    /// <summary>
    /// �� CameraItem ���ã����� PhotoResult
    /// </summary>
    public PhotoResult Detect(Camera cam, Bounds bounds)
    {
        if (windowMargins.Count == 0 ||
            windowMargins.Count != windowStarValues.Count)
        {
            Debug.LogError("PhotoDetector ���ô���windowMargins / windowStarValues ���Ȳ�һ�£�");
            return default;
        }

        int layers = windowMargins.Count;
        Rect[] windows = new Rect[layers];
        float sw = Screen.width, sh = Screen.height;

        // ���ݰٷֱ�����ÿ�����
        for (int i = 0; i < layers; i++)
        {
            float m = Mathf.Clamp01(windowMargins[i]);
            float left = sw * m;
            float top = sh * m;
            windows[i] = new Rect(left, top, sw - 2 * left, sh - 2 * top);
        }

        // ȡ 8 ����Ļ��
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

        // �ж�ÿһ��
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
