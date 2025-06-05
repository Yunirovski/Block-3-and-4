// Assets/Scripts/UI/ProgressCanvasController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 增强的进度Canvas控制器
/// 按住Tab键显示，松开隐藏
/// 显示星星进度和物品解锁状态
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
    [Tooltip("Text showing total stars")]
    public TMP_Text starsText;

    [Tooltip("4 progress text items")]
    public TMP_Text[] progressTexts = new TMP_Text[4];

    [Header("Colors")]
    [Tooltip("Locked color")]
    public Color lockedColor = Color.gray;
    [Tooltip("Almost unlock color")]
    public Color almostColor = Color.yellow;
    [Tooltip("Unlocked color")]
    public Color unlockedColor = Color.green;

    [Header("Tab Display Settings")]
    [Tooltip("Hold Tab key to show")]
    public bool enableTabDisplay = true;
    [Tooltip("Fade in/out time")]
    public float fadeTime = 0.2f;
    [Tooltip("Background transparency")]
    [Range(0f, 1f)]
    public float backgroundAlpha = 0.8f;

    // Current stars
    private int currentStars = 0;

    // Stars needed for unlocks
    private readonly int[] requiredStars = { 6, 12, 18, 24 };

    // Tab display related
    private bool isShowingByTab = false;
    private bool isShowingByP = false;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    void Start()
    {
        Debug.Log("ProgressCanvasController Start!");

        // Initialize Canvas components
        InitializeCanvas();

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

    void InitializeCanvas()
    {
        if (progressCanvas == null)
        {
            Debug.LogError("Progress Canvas is null! Please set it in Inspector!");
            return;
        }

        // Get or add CanvasGroup component
        canvasGroup = progressCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = progressCanvas.gameObject.AddComponent<CanvasGroup>();
            Debug.Log("Added CanvasGroup component");
        }

        // Initial state: hidden
        progressCanvas.gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Debug.Log("Canvas initialization complete");
    }

    void SetupBackground()
    {
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Simple;

            // Set background transparency
            Color bgColor = backgroundImage.color;
            bgColor.a = backgroundAlpha;
            backgroundImage.color = bgColor;

            Debug.Log("Background setup complete");
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Tab key show/hide
        if (enableTabDisplay)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ShowCanvasByTab();
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                HideCanvasByTab();
            }
        }

        // P key toggle show/hide
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("P key pressed!");
            ToggleCanvasByP();
        }

        // R key add stars (for testing)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed!");
            AddStar();
        }

        // T key reset (for testing)
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("T key pressed!");
            ResetProgress();
        }
    }

    void ShowCanvasByTab()
    {
        if (isShowingByTab) return;

        Debug.Log("Tab key pressed - Show progress canvas");
        isShowingByTab = true;

        if (!IsCanvasVisible())
        {
            ShowCanvas();
        }
    }

    void HideCanvasByTab()
    {
        if (!isShowingByTab) return;

        Debug.Log("Tab key released - Hide progress canvas");
        isShowingByTab = false;

        // Only hide if not shown by P key
        if (!isShowingByP)
        {
            HideCanvas();
        }
    }

    public void ToggleCanvasByP()
    {
        Debug.Log("ToggleCanvasByP called!");

        if (progressCanvas != null)
        {
            bool isActive = IsCanvasVisible();
            Debug.Log($"Canvas current state: {isActive}, will set to: {!isActive}");

            if (isActive)
            {
                isShowingByP = false;
                // Only hide if not shown by Tab key
                if (!isShowingByTab)
                {
                    HideCanvas();
                }
            }
            else
            {
                isShowingByP = true;
                ShowCanvas();
            }
        }
        else
        {
            Debug.LogError("Progress Canvas is null! Please set Progress Canvas in Inspector!");
        }
    }

    void ShowCanvas()
    {
        if (progressCanvas == null) return;

        UpdateDisplay(); // Update data before showing

        progressCanvas.gameObject.SetActive(true);

        // Stop previous fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Start fade in
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, 1f));

        Debug.Log("Canvas fade in show");
    }

    void HideCanvas()
    {
        if (progressCanvas == null) return;

        // Stop previous fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Start fade out
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, 0f));

        Debug.Log("Canvas fade out hide");
    }

    bool IsCanvasVisible()
    {
        return progressCanvas != null && progressCanvas.gameObject.activeInHierarchy && canvasGroup.alpha > 0.1f;
    }

    System.Collections.IEnumerator FadeCanvasGroup(float fromAlpha, float toAlpha)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = fromAlpha;

        // If fading in, activate GameObject first
        if (toAlpha > 0f)
        {
            progressCanvas.gameObject.SetActive(true);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaledDeltaTime to support pause
            float t = elapsed / fadeTime;

            // Use smooth curve
            t = Mathf.SmoothStep(0f, 1f, t);

            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = toAlpha;

        // If fading out, deactivate GameObject at the end
        if (toAlpha <= 0f)
        {
            progressCanvas.gameObject.SetActive(false);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        fadeCoroutine = null;
    }

    void AddStar()
    {
        currentStars++;

        // Sync to ProgressionManager
        if (ProgressionManager.Instance != null)
        {
            // Generate fake photo star
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
            starsText.text = $"Total Stars: {currentStars}/24";
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

    /// <summary>
    /// External call: Force refresh display
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// Check if showing by Tab key
    /// </summary>
    public bool IsShowingByTab()
    {
        return isShowingByTab;
    }

    /// <summary>
    /// Check if showing by P key
    /// </summary>
    public bool IsShowingByP()
    {
        return isShowingByP;
    }
}