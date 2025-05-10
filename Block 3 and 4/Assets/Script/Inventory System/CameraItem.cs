// Assets/Scripts/Items/CameraItem.cs
using System.Collections;
using System.Collections.Generic;   // ★ 解决 Dictionary<> 未定义
using System.IO;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem")]
public class CameraItem : BaseItem
{
    /* ───── Inspector ───── */
    [Header("Settings")]
    [Tooltip("两次拍照的最短冷却时间（秒）")]
    public float shootCooldown = 1f;

    [Header("Injected UI")]
    [Tooltip("常规 HUD Canvas")]
    public Canvas mainCanvas;
    [Tooltip("相机取景 HUD Canvas")]
    public Canvas cameraCanvas;
    [Tooltip("取景模式下的提示/冷却文本 (挂在 cameraCanvas 下)")]
    public TMP_Text debugText;
    [Tooltip("取景模式下的结果文本 (挂在 cameraCanvas 下)")]
    public TMP_Text resultText;

    /* ───── Runtime ───── */
    [System.NonSerialized] public Camera cam;

    // 添加对模型的引用
    [System.NonSerialized] private GameObject currentModel;

    bool isCamMode;
    bool justEntered;
    float nextShotTime;
    int photoCnt;

    /* ============================ 注入 ============================ */
    public void Init(Camera c, Canvas main, Canvas camHud,
                     TMP_Text dbg, TMP_Text res)
    {
        cam = c;
        mainCanvas = main;
        cameraCanvas = camHud;
        debugText = dbg;
        resultText = res;
        ResetUI();
    }

    /* ======================== Inventory 回调 ======================= */
    public override void OnSelect(GameObject model)
    {
        // 存储模型引用，但不立即隐藏
        currentModel = model;
        ResetUI();
    }

    public override void OnDeselect()
    {
        ExitCameraMode();
        currentModel = null; // 清除模型引用
    }

    public override void OnReady() => debugText?.SetText("按 Q 进入相机模式");

    public override void OnUnready()
    {
        ExitCameraMode();
        currentModel = null; // 清除模型引用
    }

    public override void OnUse()
    {
        if (!isCamMode) EnterCameraMode();
    }

    /* =========================== 输入监听 ========================== */
    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isCamMode) ExitCameraMode();
            else EnterCameraMode();
        }

        if (isCamMode && Input.GetMouseButtonDown(0))
        {
            if (justEntered) { justEntered = false; return; }
            TryShoot();
        }
    }

    /* =========================== UI 切换 ========================== */
    void EnterCameraMode()
    {
        isCamMode = true;
        justEntered = true;
        nextShotTime = 0f;

        // 隐藏相机模型
        if (currentModel != null)
            currentModel.SetActive(false);

        if (mainCanvas) mainCanvas.enabled = false;
        if (cameraCanvas) cameraCanvas.enabled = true;
        debugText?.SetText("Camera ON");
        resultText?.SetText("");
    }

    void ExitCameraMode()
    {
        if (!isCamMode) return;
        isCamMode = false;

        // 恢复相机模型显示
        if (currentModel != null)
            currentModel.SetActive(true);

        if (cameraCanvas) cameraCanvas.enabled = false;
        if (mainCanvas) mainCanvas.enabled = true;
        debugText?.SetText("Camera OFF");
    }

    void ResetUI()
    {
        isCamMode = false;
        if (cameraCanvas) cameraCanvas.enabled = false;
        if (mainCanvas) mainCanvas.enabled = true;
    }

    /* ============================ 拍 照 ============================ */
    void TryShoot()
    {
        if (Time.time < nextShotTime)
        {
            float remain = nextShotTime - Time.time;
            debugText?.SetText($"冷却 {remain:F1}s");
            return;
        }

        if (ConsumableManager.Instance == null ||
            !ConsumableManager.Instance.UseFilm())
        {
            debugText?.SetText("胶卷不足");
            return;
        }

        nextShotTime = Time.time + shootCooldown;
        ScreenshotHelper.Instance.StartCoroutine(CapRoutine());
    }

    IEnumerator CapRoutine()
    {
#if UNITY_2023_1_OR_NEWER
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                           FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
#endif
        bool[] states = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            states[i] = canvases[i].enabled;
            canvases[i].enabled = false;
        }

        yield return new WaitForEndOfFrame();

        Texture2D tex = new(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = states[i];

        ProcessShot(tex);
        ExitCameraMode();  // 拍完自动退出
    }

    /* ========================= 评分与扣分 ========================= */
    void ProcessShot(Texture2D tex)
    {
        // 1) 保存文件
        string fname = $"photo_{photoCnt:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fname);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCnt++;
        debugText?.SetText($"已保存 {fname}");

        // 2) 收集动物 & 主目标评分
        var animals = Object.FindObjectsOfType<AnimalEvent>();
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        var pd = PhotoDetector.Instance;

        // 阈值：占屏≥5% 且 与主目标距离 ≤2×才算"近"
        const float areaMinPct = 0.05f;
        const float distFactor = 2f;

        int bestStars = 0;
        float bestDist = float.MaxValue;
        AnimalEvent bestAE = null;

        // 缓存：AnimalEvent → (stars,dist,areaPct)
        var cache = new Dictionary<AnimalEvent, (int stars, float dist, float area)>();

        foreach (var ae in animals)
        {
            if (!ae.gameObject.activeInHierarchy) continue;
            var col = ae.GetComponent<Collider>();
            if (col == null) continue;
            if (!GeometryUtility.TestPlanesAABB(planes, col.bounds)) continue;

            int stars = pd.ScoreSingle(cam, col.bounds);
            if (stars <= 0) continue;

            float dist = Vector3.Distance(cam.transform.position, col.bounds.center);
            float area = pd.GetAreaPercent(cam, col.bounds);

            cache[ae] = (stars, dist, area);

            if (stars > bestStars)
            {
                bestStars = stars;
                bestAE = ae;
                bestDist = dist;
            }
        }

        if (bestAE == null)
        {
            resultText?.SetText("未检测到任何动物");
            return;
        }

        // 3) 统计"近"目标数
        int nearCount = 0;
        foreach (var kv in cache.Values)
        {
            var (stars, dist, area) = kv;
            if (area >= areaMinPct && dist <= bestDist * distFactor)
                nearCount++;
        }

        // 4) 多目标扣分 & 彩蛋奖励
        int penalty = Mathf.Max(0, nearCount - 1) * pd.multiTargetPenalty;
        int final = Mathf.Clamp(bestStars - penalty, 1, 4);
        if (bestAE.isEasterEgg)
            final = Mathf.Clamp(final + 1, 1, 5);

        // 5) 汇报进度 & 更新 UI
        bestAE.TriggerEvent(path, final);
        resultText?.SetText($"{bestAE.animalName}: {final}★ (近:{nearCount} 扣:{penalty})");
    }
}