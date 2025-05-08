// Assets/Scripts/Items/CameraItem.cs
using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem")]
public class CameraItem : BaseItem
{
    [Header("Settings")]
    [Tooltip("两次拍照的最短冷却时间（秒）")]
    public float shootCooldown = 1f;

    [Header("Injected UI")]
    [Tooltip("常规 HUD Canvas")]
    public Canvas mainCanvas;
    [Tooltip("相机取景 HUD Canvas")]
    public Canvas cameraCanvas;
    [Tooltip("用于显示提示或冷却信息的文本")]
    public TMP_Text debugText;

    // 运行时状态
    [System.NonSerialized] public Camera cam;
    bool isCamMode;
    bool justEntered;
    float nextShotTime;
    int photoCnt;

    /// <summary>
    /// 由 InventorySystem 注入必要的引用
    /// </summary>
    public void Init(Camera c, Canvas main, Canvas camHud, TMP_Text dbg)
    {
        cam = c;
        mainCanvas = main;
        cameraCanvas = camHud;
        debugText = dbg;
        ResetUI();
    }

    // 当此物品被选中（装备到手上）时调用
    public override void OnSelect(GameObject model)
    {
        // 相机没有可见的手持模型
        model.SetActive(false);
        ResetUI();
    }

    public override void OnDeselect() => ExitCameraMode();
    public override void OnReady() => debugText?.SetText("按 Q 进入相机模式");
    public override void OnUnready() => ExitCameraMode();

    // 左键点击时，如果不在相机模式，就进入相机模式
    public override void OnUse()
    {
        if (!isCamMode) EnterCameraMode();
    }

    /// <summary>
    /// 每帧在 InventorySystem.Update 中调用，监听 Q 和 鼠标左键
    /// </summary>
    public void HandleInput()
    {
        // Q 切换取景模式
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isCamMode) ExitCameraMode();
            else EnterCameraMode();
        }

        // 在取景模式下，左键拍照
        if (isCamMode && Input.GetMouseButtonDown(0))
        {
            if (justEntered)
            {
                // 刚打开取景，忽略首帧点击
                justEntered = false;
                return;
            }
            TryShoot();
        }
    }

    void EnterCameraMode()
    {
        isCamMode = true;
        justEntered = true;
        nextShotTime = 0f;              // 重置冷却
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
        // 确保退出或初始时取景 UI 隐藏，主 HUD 可见
        isCamMode = false;
        if (cameraCanvas) cameraCanvas.enabled = false;
        if (mainCanvas) mainCanvas.enabled = true;
    }

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
        var canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var canvases = Object.FindObjectsOfType<Canvas>();
#endif
        // 关闭所有 Canvas，防止 UI 入镜
        bool[] states = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            states[i] = canvases[i].enabled;
            canvases[i].enabled = false;
        }

        yield return new WaitForEndOfFrame();

        // 读取屏幕像素
        Texture2D tex = new(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        // 恢复 Canvas
        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = states[i];

        // 处理拍照结果
        ProcessShot(tex);

        // 拍完自动退出取景
        ExitCameraMode();
    }

    void ProcessShot(Texture2D tex)
    {
        // 1) 保存文件
        string fname = $"photo_{photoCnt:D4}.png";
        string path = Path.Combine(Application.persistentDataPath, fname);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCnt++;

        // 2) 收集动物 & 评分
        AnimalEvent[] animals = Object.FindObjectsOfType<AnimalEvent>();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

        int bestStars = 0;
        int targets = 0;
        AnimalEvent bestAE = null;

        foreach (var ae in animals)
        {
            if (!ae.gameObject.activeInHierarchy) continue;
            Collider col = ae.GetComponent<Collider>();
            if (col == null) continue;
            if (!GeometryUtility.TestPlanesAABB(planes, col.bounds)) continue;

            int stars = PhotoDetector.Instance.ScoreSingle(cam, col.bounds);
            if (stars > 0) targets++;
            if (stars > bestStars)
            {
                bestStars = stars;
                bestAE = ae;
            }
        }

        if (bestAE == null)
        {
            debugText?.SetText("Nothing detected");
            return;
        }

        // 3) 多目标惩罚 & 彩蛋加星
        int penalty = Mathf.Max(0, targets - 1) * PhotoDetector.Instance.multiTargetPenalty;
        int final = Mathf.Clamp(bestStars - penalty, 1, 4);
        if (bestAE.isEasterEgg) final = Mathf.Clamp(final + 1, 1, 5);

        // 4) 汇报进度（ProgressionManager 会触发 AnimalStarUI 更新）
        bestAE.TriggerEvent(path, final);

        debugText?.SetText($"{bestAE.animalName}: {final}★");
    }
}
