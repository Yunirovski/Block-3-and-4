using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

/// <summary>
/// Camera‐based item that captures a screenshot of the game view,
/// hides UI during capture, saves the image, and detects any tagged
/// “AnimalDetectable” objects within a specified world‐space radius.
/// </summary>
[CreateAssetMenu(menuName = "Items/CameraItem_Tag")]
public class CameraItem : BaseItem
{
    [Header("Capture Settings")]
    [Tooltip("Optional RenderTexture for future use; not required for current capture flow.")]
    public RenderTexture textureSource;

    [Tooltip("World‐space radius around the raycast hit point to search for detectable objects.")]
    public float detectRadius = 2f;

    [Tooltip("LayerMask indicating which layers to include in detection.")]
    public LayerMask detectMask;

    // Internal references (injected at runtime)
    [System.NonSerialized] private Camera playerCam;
    [System.NonSerialized] private TMP_Text debugTxt;
    [System.NonSerialized] private TMP_Text detectTxt;

    private int photoCount;    // Incremental counter for naming captures
    private bool photoMode;    // Flag indicating whether capture mode is active

    private const string TAG_ANIMAL = "AnimalDetectable";

    #region Initialization
    /// <summary>
    /// Injects the main Camera reference for screen‐to‐world raycasting.
    /// </summary>
    /// <param name="cam">The player’s Camera component.</param>
    public void Init(Camera cam) => playerCam = cam;

    /// <summary>
    /// Injects the on‐screen debug and detection text components.
    /// </summary>
    /// <param name="dbg">Text for debug/status updates.</param>
    /// <param name="det">Text for detection results.</param>
    public void InitUI(TMP_Text dbg, TMP_Text det)
    {
        debugTxt = dbg;
        detectTxt = det;
    }
    #endregion

    /// <summary>
    /// Called when the player readies (equips) this item.
    /// Enables photo mode and updates the debug UI.
    /// </summary>
    public override void OnReady()
    {
        photoMode = true;
        if (debugTxt != null)
            debugTxt.text = "Photo Mode: ON";
    }

    /// <summary>
    /// Called when the player un‑readies (holsters) this item.
    /// Disables photo mode and updates the debug UI.
    /// </summary>
    public override void OnUnready()
    {
        photoMode = false;
        if (debugTxt != null)
            debugTxt.text = "Photo Mode: OFF";
    }

    /// <summary>
    /// Called when the player uses this item (left‐click).
    /// If photo mode is active, begins the screenshot capture coroutine.
    /// </summary>
    public override void OnUse()
    {
        if (!photoMode || playerCam == null)
            return;

        ScreenshotHelper.Instance.StartCoroutine(CaptureCoroutine());
    }

    /// <summary>
    /// Coroutine that:
    /// 1. Hides all UI canvases.
    /// 2. Waits for end of frame (including post‑processing).
    /// 3. Reads back the screen into a Texture2D.
    /// 4. Restores UI canvases.
    /// 5. Saves the PNG and performs object detection.
    /// </summary>
    private IEnumerator CaptureCoroutine()
    {
        // 1) Cache and disable all Canvas components
#if UNITY_2023_1_OR_NEWER
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var canvases = Object.FindObjectsOfType<Canvas>();
#endif

        bool[] previousStates = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            previousStates[i] = canvases[i].enabled;
            canvases[i].enabled = false;
        }

        // 2) Wait for render to complete (lighting, post-processing, etc.)
        yield return new WaitForEndOfFrame();

        // 3) Read screen pixels into a Texture2D
        var screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        // 4) Restore original UI visibility
        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = previousStates[i];

        // 5) Save file & detect objects
        SaveAndDetect(screenshot);
    }

    /// <summary>
    /// Saves the provided Texture2D as a PNG file and performs detection
    /// of any colliders tagged "AnimalDetectable" within detectRadius.
    /// Invokes the detected animal’s event if found.
    /// </summary>
    /// <param name="tex">Captured screen Texture2D.</param>
    private void SaveAndDetect(Texture2D tex)
    {
        // Build filename & path
        string filename = $"photo_{photoCount:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, filename);

        // Save the PNG to disk
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCount++;

        if (debugTxt != null)
            debugTxt.text = $"Photo saved: {filename}";

        // Compute world pivot via screen‐center raycast
        Vector3 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = playerCam.ScreenPointToRay(screenCenter);
        Vector3 pivotPoint = ray.origin + ray.direction * 100f;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, detectMask))
            pivotPoint = hit.point;

        // OverlapSphere detection around pivotPoint
        Collider[] hits = Physics.OverlapSphere(pivotPoint, detectRadius, detectMask);
        string resultText = "Nothing detected";

        foreach (var col in hits)
        {
            if (!col.CompareTag(TAG_ANIMAL))
                continue;

            var animal = col.GetComponent<AnimalEvent>();
            if (animal == null)
                continue;

            // Trigger the animal’s detection event (adds score, etc.)
            animal.TriggerEvent(path);
            resultText = $"Detected: {animal.animalName}";
            break;
        }

        if (detectTxt != null)
            detectTxt.text = resultText;
    }
}
