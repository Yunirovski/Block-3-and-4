// Assets/Scripts/Items/CameraItem.cs
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem")]
public class CameraItem : BaseItem
{
    [Header("Settings")]
    [Tooltip("两次拍照的最短冷却时间（秒）")]
    public float shootCooldown = 1f;

    [Header("Audio")]
    [Tooltip("拍照快门音效")]
    public AudioClip shutterSound;

    [System.NonSerialized] public Camera cam;
    [System.NonSerialized] private GameObject currentModel;
    [System.NonSerialized] private AudioSource audioSource;

    bool isCamMode;
    bool justEntered;
    float nextShotTime;
    int photoCnt;

    public void Init(Camera c)
    {
        cam = c;

        if (cam != null && audioSource == null)
        {
            audioSource = cam.gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = cam.gameObject.AddComponent<AudioSource>();
        }
    }

    public override void OnSelect(GameObject model)
    {
        currentModel = model;
        ResetUI();
    }

    public override void OnDeselect()
    {
        ExitCameraMode();
        currentModel = null;
    }

    public override void OnReady()
    {
        // 通过UIManager更新提示文本
        UIManager.Instance.UpdateCameraDebugText("按 Q 进入相机模式");
    }

    public override void OnUnready()
    {
        ExitCameraMode();
        currentModel = null;
    }

    public override void OnUse()
    {
        if (!isCamMode) EnterCameraMode();
    }

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

    void EnterCameraMode()
    {
        isCamMode = true;
        justEntered = true;
        nextShotTime = 0f;

        if (currentModel != null)
            currentModel.SetActive(false);

        // 使用UIManager进入相机模式
        UIManager.Instance.EnterCameraMode();
    }

    void ExitCameraMode()
    {
        if (!isCamMode) return;
        isCamMode = false;

        if (currentModel != null)
            currentModel.SetActive(true);

        // 使用UIManager退出相机模式
        UIManager.Instance.ExitCameraMode();
    }

    void ResetUI()
    {
        isCamMode = false;

        // 通过UIManager重置UI状态
        UIManager.Instance.SetCameraHUDVisible(false);
        UIManager.Instance.SetMainHUDVisible(true);
    }

    void TryShoot()
    {
        if (Time.time < nextShotTime)
        {
            float remain = nextShotTime - Time.time;
            UIManager.Instance.UpdateCameraDebugText($"冷却 {remain:F1}s");
            return;
        }

        if (ConsumableManager.Instance == null || !ConsumableManager.Instance.UseFilm())
        {
            UIManager.Instance.UpdateCameraDebugText("胶卷不足");
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

        if (shutterSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shutterSound);
        }

        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = states[i];

        ProcessShot(tex);
        ExitCameraMode();
    }

    void ProcessShot(Texture2D tex)
    {
        // Create directories for animal classifications if they don't exist
        string baseDir = Application.persistentDataPath;
        string[] folderNames = new string[] {
            "Bear", "Deer", "Fox", "Rabbit", "Wolf", "Penguin", "Eagle", "Turtle", "test1", "test2"
        };

        foreach (string folderName in folderNames)
        {
            string folderPath = Path.Combine(baseDir, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Debug.Log($"Created directory: {folderPath}");
            }
        }

        var animals = UnityEngine.Object.FindObjectsOfType<AnimalEvent>();
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        var pd = PhotoDetector.Instance;

        const float areaMinPct = 0.05f;
        const float distFactor = 2f;

        int bestStars = 0;
        float bestDist = float.MaxValue;
        AnimalEvent bestAE = null;
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

        string targetFolder;
        string uniqueId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        if (bestAE == null)
        {
            // No animal detected, save to test folders
            targetFolder = UnityEngine.Random.value > 0.5f ? "test1" : "test2";
            UIManager.Instance.UpdateCameraResultText("未检测到任何动物");
        }
        else
        {
            int nearCount = 0;
            foreach (var kv in cache.Values)
            {
                var (stars, dist, area) = kv;
                if (area >= areaMinPct && dist <= bestDist * distFactor)
                    nearCount++;
            }

            int penalty = Mathf.Max(0, nearCount - 1) * pd.multiTargetPenalty;
            int final = Mathf.Clamp(bestStars - penalty, 1, 4);
            if (bestAE.isEasterEgg)
                final = Mathf.Clamp(final + 1, 1, 5);

            // Animal detected, save to its specific folder
            targetFolder = bestAE.animalName;
            // If the folder doesn't exist yet, create it
            string animalFolder = Path.Combine(baseDir, targetFolder);
            if (!Directory.Exists(animalFolder))
            {
                Directory.CreateDirectory(animalFolder);
                Debug.Log($"Created directory for new animal: {animalFolder}");
            }

            bestAE.TriggerEvent(Path.Combine(baseDir, targetFolder, $"{targetFolder}_{uniqueId}.png"), final);
            UIManager.Instance.UpdateCameraResultText($"{bestAE.animalName}: {final}★ (近:{nearCount} 扣:{penalty})");
        }

        // Create the filename with animal type prefix
        string fname = $"{targetFolder}_{uniqueId}.png";
        string path = Path.Combine(baseDir, targetFolder, fname);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCnt++;

        UIManager.Instance.UpdateCameraDebugText($"已保存到 {targetFolder} 文件夹: {fname}");

        if (bestAE != null && PhotoCollectionManager.Instance != null)
        {
            bool added = PhotoCollectionManager.Instance.AddPhoto(bestAE.animalName, path, cache[bestAE].stars);
            if (!added)
            {
                UIManager.Instance.UpdateCameraResultText($"{UIManager.Instance.cameraResultText.text}\n照片已达上限({PhotoLibrary.MaxPerAnimal})");

                // 显示照片已达上限弹窗
                UIManager.Instance.ShowPhotoFullAlert(bestAE.animalName, path, cache[bestAE].stars);
            }
        }
    }
}