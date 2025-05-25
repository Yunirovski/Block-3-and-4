// Assets/Scripts/Systems/ProgressionManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Progress and unlock center. Use stars to unlock items:
/// • Each animal can get up to 3 stars
/// • In Inspector, set how many stars are needed for each item
/// • Tick "ManualUnlockX" to unlock that item when game starts
/// • Take photos to get stars and unlock items
/// • Can save and load progress
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Star Limit Settings")]
    [Tooltip("Max stars each animal can get")]
    public int maxStarsPerAnimal = 3;

    [Tooltip("Show message when max stars reached")]
    public bool showMaxStarWarning = true;

    [Header("Item Unlock Thresholds")]
    [Tooltip("Stars needed to unlock grapple")]
    public int grappleStarsNeeded = 6;

    [Tooltip("Stars needed to unlock skateboard")]
    public int skateboardStarsNeeded = 12;

    [Tooltip("Stars needed to unlock dart gun")]
    public int dartGunStarsNeeded = 18;

    [Tooltip("Stars needed to unlock magic wand")]
    public int magicWandStarsNeeded = 24;

    [Header("Item Unlock (ScriptableObjects)")]
    public BaseItem grappleItem;
    public BaseItem skateboardItem;
    public BaseItem dartGunItem;
    public BaseItem magicWandItem;

    [Header("Manual Unlock Flags (Inspector)")]
    [Tooltip("Unlock grapple at start")]
    public bool ManualUnlockGrapple = false;

    [Tooltip("Unlock skateboard at start")]
    public bool ManualUnlockSkateboard = false;

    [Tooltip("Unlock dart gun at start")]
    public bool ManualUnlockDartGun = false;

    [Tooltip("Unlock magic wand at start")]
    public bool ManualUnlockMagicWand = false;

    [Header("Save Settings")]
    [Tooltip("Auto save is on")]
    public bool enableAutoSave = true;

    [Tooltip("Time between auto saves (seconds)")]
    public float autoSaveInterval = 60f;

    [Tooltip("Save file name")]
    public string saveFileName = "player_progress.json";

    // -- Inside data --
    private Dictionary<string, int> bestStars = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> BestStars => bestStars;

    public int TotalStars { get; private set; }
    public int UniqueAnimals { get; private set; }

    public bool HasGrapple { get; private set; }
    public bool HasSkateboard { get; private set; }
    public bool HasDartGun { get; private set; }
    public bool HasMagicWand { get; private set; }

    // Where the save file is
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

    // Timer for auto save
    private float autoSaveTimer = 0f;

    // Did we finish setup
    private bool isInitialized = false;

    /// <summary>Animal star update event (animalKey, newStars)</summary>
    public event Action<string, int> OnAnimalStarUpdated;

    /// <summary>Animal reached max stars (animalKey, maxStars)</summary>
    public event Action<string, int> OnAnimalMaxStarsReached;

    /// <summary>When progress is saved</summary>
    public event Action OnProgressSaved;

    /// <summary>When progress is loaded</summary>
    public event Action OnProgressLoaded;

    [Serializable]
    private class SaveData
    {
        public Dictionary<string, int> bestStars = new Dictionary<string, int>();
        public int totalStars;
        public int uniqueAnimals;
        public bool hasGrapple;
        public bool hasSkateboard;
        public bool hasDartGun;
        public bool hasMagicWand;
        public int maxStarsPerAnimal = 3;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Load saved game
        LoadProgress();

        // Manually unlock (in editor only)
        if (Application.isEditor)
        {
            if (ManualUnlockGrapple && !HasGrapple) UnlockGrapple();
            if (ManualUnlockSkateboard && !HasSkateboard) UnlockSkateboard();
            if (ManualUnlockDartGun && !HasDartGun) UnlockDartGun();
            if (ManualUnlockMagicWand && !HasMagicWand) UnlockMagicWand();
        }

        // Update score
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SetStars(TotalStars);

        isInitialized = true;
    }

    void Update()
    {
        // Handle auto save
        if (enableAutoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveProgress();
                autoSaveTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Add new stars after taking photo. Called by AnimalEvent.
    /// Max stars per animal is limited.
    /// </summary>
    public void RegisterStars(string animalKey, int stars, bool isEasterEgg)
    {
        if (string.IsNullOrEmpty(animalKey) || stars <= 0) return;

        if (bestStars.TryGetValue(animalKey, out int currentStars))
        {
            if (currentStars >= maxStarsPerAnimal)
            {
                if (showMaxStarWarning)
                {
                    ShowPopup($"{animalKey} already has {maxStarsPerAnimal}★");
                }
                OnAnimalMaxStarsReached?.Invoke(animalKey, maxStarsPerAnimal);
                return;
            }
        }

        int cap = isEasterEgg ? 5 : 4;
        int clamped = Mathf.Clamp(stars, 1, cap);

        if (bestStars.TryGetValue(animalKey, out int prev))
        {
            int maxCanAdd = maxStarsPerAnimal - prev;
            int actualStarsToAdd = Mathf.Min(clamped, prev + maxCanAdd) - prev;

            if (actualStarsToAdd <= 0)
            {
                if (showMaxStarWarning)
                {
                    ShowPopup($"{animalKey} has max stars ({maxStarsPerAnimal}★)");
                }
                OnAnimalMaxStarsReached?.Invoke(animalKey, maxStarsPerAnimal);
                return;
            }

            int newStars = prev + actualStarsToAdd;
            newStars = Mathf.Min(newStars, maxStarsPerAnimal);

            bestStars[animalKey] = newStars;
            TotalStars += newStars - prev;

            if (newStars >= maxStarsPerAnimal)
            {
                OnAnimalMaxStarsReached?.Invoke(animalKey, maxStarsPerAnimal);
                if (showMaxStarWarning)
                {
                    ShowPopup($"{animalKey} has {maxStarsPerAnimal}★ now!");
                }
            }
        }
        else
        {
            int newStars = Mathf.Min(clamped, maxStarsPerAnimal);
            bestStars[animalKey] = newStars;
            TotalStars += newStars;
            UniqueAnimals++;

            if (newStars >= maxStarsPerAnimal)
            {
                OnAnimalMaxStarsReached?.Invoke(animalKey, maxStarsPerAnimal);
                if (showMaxStarWarning)
                {
                    ShowPopup($"{animalKey} has {maxStarsPerAnimal}★ now!");
                }
            }
        }

        OnAnimalStarUpdated?.Invoke(animalKey, bestStars[animalKey]);

        var scoreManager = ScoreManager.Instance;
        if (scoreManager != null)
            scoreManager.SetStars(TotalStars);

        CheckItemUnlocks();

        if (isInitialized && enableAutoSave)
        {
            SaveProgress();
        }
    }

    public bool HasMaxStars(string animalKey)
    {
        if (bestStars.TryGetValue(animalKey, out int stars))
        {
            return stars >= maxStarsPerAnimal;
        }
        return false;
    }

    public int GetAnimalStars(string animalKey)
    {
        return bestStars.TryGetValue(animalKey, out int stars) ? stars : 0;
    }

    public int GetRemainingStars(string animalKey)
    {
        int current = GetAnimalStars(animalKey);
        return Mathf.Max(0, maxStarsPerAnimal - current);
    }

    public List<string> GetMaxedAnimals()
    {
        var maxedAnimals = new List<string>();
        foreach (var kvp in bestStars)
        {
            if (kvp.Value >= maxStarsPerAnimal)
            {
                maxedAnimals.Add(kvp.Key);
            }
        }
        return maxedAnimals;
    }

    public string GetProgressSummary()
    {
        int maxedCount = GetMaxedAnimals().Count;
        return $"Animals photographed: {UniqueAnimals}, Perfect shots: {maxedCount}/{UniqueAnimals}, Total stars: {TotalStars}";
    }

    void CheckItemUnlocks()
    {
        if (!HasGrapple && TotalStars >= grappleStarsNeeded) UnlockGrapple();
        if (!HasSkateboard && TotalStars >= skateboardStarsNeeded) UnlockSkateboard();
        if (!HasDartGun && TotalStars >= dartGunStarsNeeded) UnlockDartGun();
        if (!HasMagicWand && TotalStars >= magicWandStarsNeeded) UnlockMagicWand();
    }

    void UnlockGrapple()
    {
        HasGrapple = true;
        InventoryCycler.RegisterItem(grappleItem);
        ShowPopup($"Got {grappleStarsNeeded}★, Grapple unlocked! Press E to equip");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockSkateboard()
    {
        HasSkateboard = true;
        InventoryCycler.RegisterItem(skateboardItem);
        ShowPopup($"Got {skateboardStarsNeeded}★, Skateboard unlocked! Press E to equip");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockDartGun()
    {
        HasDartGun = true;
        InventoryCycler.RegisterItem(dartGunItem);
        ShowPopup($"Got {dartGunStarsNeeded}★, Dart Gun unlocked! Press E to equip");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockMagicWand()
    {
        HasMagicWand = true;
        InventoryCycler.RegisterItem(magicWandItem);
        ShowPopup($"Got {magicWandStarsNeeded}★, Magic Wand unlocked! Press E to equip");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void ShowPopup(string msg)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPopup(msg);
        }
        else
        {
            Debug.Log($"ProgressionManager: {msg}");
        }
    }

    public void SaveProgress()
    {
        try
        {
            SaveData saveData = new SaveData
            {
                bestStars = new Dictionary<string, int>(bestStars),
                totalStars = TotalStars,
                uniqueAnimals = UniqueAnimals,
                hasGrapple = HasGrapple,
                hasSkateboard = HasSkateboard,
                hasDartGun = HasDartGun,
                hasMagicWand = HasMagicWand,
                maxStarsPerAnimal = maxStarsPerAnimal
            };

            string jsonData = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SaveFilePath, jsonData);

            Debug.Log($"Progress saved to: {SaveFilePath}");
            OnProgressSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
    }

    public void LoadProgress()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("Save file not found. Using default.");
            InitializeDefaultProgress();
            return;
        }

        try
        {
            string jsonData = File.ReadAllText(SaveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

            bestStars = new Dictionary<string, int>(saveData.bestStars);
            TotalStars = saveData.totalStars;
            UniqueAnimals = saveData.uniqueAnimals;
            HasGrapple = saveData.hasGrapple;
            HasSkateboard = saveData.hasSkateboard;
            HasDartGun = saveData.hasDartGun;
            HasMagicWand = saveData.hasMagicWand;

            if (saveData.maxStarsPerAnimal > 0)
            {
                maxStarsPerAnimal = saveData.maxStarsPerAnimal;
            }

            if (HasGrapple && grappleItem != null)
                InventoryCycler.RegisterItem(grappleItem);
            if (HasSkateboard && skateboardItem != null)
                InventoryCycler.RegisterItem(skateboardItem);
            if (HasDartGun && dartGunItem != null)
                InventoryCycler.RegisterItem(dartGunItem);
            if (HasMagicWand && magicWandItem != null)
                InventoryCycler.RegisterItem(magicWandItem);

            Debug.Log($"Progress loaded: {TotalStars} stars, {UniqueAnimals} animals, max per animal: {maxStarsPerAnimal}");
            OnProgressLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Load failed: {e.Message}");
            InitializeDefaultProgress();
        }
    }

    private void InitializeDefaultProgress()
    {
        bestStars = new Dictionary<string, int>();
        TotalStars = 0;
        UniqueAnimals = 0;
        HasGrapple = false;
        HasSkateboard = false;
        HasDartGun = false;
        HasMagicWand = false;
    }

    public void ResetProgress()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowConfirmation(
                "Reset Progress",
                "Are you sure? This will delete all stars and items. Can't undo!",
                () => {
                    PerformProgressReset();
                },
                null
            );
        }
        else
        {
            PerformProgressReset();
        }
    }

    private void PerformProgressReset()
    {
        InitializeDefaultProgress();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SetStars(0);

        SaveProgress();
        ShowPopup("Game progress reset");
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveProgress();
        }
    }
}
