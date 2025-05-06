// Assets/Scripts/Items/CameraItem.cs
using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem")]
public class CameraItem : BaseItem
{
    /* ───── Inspector ───── */
    [Header("Settings")] public float shootCooldown = 1f;    // 两次拍照最短间隔

    [Header("Injected UI")]
    public Canvas mainCanvas;      // 常规 HUD
    public Canvas cameraCanvas;    // 取景框 HUD
    public TMP_Text debugText;
    public TMP_Text resultText;

    /* ───── 运行时 ───── */
    [System.NonSerialized] public Camera cam;   // 由 InventorySystem 注入

    bool isCamMode;      // 是否处于取景模式
    bool justEntered;    // 刚进入取景时，忽略首帧左键
    float nextShotTime;   // 下一次允许拍照的时间
    int photoCnt;       // 已拍照片计数

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
        model.SetActive(false);      // 相机模型隐藏；只显示 UI
        ResetUI();
    }
    public override void OnDeselect() => ExitCameraMode();
    public override void OnReady() => debugText?.SetText("按 Q 进入相机模式");
    public override void OnUnready() => ExitCameraMode();
    public override void OnUse() { if (!isCamMode) EnterCameraMode(); }   // 第一次左键→取景

    /* =========================== 输入监听 ========================== */
    public void HandleInput()
    {
        // Q 键切换取景
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isCamMode) ExitCameraMode();
            else EnterCameraMode();
        }

        // 取景状态：左键拍照
        if (isCamMode && Input.GetMouseButtonDown(0))
        {
            if (justEntered) { justEntered = false; return; }  // 忽略首帧
            TryShoot();
        }
    }

    /* =========================== UI 切换 ========================== */
    void EnterCameraMode()
    {
        isCamMode = true;
        justEntered = true;
        nextShotTime = 0f;                // 清零冷却
        mainCanvas.enabled = false;
        cameraCanvas.enabled = true;
        debugText?.SetText("Camera ON");
    }

    void ExitCameraMode()
    {
        if (!isCamMode) return;
        isCamMode = false;
        cameraCanvas.enabled = false;
        mainCanvas.enabled = true;
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
        // 关闭所有 Canvas，避免拍到 UI
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

        // 恢复 Canvas
        for (int i = 0; i < canvases.Length; i++) canvases[i].enabled = states[i];

        ProcessShot(tex);
        ExitCameraMode();      // 拍完自动退出取景
    }

    void ProcessShot(Texture2D tex)
    {
        /* 1) 保存 PNG */
        string fname = $"photo_{photoCnt:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fname);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCnt++;
        debugText?.SetText($"已保存 {fname}");

        /* 2) 收集动物 & 评分 */
        var animals = Object.FindObjectsOfType<AnimalEvent>();
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);

        int bestStars = 0, targets = 0;
        AnimalEvent bestAE = null;

        foreach (var ae in animals)
        {
            Collider col = ae.GetComponent<Collider>();
            if (!col || !ae.gameObject.activeInHierarchy) continue;
            if (!GeometryUtility.TestPlanesAABB(planes, col.bounds)) continue;

            int stars = PhotoDetector.Instance.ScoreSingle(cam, col.bounds);
            if (stars > 0) targets++;
            if (stars > bestStars) { bestStars = stars; bestAE = ae; }
        }

        if (!bestAE)
        {
            resultText?.SetText("未检测到任何动物");
            return;
        }

        /* 3) 多目标扣分 & 彩蛋奖励 */
        int penalty = Mathf.Max(0, targets - 1) * PhotoDetector.Instance.multiTargetPenalty;
        int final = Mathf.Clamp(bestStars - penalty, 1, 4);
        if (bestAE.isEasterEgg) final = Mathf.Clamp(final + 1, 1, 5);

        /* 4) 汇报进度 & UI */
        bestAE.TriggerEvent(path, final);
        resultText?.SetText($"{bestAE.animalName}: {final}★  (targets:{targets}  -{penalty})");
    }
}
