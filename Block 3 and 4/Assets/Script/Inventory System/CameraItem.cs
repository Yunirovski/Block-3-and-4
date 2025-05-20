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
        // Save camera and add sound player
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
        // Update help text with UI Manager
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
        // Use right mouse button to toggle camera mode instead of Q key
        if (Input.GetMouseButtonDown(1))
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
        // Turn on camera mode
        isCamMode = true;
        justEntered = true;
        nextShotTime = 0f;

        if (currentModel != null)
            currentModel.SetActive(false);

        // Show camera screen
        UIManager.Instance.EnterCameraMode();
    }

    void ExitCameraMode()
    {
        // Turn off camera mode
        if (!isCamMode) return;
        isCamMode = false;

        if (currentModel != null)
            currentModel.SetActive(true);

        // Hide camera screen
        UIManager.Instance.ExitCameraMode();
    }

    void ResetUI()
    {
        // Reset to normal state
        isCamMode = false;

        // Show normal screen
        UIManager.Instance.SetCameraHUDVisible(false);
        UIManager.Instance.SetMainHUDVisible(true);
    }

    void TryShoot()
    {
        // Check if camera is ready
        if (Time.time < nextShotTime)
        {
            float remain = nextShotTime - Time.time;
            UIManager.Instance.UpdateCameraDebugText($"Cooldown {remain:F1}s");
            return;
        }

        // Check if we have film
        if (ConsumableManager.Instance == null || !ConsumableManager.Instance.UseFilm())
        {
            UIManager.Instance.UpdateCameraDebugText("No film left");
            return;
        }

        // Take the photo
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
        // Removed ExitCameraMode() to keep in camera mode after taking a photo
    }

    void ProcessShot(Texture2D tex)
    {
        // Make folders for each animal type
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
            // No animal in photo, save to "nothing" folder
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

            // Animal is in photo, save to animal folder
            targetFolder = bestAE.animalName;
            // If folder not made yet, make it now
            string animalFolder = Path.Combine(baseDir, targetFolder);
            if (!Directory.Exists(animalFolder))
            {
                Directory.CreateDirectory(animalFolder);
                Debug.Log($"Made new folder for animal: {animalFolder}");
            }

            bestAE.TriggerEvent(Path.Combine(baseDir, targetFolder, $"{targetFolder}_{uniqueId}.png"), final);
            UIManager.Instance.UpdateCameraResultText($"{bestAE.animalName}: {final}★ (近:{nearCount} 扣:{penalty})");
        }

        // Make file name with animal name and date
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

                // Show full photo alert
                UIManager.Instance.ShowPhotoFullAlert(bestAE.animalName, path, cache[bestAE].stars);
            }
        }
    }
}