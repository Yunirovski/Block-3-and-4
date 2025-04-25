// Assets/Scripts/Items/CameraItem_Tag.cs
using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem_Tag")]
public class CameraItem : BaseItem
{
    [Header("Detection Settings")]
    public float detectRadius = 2f;
    public LayerMask detectMask;

    [Header("Capture Settings")]
    public float shootCooldown = 1.5f;

    [System.NonSerialized] private Camera cam;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text resultText;

    private static float s_LastShotTime = -999f;
    private int photoCount;
    private bool photoMode;

    public void Init(Camera camera) => cam = camera;
    public void InitUI(TMP_Text dbg, TMP_Text res)
    {
        debugText = dbg;
        resultText = res;
    }

    public override void OnReady()
    {
        photoMode = true;
        debugText?.SetText("Photo Mode ON");
    }

    public override void OnUnready()
    {
        photoMode = false;
        debugText?.SetText("Photo Mode OFF");
    }

    public override void OnUse()
    {
        if (!photoMode || cam == null) return;

        // 冷却检查
        float cdRem = shootCooldown - (Time.time - s_LastShotTime);
        if (cdRem > 0f)
        {
            debugText?.SetText($"Cooling… {cdRem:F1}s");
            return;
        }
        // 胶卷检查
        if (!ConsumableManager.Instance.UseFilm())
        {
            debugText?.SetText("No film left!");
            return;
        }

        s_LastShotTime = Time.time;
        // ScriptableObject 无法直接 StartCoroutine
        ScreenshotHelper.Instance.StartCoroutine(CaptureCoroutine());
    }

    private IEnumerator CaptureCoroutine()
    {
        // 隐藏所有 Canvas
#if UNITY_2023_1_OR_NEWER
        var cvs = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var cvs = Object.FindObjectsOfType<Canvas>();
#endif
        var states = new bool[cvs.Length];
        for (int i = 0; i < cvs.Length; i++)
        {
            states[i] = cvs[i].enabled;
            cvs[i].enabled = false;
        }

        yield return new WaitForEndOfFrame();

        var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        for (int i = 0; i < cvs.Length; i++)
            cvs[i].enabled = states[i];

        ProcessShot(tex);
    }

    private void ProcessShot(Texture2D tex)
    {
        // 1) 保存图片
        string fileName = $"photo_{photoCount:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCount++;
        debugText?.SetText($"Saved {fileName}");

        // 2) 计算检测中心
        Vector3 screenCenter = new Vector3(Screen.width * .5f, Screen.height * .5f, 0f);
        Ray ray = cam.ScreenPointToRay(screenCenter);
        Vector3 pivot = ray.origin + ray.direction * 100f;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, detectMask))
            pivot = hit.point;

        // 3) OverlapSphere 检测动物
        Collider[] cols = Physics.OverlapSphere(pivot, detectRadius, detectMask);
        string uiMsg = "Nothing detected";

        foreach (var col in cols)
        {
            if (!col.CompareTag("AnimalDetectable")) continue;
            var ae = col.GetComponent<AnimalEvent>();
            if (ae == null) continue;

            // —— 调用单例 PhotoDetector —— 
            PhotoResult pr = PhotoDetector.Instance.Detect(cam, col.bounds);
            int stars = pr.totalStars;

            ae.TriggerEvent(path, stars);

            uiMsg = $"{ae.animalName}: {stars}★";
            break;
        }

        resultText?.SetText(uiMsg);
    }
}
