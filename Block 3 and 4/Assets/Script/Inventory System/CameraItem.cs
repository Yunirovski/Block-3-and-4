using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

/// <summary>
/// ScriptableObject-based camera item that captures a screenshot of the game view,
/// hides UI during capture, saves the image, evaluates it with PhotoScorer,
/// triggers detection events on AnimalEvent components, and updates UI texts.
/// </summary>
[CreateAssetMenu(menuName = "Items/CameraItem_Tag")]
public class CameraItem : BaseItem
{
    [Header("Detection Settings")]
    [Tooltip("World-space radius around the pivot point to search for detectable objects.")]
    public float detectRadius = 2f;

    [Tooltip("LayerMask specifying which layers to include in raycast and OverlapSphere.")]
    public LayerMask detectMask;

    // Runtime-injected references
    [System.NonSerialized] private Camera cam;
    [System.NonSerialized] private TMP_Text debugText;
    [System.NonSerialized] private TMP_Text resultText;

    private int photoCount;    // Counter for naming saved photos
    private bool photoMode;    // Whether photo mode is currently active

    private const string TagAnimal = "AnimalDetectable";

    /// <summary>
    /// Injects the Camera reference for screen-to-world projections.
    /// </summary>
    public void Init(Camera camera) => cam = camera;

    /// <summary>
    /// Injects the UI text components for debug and detection result messages.
    /// </summary>
    public void InitUI(TMP_Text debug, TMP_Text result)
    {
        debugText = debug;
        resultText = result;
    }

    /// <summary>
    /// Called when the player readies (equips) this item.
    /// Enables photo mode and updates the debug UI.
    /// </summary>
    public override void OnReady()
    {
        photoMode = true;
        if (debugText != null)
            debugText.text = "Photo Mode: ON";
    }

    /// <summary>
    /// Called when the player un-readies (holsters) this item.
    /// Disables photo mode and updates the debug UI.
    /// </summary>
    public override void OnUnready()
    {
        photoMode = false;
        if (debugText != null)
            debugText.text = "Photo Mode: OFF";
    }

    /// <summary>
    /// Called when the player uses this item (left-click while ready).
    /// Starts the screenshot capture coroutine if photo mode is active.
    /// </summary>
    public override void OnUse()
    {
        if (photoMode && cam != null)
        {
            ScreenshotHelper.Instance.StartCoroutine(CaptureCoroutine());
        }
    }

    /// <summary>
    /// Coroutine that:
    /// 1) Hides all UI canvases.
    /// 2) Waits for end of frame (including post-processing).
    /// 3) Reads back the screen into a Texture2D.
    /// 4) Restores UI canvases.
    /// 5) Processes the captured screenshot.
    /// </summary>
    private IEnumerator CaptureCoroutine()
    {
        // 1) Cache and disable all Canvas components
#if UNITY_2023_1_OR_NEWER
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var canvases = Object.FindObjectsOfType<Canvas>();
#endif
        var previousStates = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            previousStates[i] = canvases[i].enabled;
            canvases[i].enabled = false;
        }

        // 2) Wait for frame rendering to complete
        yield return new WaitForEndOfFrame();

        // 3) Capture screen pixels into a Texture2D
        var screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        // 4) Restore original canvas states
        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = previousStates[i];

        // 5) Process the screenshot (save, detect, score, and update UI)
        ProcessShot(screenshot);
    }

    /// <summary>
    /// Saves the captured Texture2D to disk, performs world-space detection using
    /// a raycast and OverlapSphere, evaluates the photo score, triggers the animal event,
    /// and updates the detection result UI.
    /// </summary>
    /// <param name="tex">The captured screenshot as a Texture2D.</param>
    private void ProcessShot(Texture2D tex)
    {
        // --- 1) Save PNG file ---
        string fileName = $"photo_{photoCount:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCount++;
        if (debugText != null)
            debugText.text = $"Saved {fileName}";

        // --- 2) Determine world‐space pivot via screen‐center raycast ---
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = cam.ScreenPointToRay(screenCenter);
        Vector3 pivotPoint = ray.origin + ray.direction * 100f;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, detectMask))
            pivotPoint = hit.point;

        // --- 3) Detect colliders in radius and process first valid animal ---
        Collider[] hits = Physics.OverlapSphere(pivotPoint, detectRadius, detectMask);
        string uiMessage = "Nothing detected";

        foreach (var col in hits)
        {
            if (!col.CompareTag(TagAnimal))
                continue;

            var animalEvent = col.GetComponent<AnimalEvent>();
            if (animalEvent == null)
                continue;

            // Evaluate photo score and get star rating
            ScoreResult scoreResult = PhotoScorer.Evaluate(cam, col.bounds, animalEvent.rarityLevel);

            // Trigger the animal's detection event (photoPath, starRating)
            animalEvent.TriggerEvent(path, scoreResult.stars);

            // Update result UI with animal name and stars
            uiMessage = $"{animalEvent.animalName} Rating: {scoreResult.stars}★";
            break; // Only process the first detected animal
        }

        if (resultText != null)
            resultText.text = uiMessage;
    }
}
