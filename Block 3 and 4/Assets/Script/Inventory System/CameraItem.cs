using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem_Tag")]
public class CameraItem : BaseItem
{
    [Header("Detection")]
    public float detectRadius = 2f;
    public LayerMask detectMask;

    [Header("Capture")]
    public float shootCooldown = 1.5f;

    [System.NonSerialized] private Camera cam;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text resultText;

    private static float s_LastShot = -999f;
    private int photoCnt;
    private bool photoMode;

    public void Init(Camera c) => cam = c;
    public void InitUI(TMP_Text dbg, TMP_Text res) { debugText = dbg; resultText = res; }

    public override void OnReady() { photoMode = true; debugText?.SetText("Photo ON"); }
    public override void OnUnready() { photoMode = false; debugText?.SetText("Photo OFF"); }

    public override void OnUse()
    {
        if (!photoMode || cam == null) return;
        float cd = shootCooldown - (Time.time - s_LastShot);
        if (cd > 0) { debugText?.SetText($"Cooling…{cd:F1}s"); return; }
        if (!ConsumableManager.Instance.UseFilm()) { debugText?.SetText("No film!"); return; }

        s_LastShot = Time.time;
        ScreenshotHelper.Instance.StartCoroutine(CapRoutine());
    }

    IEnumerator CapRoutine()
    {
#if UNITY_2023_1_OR_NEWER
        var cvs = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var cvs = Object.FindObjectsOfType<Canvas>();
#endif
        bool[] s = new bool[cvs.Length];
        for (int i = 0; i < cvs.Length; i++) { s[i] = cvs[i].enabled; cvs[i].enabled = false; }

        yield return new WaitForEndOfFrame();

        Texture2D t = new(Screen.width, Screen.height, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); t.Apply();

        for (int i = 0; i < cvs.Length; i++) cvs[i].enabled = s[i];

        ProcessShot(t);
    }

    void ProcessShot(Texture2D tex)
    {
        string name = $"photo_{photoCnt:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, name);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCnt++; debugText?.SetText($"Saved {name}");

        Vector3 sc = new(Screen.width * .5f, Screen.height * .5f, 0);
        Ray r = cam.ScreenPointToRay(sc);
        Vector3 pivot = r.origin + r.direction * 100;
        if (Physics.Raycast(r, out var h, 100, detectMask)) pivot = h.point;

        Collider[] cols = Physics.OverlapSphere(pivot, detectRadius, detectMask);
        if (cols.Length == 0) { resultText?.SetText("Nothing detected"); return; }

        int animals = 0, bestStars = 0; AnimalEvent bestAE = null;
        foreach (var c in cols)
        {
            if (!c.CompareTag("AnimalDetectable")) continue;
            var ae = c.GetComponent<AnimalEvent>(); if (ae == null) continue;

            animals++;
            var pr = PhotoDetector.Instance.DetectSingle(cam, c.bounds);
            if (pr.totalStars > bestStars) { bestStars = pr.totalStars; bestAE = ae; }
        }

        if (bestAE == null) { resultText?.SetText("Nothing detected"); return; }

        int penalty = (animals - 1) * PhotoDetector.Instance.multiTargetPenalty;
        int finalStars = Mathf.Max(1, bestStars - penalty);

        bestAE.TriggerEvent(path, finalStars);
        resultText?.SetText($"{bestAE.animalName}: {finalStars}★ (-{penalty})");
    }
}
