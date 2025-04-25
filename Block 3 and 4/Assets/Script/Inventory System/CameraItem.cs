using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem_Tag")]
public class CameraItem : BaseItem
{
    [Header("Capture")] public float shootCooldown = 1.5f;

    [System.NonSerialized] private Camera cam;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text resultText;

    private static float s_LastShot = -999f;
    private int photoCnt;
    private bool photoMode;

    /* ---- 注入 ---- */
    public void Init(Camera c) => cam = c;
    public void InitUI(TMP_Text dbg, TMP_Text res) { debugText = dbg; resultText = res; }

    public override void OnReady() { photoMode = true; debugText?.SetText("Photo ON"); }
    public override void OnUnready() { photoMode = false; debugText?.SetText("Photo OFF"); }

    public override void OnUse()
    {
        if (!photoMode || cam == null) return;

        float cd = shootCooldown - (Time.time - s_LastShot);
        if (cd > 0f) { debugText?.SetText($"Cooling… {cd:F1}s"); return; }
        if (!ConsumableManager.Instance.UseFilm())
        {
            debugText?.SetText("No film!");
            return;
        }

        s_LastShot = Time.time;
        ScreenshotHelper.Instance.StartCoroutine(CapRoutine());
    }

    /* ---------- Capture + Analyse ---------- */
    IEnumerator CapRoutine()
    {
#if UNITY_2023_1_OR_NEWER
        var cvs = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var cvs = Object.FindObjectsOfType<Canvas>();
#endif
        bool[] states = new bool[cvs.Length];
        for (int i = 0; i < cvs.Length; i++) { states[i] = cvs[i].enabled; cvs[i].enabled = false; }

        yield return new WaitForEndOfFrame();

        Texture2D tex = new(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();

        for (int i = 0; i < cvs.Length; i++) cvs[i].enabled = states[i];

        ProcessShot(tex);
    }

    void ProcessShot(Texture2D tex)
    {
        /* 1) Save PNG */
        string fileName = $"photo_{photoCnt:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCnt++;
        debugText?.SetText($"Saved {fileName}");

        /* 2) Gather ALL AnimalEvent in scene & frustum-cull */
        AnimalEvent[] allAnimals = Object.FindObjectsOfType<AnimalEvent>();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

        int starredAnimals = 0;
        int bestStars = 0;
        AnimalEvent bestAE = null;

        foreach (var ae in allAnimals)
        {
            if (!ae.gameObject.activeInHierarchy) continue;

            Collider col = ae.GetComponent<Collider>();
            if (col == null) continue;

            // 只处理在镜头视锥内的动物
            if (!GeometryUtility.TestPlanesAABB(planes, col.bounds)) continue;

            int stars = PhotoDetector.Instance.ScoreSingle(cam, col.bounds);
            if (stars > 0) starredAnimals++;               // 有星才算目标

            if (stars > bestStars)
            {
                bestStars = stars;
                bestAE = ae;
            }
        }

        if (bestAE == null) { resultText?.SetText("Nothing detected"); return; }

        /* 3) 多目标扣分 & 彩蛋加星 */
        int penaltyCount = Mathf.Max(0, starredAnimals - 1);
        int penalty = penaltyCount * PhotoDetector.Instance.multiTargetPenalty;

        int finalStars = Mathf.Clamp(bestStars - penalty, 1, 4);      // 1-4 ★
        if (bestAE.isEasterEgg) finalStars = Mathf.Clamp(finalStars + 1, 1, 5);

        /* 4) Trigger & UI */
        bestAE.TriggerEvent(path, finalStars);
        resultText?.SetText(
            $"{bestAE.animalName}: {finalStars}★  (targets:{starredAnimals}  -{penalty})");
    }
}
