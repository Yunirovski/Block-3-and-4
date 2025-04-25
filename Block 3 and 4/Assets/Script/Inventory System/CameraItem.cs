using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

/// <summary>
/// Camera item: hides UI, captures a screenshot, scores it, triggers AnimalEvent,
/// consumes film, enforces a cooldown, and updates on-screen texts.
/// </summary>
[CreateAssetMenu(menuName = "Items/CameraItem_Tag")]
public class CameraItem : BaseItem
{
    // ───────────────────────── Inspector ─────────────────────────
    [Header("Detection Settings")]
    [Tooltip("World-space radius around the pivot point to search for detectable objects.")]
    public float detectRadius = 2f;

    [Tooltip("LayerMask specifying which layers to include in raycast and OverlapSphere.")]
    public LayerMask detectMask;

    [Header("Capture Settings")]
    [Tooltip("Cooldown (seconds) between two consecutive shots.")]
    public float shootCooldown = 1.5f;

    // ───────────────────────── Runtime refs (injected) ─────────────────────────
    [System.NonSerialized] private Camera cam;
    [Header("UI Text (assign in InventorySystem)")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text resultText;


    // ───────────────────────── Private state ─────────────────────────
    private int photoCount;          // Naming counter
    private bool photoMode;           // Ready state flag
    private const string TagAnimal = "AnimalDetectable";

    private static float s_LastShotTime = -999f; // shared cooldown

    // ───────────────────────── Init helpers ─────────────────────────
    public void Init(Camera camera) => cam = camera;

    public void InitUI(TMP_Text debug, TMP_Text result)
    {
        debugText = debug;
        resultText = result;
    }

    // ───────────────────────── Item life-cycle ─────────────────────────
    public override void OnReady()
    {
        photoMode = true;
        debugText?.SetText("Photo Mode: ON");
    }

    public override void OnUnready()
    {
        photoMode = false;
        debugText?.SetText("Photo Mode: OFF");
    }

    // ───────────────────────── Primary action ─────────────────────────
    public override void OnUse()
    {
        if (!photoMode || cam == null) return;

        // 1) cooldown check
        float remainingCd = shootCooldown - (Time.time - s_LastShotTime);
        if (remainingCd > 0f)
        {
            debugText?.SetText($"Camera cooling… {remainingCd:F1}s");
            return;
        }

        // 2) film check
        if (!ConsumableManager.Instance.UseFilm())
        {
            debugText?.SetText("胶卷已用尽！");
            return;
        }

        s_LastShotTime = Time.time;
        ScreenshotHelper.Instance.StartCoroutine(CaptureCoroutine());
    }

    // ───────────────────────── Capture coroutine ─────────────────────────
    private IEnumerator CaptureCoroutine()
    {
        // 1) Disable all canvases
#if UNITY_2023_1_OR_NEWER
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var canvases = Object.FindObjectsOfType<Canvas>();
#endif
        var states = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            states[i] = canvases[i].enabled;
            canvases[i].enabled = false;
        }

        // 2) wait end-of-frame
        yield return new WaitForEndOfFrame();

        // 3) read pixels
        var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        // 4) restore canvases
        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = states[i];

        // 5) analyse & save
        ProcessShot(tex);
    }

    // ───────────────────────── Analyse / save ─────────────────────────
    private void ProcessShot(Texture2D tex)
    {
        /* 1. save PNG */
        string fileName = $"photo_{photoCount:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCount++;
        debugText?.SetText($"Saved {fileName}");

        /* 2. get world pivot */
        Vector3 scrCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = cam.ScreenPointToRay(scrCenter);
        Vector3 pivot = ray.origin + ray.direction * 100f;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, detectMask))
            pivot = hit.point;

        /* 3. detect animals */
        Collider[] hits = Physics.OverlapSphere(pivot, detectRadius, detectMask);
        string uiMsg = "Nothing detected";

        foreach (var col in hits)
        {
            if (!col.CompareTag(TagAnimal)) continue;

            AnimalEvent ani = col.GetComponent<AnimalEvent>();
            if (ani == null) continue;

            // score & trigger
            ScoreResult res = PhotoScorer.Evaluate(cam, col.bounds, ani.rarityLevel);
            ani.TriggerEvent(path, res.stars);

            uiMsg = $"{ani.animalName} Rating: {res.stars}★";
            break; // only first hit
        }

        resultText?.SetText(uiMsg);
    }
}
