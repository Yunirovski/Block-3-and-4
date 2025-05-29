// Assets/Scripts/UI/ProgressCanvasController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple progress Canvas controller
/// Show 4 items progress: 6/24, 12/24, 18/24, 24/24
/// Press R to add stars, green when unlocked, yellow when almost unlocked
/// </summary>
public class ProgressCanvasController : MonoBehaviour
{
    [Header("Canvas Reference")]
    [Tooltip("Progress Canvas")]
    public Canvas progressCanvas;

    [Header("Background")]
    [Tooltip("Background Image")]
    public Image backgroundImage;
    [Tooltip("Background Sprite")]
    public Sprite backgroundSprite;

    [Header("Progress Text")]
    [Tooltip("Text to show total stars")]
    public TMP_Text starsText;

    [Tooltip("4 progress Text items")]
    public TMP_Text[] progressTexts = new TMP_Text[4];

    [Header("Colors")]
    [Tooltip("Locked color")]
    public Color lockedColor = Color.gray;
    [Tooltip("Almost unlock color")]
    public Color almostColor = Color.yellow;
    [Tooltip("Unlocked color")]
    public Color unlockedColor = Color.green;

    // Current stars
    private int currentStars = 0;

    // Stars needed and item names
    private readonly int[] requiredStars = { 6, 12, 18, 24 };
    private readonly string[] itemNames = { "Grapple", "Skateboard", "Dart Gun", "Magic Wand" };

    void Start()
    {
        Debug.Log("ProgressCanvasController Start!");

        // Hide canvas at start
        if (progressCanvas != null)
        {
            progressCanvas.gameObject.SetActive(false);
            Debug.Log("Canvas hidden at start");
        }
        else
        {
            Debug.LogError("Progress Canvas is null! Please set it in Inspector!");
        }

        // Setup background
        SetupBackground();

        // Get stars from ProgressionManager
        if (ProgressionManager.Instance != null)
        {
            currentStars = ProgressionManager.Instance.TotalStars;
            ProgressionManager.Instance.OnAnimalStarUpdated += OnStarsUpdated;
            Debug.Log("Connected to ProgressionManager, current stars: " + currentStars);
        }
        else
        {
            Debug.Log("No ProgressionManager found, using local stars");
        }

        UpdateDisplay();
    }

    void SetupBackground()
    {
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Simple;
            Debug.Log("Background set up");
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // P key to show/hide Canvas
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("P key pressed!");
            ToggleCanvas();
        }

        // R key to add stars
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed!");
            AddStar();
        }

        // T key to reset
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("T key pressed!");
            ResetProgress();
        }
    }

    public void ToggleCanvas()
    {
        Debug.Log("ToggleCanvas called!");

        if (progressCanvas != null)
        {
            bool isActive = progressCanvas.gameObject.activeInHierarchy;
            Debug.Log("Canvas current state: " + isActive + ", will set to: " + !isActive);

            progressCanvas.gameObject.SetActive(!isActive);

            if (!isActive)
            {
                UpdateDisplay();
            }
        }
        else
        {
            Debug.LogError("Progress Canvas is null! Please set Progress Canvas in Inspector!");
        }
    }

    void AddStar()
    {
        currentStars++;

        // Sync to ProgressionManager
        if (ProgressionManager.Instance != null)
        {
            // Make fake photo star
            string testAnimal = "TestAnimal_" + Time.time;
            ProgressionManager.Instance.RegisterStars(testAnimal, 1, false);
        }
        else
        {
            UpdateDisplay();
        }

        Debug.Log("Current Stars: " + currentStars);
    }

    void ResetProgress()
    {
        currentStars = 0;

        // Sync to ProgressionManager
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.ResetProgress();
        }
        else
        {
            UpdateDisplay();
        }

        Debug.Log("Progress Reset");
    }

    void OnStarsUpdated(string animalKey, int stars)
    {
        if (ProgressionManager.Instance != null)
        {
            currentStars = ProgressionManager.Instance.TotalStars;
            UpdateDisplay();
        }
    }

    void UpdateDisplay()
    {
        // Update stars text
        if (starsText != null)
        {
            starsText.text = "Total Stars: " + currentStars + "/24";
        }

        // Update 4 progress texts
        for (int i = 0; i < 4 && i < progressTexts.Length; i++)
        {
            if (progressTexts[i] != null)
            {
                // Set text content - only numbers
                string progressText = currentStars + "/" + requiredStars[i];
                progressTexts[i].text = progressText;

                // Set color
                Color targetColor = GetItemColor(i);
                progressTexts[i].color = targetColor;
            }
        }
    }

    Color GetItemColor(int itemIndex)
    {
        int required = requiredStars[itemIndex];

        if (currentStars >= required)
        {
            // Unlocked - green
            return unlockedColor;
        }
        else if (currentStars >= required - 2)
        {
            // Almost unlock - yellow
            return almostColor;
        }
        else
        {
            // Locked - gray
            return lockedColor;
        }
    }

    void OnDestroy()
    {
        // Stop listening
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnAnimalStarUpdated -= OnStarsUpdated;
        }
    }

    // Public methods
    public void SetStars(int stars)
    {
        currentStars = stars;
        UpdateDisplay();
    }

    public int GetCurrentStars()
    {
        return currentStars;
    }

    // Set background sprite
    public void SetBackgroundSprite(Sprite sprite)
    {
        backgroundSprite = sprite;
        SetupBackground();
    }
}