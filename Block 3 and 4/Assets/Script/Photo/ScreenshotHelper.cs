using System.Collections;
using UnityEngine;

/// <summary>
/// Utility MonoBehaviour for capturing a Camera's rendered output into a Texture2D.
/// Use the <see cref="Capture"/> method to render into a RenderTexture,
/// read its pixels, and receive the result via callback.
/// </summary>
[DisallowMultipleComponent]
public class ScreenshotHelper : MonoBehaviour
{
    /// <summary>
    /// Singleton instance for global access.
    /// </summary>
    public static ScreenshotHelper Instance { get; private set; }

    private void Awake()
    {
        // Enforce singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Optionally: DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Captures the next frame from the specified camera into the given RenderTexture,
    /// then reads the pixels into a new Texture2D and invokes the callback.
    /// </summary>
    /// <param name="rt">RenderTexture to use as the capture target.</param>
    /// <param name="cam">Camera whose output will be rendered into <paramref name="rt"/>.</param>
    /// <param name="onFinish">
    /// Callback fired when capture is complete. Provides the resulting Texture2D.
    /// </param>
    public void Capture(RenderTexture rt, Camera cam, System.Action<Texture2D> onFinish)
    {
        if (rt == null || cam == null)
        {
            Debug.LogError("ScreenshotHelper.Capture: RenderTexture or Camera reference is null.");
            return;
        }

        StartCoroutine(CaptureRoutine(rt, cam, onFinish));
    }

    /// <summary>
    /// Coroutine workflow:
    /// 1. Cache the camera¡¯s current targetTexture.
    /// 2. Set the camera¡¯s targetTexture to <paramref name="rt"/>.
    /// 3. Wait for end of frame (including post-processing).
    /// 4. Read pixels from the active RenderTexture into a Texture2D.
    /// 5. Restore the original camera targetTexture and deactivate the RT.
    /// 6. Invoke the callback with the captured Texture2D.
    /// </summary>
    /// <param name="rt">RenderTexture for capture.</param>
    /// <param name="cam">Camera used for rendering.</param>
    /// <param name="onFinish">Callback to receive the Texture2D.</param>
    private IEnumerator CaptureRoutine(RenderTexture rt, Camera cam, System.Action<Texture2D> onFinish)
    {
        // 1) Cache and assign
        RenderTexture previousTarget = cam.targetTexture;
        cam.targetTexture = rt;

        // 2) Ensure RT is active
        RenderTexture.active = rt;

        // 3) Wait for the frame to finish rendering (lighting, post-processing, etc.)
        yield return new WaitForEndOfFrame();

        // 4) Read pixels into Texture2D
        Texture2D screenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        screenshot.Apply();

        // 5) Cleanup: release active RT and restore camera target
        RenderTexture.active = null;
        cam.targetTexture = previousTarget;

        // 6) Deliver the result
        onFinish?.Invoke(screenshot);
    }
}
