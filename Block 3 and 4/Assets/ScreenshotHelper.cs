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
    /// 方案 A：临时将摄像机输出到 rt，等待一帧后读取像素并回调
    /// </summary>
    public void Capture(RenderTexture rt, Camera cam, System.Action<Texture2D> onFinish)
    {
        StartCoroutine(CaptureRoutine(rt, cam, onFinish));
    }

    private IEnumerator CaptureRoutine(RenderTexture rt, Camera cam, System.Action<Texture2D> onFinish)
    {
        RenderTexture prev = cam.targetTexture;
        cam.targetTexture = rt;               // 输出指向 RT

        yield return new WaitForEndOfFrame(); // 等待本帧（含光照/后处理）

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        cam.targetTexture = prev;             // 立即还原，防止黑屏
        onFinish?.Invoke(tex);
    }
}       