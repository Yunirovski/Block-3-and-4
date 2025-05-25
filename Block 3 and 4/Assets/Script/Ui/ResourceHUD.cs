// Assets/Scripts/UI/ResourceHUD.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Resource HUD Display: Shows food, film, and total stars.
/// Auto updates when values change.
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    [Header("UI Text References")]
    [Tooltip("Text that shows food amount")]
    public TMP_Text foodText;

    [Tooltip("Text that shows film amount")]
    public TMP_Text filmText;

    [Tooltip("Text that shows total stars")]
    public TMP_Text starsText;

    [Header("Display Settings")]
    [Tooltip("Food text format, {0}=current, {1}=max")]
    public string foodFormat = "Food: {0}/{1}";

    [Tooltip("Film text format, {0}=current, {1}=max")]
    public string filmFormat = "Film: {0}/{1}";

    [Tooltip("Stars text format, {0}=total stars")]
    public string starsFormat = "Stars: {0}";

    [Header("Color Settings")]
    [Tooltip("Color when resource is okay")]
    public Color normalColor = Color.white;

    [Tooltip("Color when resource is low")]
    public Color lowColor = Color.red;

    [Tooltip("Color when resource is empty")]
    public Color emptyColor = Color.red;

    [Range(0f, 1f)]
    [Tooltip("If lower than this, show as low")]
    public float lowThreshold = 0.3f;

    // Save numbers
    private int currentFood = 0;
    private int maxFood = 0;
    private int currentFilm = 0;
    private int maxFilm = 0;
    private int totalStars = 0;

    // Manager links
    private ConsumableManager consumableManager;
    private ProgressionManager progressionManager;

    private void Awake()
    {
        // Register with UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterResourceHUD(this);
        }
    }

    private void Start()
    {
        // Find managers and listen to their updates
        InitializeManagers();

        // First update
        Refresh();
    }

    private void InitializeManagers()
    {
        // Get ConsumableManager
        consumableManager = ConsumableManager.Instance;
        if (consumableManager != null)
        {
            // Listen to consumable changes
            consumableManager.OnConsumableChanged += OnConsumableChanged;
            Debug.Log("ResourceHUD: Subscribed to ConsumableManager");
        }
        else
        {
            Debug.LogWarning("ResourceHUD: Cannot find ConsumableManager");
        }

        // Get ProgressionManager
        progressionManager = ProgressionManager.Instance;
        if (progressionManager != null)
        {
            // Listen to star changes
            progressionManager.OnAnimalStarUpdated += OnStarsUpdated;
            Debug.Log("ResourceHUD: Subscribed to ProgressionManager");
        }
        else
        {
            Debug.LogWarning("ResourceHUD: Cannot find ProgressionManager");
        }
    }

    /// <summary>
    /// Called when food or film changes
    /// </summary>
    private void OnConsumableChanged()
    {
        UpdateConsumableDisplay();
    }

    /// <summary>
    /// Called when stars change
    /// </summary>
    private void OnStarsUpdated(string animalKey, int stars)
    {
        UpdateStarsDisplay();
    }

    /// <summary>
    /// Update all UI
    /// </summary>
    public void Refresh()
    {
        UpdateConsumableDisplay();
        UpdateStarsDisplay();
    }

    /// <summary>
    /// Show food and film numbers
    /// </summary>
    private void UpdateConsumableDisplay()
    {
        if (consumableManager != null)
        {
            // Save current numbers
            currentFood = consumableManager.Food;
            maxFood = consumableManager.foodCap;
            currentFilm = consumableManager.Film;
            maxFilm = consumableManager.filmCap;

            // Show food
            if (foodText != null)
            {
                foodText.text = string.Format(foodFormat, currentFood, maxFood);
                foodText.color = GetResourceColor(currentFood, maxFood);
            }

            // Show film
            if (filmText != null)
            {
                filmText.text = string.Format(filmFormat, currentFilm, maxFilm);
                filmText.color = GetResourceColor(currentFilm, maxFilm);
            }
        }
    }

    /// <summary>
    /// Show star numbers
    /// </summary>
    private void UpdateStarsDisplay()
    {
        if (progressionManager != null && starsText != null)
        {
            totalStars = progressionManager.TotalStars;
            starsText.text = string.Format(starsFormat, totalStars);
            starsText.color = normalColor; // Stars are always normal color
        }
    }

    /// <summary>
    /// Get color based on how much we have
    /// </summary>
    private Color GetResourceColor(int current, int max)
    {
        if (current == 0)
        {
            return emptyColor;
        }
        else if ((float)current / max <= lowThreshold)
        {
            return lowColor;
        }
        else
        {
            return normalColor;
        }
    }

    /// <summary>
    /// Change text by type (used by UIManager)
    /// </summary>
    public void UpdateText(string type, string value)
    {
        switch (type.ToLower())
        {
            case "food":
                if (foodText != null) foodText.text = value;
                break;
            case "film":
                if (filmText != null) filmText.text = value;
                break;
            case "stars":
                if (starsText != null) starsText.text = value;
                break;
            default:
                Debug.LogWarning($"ResourceHUD: Unknown text type: {type}");
                break;
        }
    }

    /// <summary>
    /// Get summary of all resource numbers
    /// </summary>
    public string GetResourceSummary()
    {
        return $"Food: {currentFood}/{maxFood}, Film: {currentFilm}/{maxFilm}, Stars: {totalStars}";
    }

    /// <summary>
    /// Check if we have enough food and film
    /// </summary>
    public bool HasSufficientResources(int foodNeeded = 0, int filmNeeded = 0)
    {
        return currentFood >= foodNeeded && currentFilm >= filmNeeded;
    }

    /// <summary>
    /// Get warning message if things are low or empty
    /// </summary>
    public string GetResourceWarning()
    {
        var warnings = new List<string>();

        if (currentFood == 0)
            warnings.Add("Food is empty");
        else if ((float)currentFood / maxFood <= lowThreshold)
            warnings.Add("Food is low");

        if (currentFilm == 0)
            warnings.Add("Film is empty");
        else if ((float)currentFilm / maxFilm <= lowThreshold)
            warnings.Add("Film is low");

        return warnings.Count > 0 ? string.Join(", ", warnings) : "";
    }

    private void OnDestroy()
    {
        // Stop listening to events
        if (consumableManager != null)
        {
            consumableManager.OnConsumableChanged -= OnConsumableChanged;
        }

        if (progressionManager != null)
        {
            progressionManager.OnAnimalStarUpdated -= OnStarsUpdated;
        }

        // Unregister from UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterResourceHUD(this);
        }
    }

    private void OnDisable()
    {
        // Stop listening to events
        if (consumableManager != null)
        {
            consumableManager.OnConsumableChanged -= OnConsumableChanged;
        }

        if (progressionManager != null)
        {
            progressionManager.OnAnimalStarUpdated -= OnStarsUpdated;
        }
    }
}
