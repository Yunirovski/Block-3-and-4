// Assets/Scripts/Items/CameraItem_Tag.cs

using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

/// <summary>
/// A camera item that lets the player take photos of animals.
/// - When readied, enters photo mode.
/// - Left-click to capture (with a cooldown and film cost).
/// - Hides all UI canvases for a clean screenshot.
/// - Saves the image to disk.
/// - Casts a ray from the screen center to determine a detection pivot.
/// - Uses an overlap sphere to find any AnimalEvent targets.
/// - Invokes the PhotoDetector to score the framing and triggers the animal’s reaction.
/// </summary>
[CreateAssetMenu(menuName = "Items/CameraItem_Tag")]
public class CameraItem : BaseItem
{
    [Header("Detection Settings")]
    [Tooltip("Radius (in world units) around the raycast pivot to detect animals.")]
    public float detectRadius = 2f;

    [Tooltip("Layer mask to filter which colliders are considered for detection.")]
    public LayerMask detectMask;

    [Header("Capture Settings")]
    [Tooltip("Time (seconds) the player must wait between shots.")]
    public float shootCooldown = 1.5f;

    // Injected at runtime: reference to the main Camera for screen-to-world conversions
    [System.NonSerialized] private Camera cam;

    // Injected at runtime: UI text for debug and result messages
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text resultText;

    // Tracks when the next shot is allowed
    private static float s_LastShotTime = -999f;

    // Counter to generate unique filenames for saved photos
    private int photoCount;

    // Whether the camera is currently in photo mode (set on ready/unready)
    private bool photoMode;

    /// <summary>
    /// Injects the Camera reference at runtime (called by InventorySystem).
    /// </summary>
    public void Init(Camera camera) => cam = camera;

    /// <summary>
    /// Injects the UI text components at runtime.
    /// </summary>
    public void InitUI(TMP_Text dbg, TMP_Text res)
    {
        debugText = dbg;
        resultText = res;
    }

    /// <summary>
    /// Called when the player equips (readies) the camera.
    /// Enables photo mode and displays a status message.
    /// </summary>
    public override void OnReady()
    {
        photoMode = true;
        debugText?.SetText("Photo Mode ON");
    }

    /// <summary>
    /// Called when the player holsters (unreadies) the camera.
    /// Disables photo mode and updates the status message.
    /// </summary>
    public override void OnUnready()
    {
        photoMode = false;
        debugText?.SetText("Photo Mode OFF");
    }

    /// <summary>
    /// Called when the player attempts to use the camera (left-click).
    /// - Checks that photo mode is active and a Camera is assigned.
    /// - Enforces cooldown and film availability.
    /// - Starts the screen-capture coroutine if all checks pass.
    /// </summary>
    public override void OnUse()
    {
        if (!photoMode || cam == null)
            return;

        // 1) Cooldown check
        float timeSinceLast = Time.time - s_LastShotTime;
        float remaining = shootCooldown - timeSinceLast;
        if (remaining > 0f)
        {
            debugText?.SetText($"Cooling… {remaining:F1}s");
            return;
        }

        // 2) Film check
        if (!ConsumableManager.Instance.UseFilm())
        {
            debugText?.SetText("No film left!");
            return;
        }

        // 3) Record shot time and start coroutine to capture the frame
        s_LastShotTime = Time.time;
        ScreenshotHelper.Instance.StartCoroutine(CaptureCoroutine());
    }

    /// <summary>
    /// Coroutine that:
    /// - Hides all Canvas objects so UI is not captured.
    /// - Waits until end of frame.
    /// - Reads the screen into a texture.
    /// - Restores the Canvas objects.
    /// - Processes the screenshot for animal detection.
    /// </summary>
    private IEnumerator CaptureCoroutine()
    {
        // Hide all Canvas objects (include inactive ones)
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        bool[] prevStates = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            prevStates[i] = canvases[i].enabled;
            canvases[i].enabled = false;
        }

        // Wait until the frame is fully rendered without UI
        yield return new WaitForEndOfFrame();

        // Capture the screen pixels into a Texture2D
        var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        // Restore UI canvases to their original state
        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = prevStates[i];

        // Continue with processing the captured image
        ProcessShot(tex);
    }

    /// <summary>
    /// Saves the screenshot to disk, determines a world-space pivot for detection,
    /// performs an OverlapSphere to find animals, uses PhotoDetector to score the framing,
    /// invokes the animal’s event, and updates the result UI text.
    /// </summary>
    private void ProcessShot(Texture2D tex)
    {
        // 1) Save the PNG to persistent data
        string fileName = $"photo_{photoCount:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCount++;
        debugText?.SetText($"Saved {fileName}");

        // 2) Compute a world-space pivot:
        //    Shoot a ray from the screen center; if it hits something, use that point.
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = cam.ScreenPointToRay(screenCenter);
        Vector3 pivot = ray.origin + ray.direction * 100f;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, detectMask))
        {
            pivot = hit.point;
        }

        // 3) Detect any animals within detectRadius of the pivot
        Collider[] cols = Physics.OverlapSphere(pivot, detectRadius, detectMask);
        string uiMsg = "Nothing detected";

        foreach (var col in cols)
        {
            if (!col.CompareTag("AnimalDetectable"))
                continue;

            var ae = col.GetComponent<AnimalEvent>();
            if (ae == null)
                continue;

            // 4) Score the photo framing using PhotoDetector
            PhotoResult pr = PhotoDetector.Instance.Detect(cam, col.bounds);
            int stars = pr.totalStars;

            // 5) Trigger the animal’s response (awards stars, plays VFX)
            ae.TriggerEvent(path, stars);

            uiMsg = $"{ae.animalName}: {stars}★";
            break;  // Only handle the first detected animal
        }

        // 6) Update the result text on HUD
        resultText?.SetText(uiMsg);
    }
}
