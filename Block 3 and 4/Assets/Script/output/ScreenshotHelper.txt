// Assets/Scripts/Systems/ScreenshotHelper.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ScreenshotHelper : MonoBehaviour
{
    private static ScreenshotHelper _instance;
    public static ScreenshotHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ScreenshotHelper(Auto)");
                _instance = go.AddComponent<ScreenshotHelper>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null) { _instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public void Capture(RenderTexture rt, Camera cam, System.Action<Texture2D> onFinish)
    {
        if (rt == null || cam == null)
        {
            Debug.LogError("ScreenshotHelper.Capture: ²ÎÊýÎª null");
            return;
        }
        StartCoroutine(CaptureRoutine(rt, cam, onFinish));
    }

    IEnumerator CaptureRoutine(RenderTexture rt, Camera cam, System.Action<Texture2D> cb)
    {
        RenderTexture prev = cam.targetTexture;
        cam.targetTexture = rt;
        RenderTexture.active = rt;

        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        cam.targetTexture = prev;

        cb?.Invoke(tex);
    }
}
