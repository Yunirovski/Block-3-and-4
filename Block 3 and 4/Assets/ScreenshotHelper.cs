using System.Collections;
using UnityEngine;

public class ScreenshotHelper : MonoBehaviour
{
    public static ScreenshotHelper Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// ���� A����ʱ������������ rt���ȴ�һ֡���ȡ���ز��ص�
    /// </summary>
    public void Capture(RenderTexture rt, Camera cam, System.Action<Texture2D> onFinish)
    {
        StartCoroutine(CaptureRoutine(rt, cam, onFinish));
    }

    private IEnumerator CaptureRoutine(RenderTexture rt, Camera cam, System.Action<Texture2D> onFinish)
    {
        RenderTexture prev = cam.targetTexture;
        cam.targetTexture = rt;               // ���ָ�� RT

        yield return new WaitForEndOfFrame(); // �ȴ���֡��������/����

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        cam.targetTexture = prev;             // ������ԭ����ֹ����
        onFinish?.Invoke(tex);
    }
}       