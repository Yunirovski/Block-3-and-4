// This file helps the camera work in the game
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
    [Tooltip("Cooldown time between photos (seconds)")]
    public float shootCooldown = 1f;

    [Header("Sound")]
    [Tooltip("Camera shutter sound effect")]
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
        UIManager.Instance.UpdateCameraDebugText("Right-click to use camera");
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
        if (isCamMode)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Q))
            {
                ExitCameraMode();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (justEntered)
                {
                    justEntered = false;
                    return;
                }
                TryShoot();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(1))
            {
                EnterCameraMode();
            }
        }
    }

    public override void HandleUpdate()
    {
        HandleInput();
    }

    void EnterCameraMode()
    {
        isCamMode = true;
        justEntered = true;
        nextShotTime = 0f;

        if (currentModel != null)
            currentModel.SetActive(false);

        UIManager.Instance.EnterCameraMode();
    }

    void ExitCameraMode()
    {
        if (!isCamMode) return;
        isCamMode = false;

        if (currentModel != null)
            currentModel.SetActive(true);

        UIManager.Instance.ExitCameraMode();
    }

    void ResetUI()
    {
        isCamMode = false;
        UIManager.Instance.SetCameraHUDVisible(false);
        UIManager.Instance.SetMainHUDVisible(true);
    }

    void TryShoot()
    {
        if (Time.time < nextShotTime)
        {
            float remain = nextShotTime - Time.time;
            UIManager.Instance.UpdateCameraDebugText($"Cooldown {remain:F1}s");
            return;
        }

        if (ConsumableManager.Instance == null || !ConsumableManager.Instance.UseFilm())
        {
            UIManager.Instance.UpdateCameraDebugText("No film left");
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

        // ✅ Flash effect: real light
        TriggerFlashLight();

        if (shutterSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shutterSound);
        }

        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = states[i];

        ProcessShot(tex);
    }

    void TriggerFlashLight()
    {
        if (cam == null) return;

        Debug.Log("⚡ Triggering spot flash...");

        GameObject flash = new GameObject("SpotFlashLight");
        flash.transform.position = cam.transform.position + cam.transform.forward * 0.3f;
        flash.transform.rotation = cam.transform.rotation;

        Light light = flash.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = 60f;
        light.innerSpotAngle = 25f;
        light.range = 90f;
        light.intensity = 1000f;
        light.color = Color.white;
        light.shadows = LightShadows.None;

        // Optional: parent it to camera so it moves with shake or recoil
        flash.transform.SetParent(cam.transform);

        // Auto destroy after 0.1 seconds
        Destroy(flash, 0.5f);
    }




    void ProcessShot(Texture2D tex)
    {
        string baseDir = Application.persistentDataPath;
        string[] folderNames = new string[] {
            "Camel", "Donkey", "Giraffe", "Goat", "Hippo", "Lion", "Pigeon", "Rhino", "nothing"
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
            targetFolder = "nothing";
            UIManager.Instance.UpdateCameraResultText("No animals found");
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

            targetFolder = bestAE.animalName;
            string animalFolder = Path.Combine(baseDir, targetFolder);
            if (!Directory.Exists(animalFolder))
            {
                Directory.CreateDirectory(animalFolder);
                Debug.Log($"Made new folder for animal: {animalFolder}");
            }

            bestAE.TriggerEvent(Path.Combine(baseDir, targetFolder, $"{targetFolder}_{uniqueId}.png"), final);
            UIManager.Instance.UpdateCameraResultText($"{bestAE.animalName}: {final}★ (近:{nearCount} 扣:{penalty})");
        }

        string fname = $"{targetFolder}_{uniqueId}.png";
        string path = Path.Combine(baseDir, targetFolder, fname);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCnt++;

        UIManager.Instance.UpdateCameraDebugText($"Saved to {targetFolder} folder: {fname}");

        if (bestAE != null && PhotoCollectionManager.Instance != null)
        {
            bool added = PhotoCollectionManager.Instance.AddPhoto(bestAE.animalName, path, cache[bestAE].stars);
            if (!added)
            {
                UIManager.Instance.UpdateCameraResultText($"{UIManager.Instance.cameraResultText.text}\nPhoto limit reached ({PhotoLibrary.MaxPerAnimal})");
                UIManager.Instance.ShowPhotoFullAlert(bestAE.animalName, path, cache[bestAE].stars);
            }
        }
    }
}
