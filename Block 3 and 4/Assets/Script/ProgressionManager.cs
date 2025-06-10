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
    [Tooltip("Stars needed to unlock camera (usually 0)")]
    public int cameraStarsNeeded = 0;

    [Tooltip("Stars needed to unlock grapple")]
    public int grappleStarsNeeded = 6;

    [Tooltip("Stars needed to unlock skateboard")]
    public int skateboardStarsNeeded = 12;

    [Tooltip("Stars needed to unlock dart gun")]
    public int dartGunStarsNeeded = 18;

    [Tooltip("Stars needed to unlock magic wand")]
    public int magicWandStarsNeeded = 24;

    [Header("Item Unlock (ScriptableObjects)")]
    public BaseItem cameraItem;
    public BaseItem grappleItem;
    public BaseItem skateboardItem;
    public BaseItem dartGunItem;
    public BaseItem magicWandItem;

    [Header("Manual Unlock Flags (Inspector)")]
    [Tooltip("Unlock camera at start")]
    public bool ManualUnlockCamera = true;

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

    public bool HasCamera { get; private set; }
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

    // Reference to InventorySystem for direct item unlocking
    private InventorySystem inventorySystem;

    /// <summary>Animal star update event (animalKey, newStars)</summary>
    public event Action<string, int> OnAnimalStarUpdated;

    /// <summary>Animal reached max stars (animalKey, maxStars)</summary>
    public event Action<string, int> OnAnimalMaxStarsReached;

    /// <summary>When progress is saved</summary>
    public event Action OnProgressSaved;

    /// <summary>When progress is loaded</summary>
    public event Action OnProgressLoaded;

    /// <summary>When item is unlocked (itemName)</summary>
    public event Action<string> OnItemUnlocked;

    [Serializable]
    private class SaveData
    {
        public Dictionary<string, int> bestStars = new Dictionary<string, int>();
        public int totalStars;
        public int uniqueAnimals;
        public bool hasCamera;
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
        // Find InventorySystem reference
        inventorySystem = FindObjectOfType<InventorySystem>();
        if (inventorySystem == null)
        {
            Debug.LogError("ProgressionManager: InventorySystem not found! Items cannot be unlocked.");
        }

        // Load saved game
        LoadProgress();

        // Manually unlock (in editor only)
        if (Application.isEditor)
        {
            if (ManualUnlockCamera && !HasCamera) UnlockCamera();
            if (ManualUnlockGrapple && !HasGrapple) UnlockGrapple();
            if (ManualUnlockSkateboard && !HasSkateboard) UnlockSkateboard();
            if (ManualUnlockDartGun && !HasDartGun) UnlockDartGun();
            if (ManualUnlockMagicWand && !HasMagicWand) UnlockMagicWand();
        }

        // Check initial unlocks based on stars
        CheckItemUnlocks();

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
        if (!HasCamera && TotalStars >= cameraStarsNeeded) UnlockCamera();
        if (!HasGrapple && TotalStars >= grappleStarsNeeded) UnlockGrapple();
        if (!HasSkateboard && TotalStars >= skateboardStarsNeeded) UnlockSkateboard();
        if (!HasDartGun && TotalStars >= dartGunStarsNeeded) UnlockDartGun();
        if (!HasMagicWand && TotalStars >= magicWandStarsNeeded) UnlockMagicWand();
    }

    void UnlockCamera()
    {
        HasCamera = true;
        RegisterItemWithInventory(cameraItem, 1); // Camera is slot 0
        ShowPopup($"Camera unlocked! Press E to equip");
        OnItemUnlocked?.Invoke("Camera");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockGrapple()
    {
        HasGrapple = true;
        RegisterItemWithInventory(grappleItem, 2); // Grapple is slot 2
        ShowPopup($"Got {grappleStarsNeeded}★, Grapple unlocked! Press E to equip");
        OnItemUnlocked?.Invoke("Grapple");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockSkateboard()
    {
        HasSkateboard = true;
        RegisterItemWithInventory(skateboardItem, 3); // Skateboard is slot 3
        ShowPopup($"Got {skateboardStarsNeeded}★, Skateboard unlocked! Press E to equip");
        OnItemUnlocked?.Invoke("Skateboard");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockDartGun()
    {
        HasDartGun = true;
        RegisterItemWithInventory(dartGunItem, 4); // Dart Gun is slot 4
        ShowPopup($"Got {dartGunStarsNeeded}★, Dart Gun unlocked! Press E to equip");
        OnItemUnlocked?.Invoke("Dart Gun");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    void UnlockMagicWand()
    {
        HasMagicWand = true;
        RegisterItemWithInventory(magicWandItem, 5); // Magic Wand is slot 5
        ShowPopup($"Got {magicWandStarsNeeded}★, Magic Wand unlocked! Press E to equip");
        OnItemUnlocked?.Invoke("Magic Wand");
        if (isInitialized && enableAutoSave) SaveProgress();
    }

    /// <summary>
    /// Register item with InventorySystem directly
    /// </summary>
    void RegisterItemWithInventory(BaseItem item, int slotIndex)
    {
        if (inventorySystem == null)
        {
            Debug.LogError($"Cannot register {item?.itemName}: InventorySystem not found!");
            return;
        }

        if (item == null)
        {
            Debug.LogError($"Cannot register item at slot {slotIndex}: item is null!");
            return;
        }

        if (slotIndex < 0 || slotIndex >= inventorySystem.availableItems.Count)
        {
            Debug.LogError($"Cannot register {item.itemName}: slot {slotIndex} is out of range!");
            return;
        }

        inventorySystem.availableItems[slotIndex] = item;
        Debug.Log($"Successfully registered {item.itemName} to inventory slot {slotIndex}");
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
                hasCamera = HasCamera,
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
            HasCamera = saveData.hasCamera;
            HasGrapple = saveData.hasGrapple;
            HasSkateboard = saveData.hasSkateboard;
            HasDartGun = saveData.hasDartGun;
            HasMagicWand = saveData.hasMagicWand;

            if (saveData.maxStarsPerAnimal > 0)
            {
                maxStarsPerAnimal = saveData.maxStarsPerAnimal;
            }

            // Re-register unlocked items (will be done in CheckItemUnlocks())
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
        HasCamera = false;
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

        // Clear all items from inventory
        if (inventorySystem != null)
        {
            for (int i = 0; i < inventorySystem.availableItems.Count; i++)
            {
                inventorySystem.availableItems[i] = null;
            }
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SetStars(0);

        SaveProgress();
        ShowPopup("Game progress reset");
    }

    /// <summary>
    /// Manual unlock methods for testing/debugging
    /// </summary>
    [ContextMenu("Force Unlock Camera")]
    public void ForceUnlockCamera()
    {
        if (!HasCamera) UnlockCamera();
    }

    [ContextMenu("Force Unlock Grapple")]
    public void ForceUnlockGrapple()
    {
        if (!HasGrapple) UnlockGrapple();
    }

    [ContextMenu("Force Unlock Skateboard")]
    public void ForceUnlockSkateboard()
    {
        if (!HasSkateboard) UnlockSkateboard();
    }

    [ContextMenu("Force Unlock Dart Gun")]
    public void ForceUnlockDartGun()
    {
        if (!HasDartGun) UnlockDartGun();
    }

    [ContextMenu("Force Unlock Magic Wand")]
    public void ForceUnlockMagicWand()
    {
        if (!HasMagicWand) UnlockMagicWand();
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