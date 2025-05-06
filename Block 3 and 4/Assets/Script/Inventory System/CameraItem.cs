// Assets/Scripts/Items/CameraItem.cs
using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem")]
public class CameraItem : BaseItem
{
    /* ─────────── Inspector ─────────── */
    [Header("Settings")]
    public float shootCooldown = 1f;          // 两次拍照最短间隔（秒）

    [Header("Injected UI")]
    public Canvas mainCanvas;               // 常规 HUD Canvas
    public Canvas cameraCanvas;             // 取景框 HUD Canvas
    public TMP_Text debugText;                // 左上调试文字
    public TMP_Text resultText;               // 拍照评分文字

    /* ─────────── 运行时 ─────────── */
    [System.NonSerialized] public Camera cam; // 主相机（由 Inventory 注入）

    bool isCamMode;      // 是否处于取景模式
    bool justEntered;    // 是否刚刚进入取景（用来忽略首帧左键）
    float nextShotTime;   // 下一次允许拍照的时间
    int photoCnt;       // 已拍照片计数

    /* ======================= 注 入 ======================= */
    public void Init(Camera c, Canvas main, Canvas camHud,
                     TMP_Text dbg, TMP_Text res)
    {
        cam = c;
        mainCanvas = main;
        cameraCanvas = camHud;
        debugText = dbg;
        resultText = res;
        ResetUI();                      // 保证初始 UI 正确
    }

    /* ==================== Inventory 生命周期 ==================== */
    public override void OnSelect(GameObject model) => ResetUI();
    public override void OnDeselect() => ExitCameraMode();
    public override void OnReady() => debugText?.SetText("按 Q 进入相机模式");
    public override void OnUnready() => ExitCameraMode();

    /// <summary>第一次左键→仅进入取景，不拍照</summary>
    public override void OnUse()
    {
        if (!isCamMode) EnterCameraMode();
    }

    /* ======================= 输入处理 ======================= */
    public void HandleInput()
    {
        /* Q 键切换取景 */
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isCamMode) ExitCameraMode();
            else EnterCameraMode();
        }

        /* 取景状态下检测左键拍照 */
        if (isCamMode && Input.GetMouseButtonDown(0))
        {
            if (justEntered)            // 忽略进入取景当帧的那一次点击
            {
                justEntered = false;
                return;
            }
            TryShoot();
        }
    }

    /* ======================== UI 切换 ======================== */
    void EnterCameraMode()
    {
        isCamMode = true;
        justEntered = true;            // 标记首帧
        nextShotTime = 0f;              // 清零冷却，保证可立即拍
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

    /* ======================== 拍 照 ======================== */
    void TryShoot()
    {
        if (Time.time < nextShotTime)   // 冷却未结束
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
        var cvs = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var cvs = UnityEngine.Object.FindObjectsOfType<Canvas>();
#endif
        // 关闭所有 Canvas 以免录到 UI
        bool[] states = new bool[cvs.Length];
        for (int i = 0; i < cvs.Length; i++) { states[i] = cvs[i].enabled; cvs[i].enabled = false; }

        yield return new WaitForEndOfFrame();

        Texture2D tex = new(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        // 恢复 Canvas 可见性
        for (int i = 0; i < cvs.Length; i++) cvs[i].enabled = states[i];

        ProcessShot(tex);
        ExitCameraMode();              // 拍完自动退出取景
    }

    /* ======================== 评分 & 进度 ======================== */
    void ProcessShot(Texture2D tex)
    {
        /* 1) 保存 PNG */
        string fname = $"photo_{photoCnt:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fname);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCnt++;
        debugText?.SetText($"已保存 {fname}");

        /* 2) 评分 */
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

        /* 3) 多目标惩罚 & 彩蛋奖励 */
        int penalty = Mathf.Max(0, targets - 1) * PhotoDetector.Instance.multiTargetPenalty;
        int final = Mathf.Clamp(bestStars - penalty, 1, 4);
        if (bestAE.isEasterEgg) final = Mathf.Clamp(final + 1, 1, 5);

        /* 4) 触发进度 */
        bestAE.TriggerEvent(path, final);
        resultText?.SetText($"{bestAE.animalName}: {final}★  (targets:{targets}  -{penalty})");
    }
}
