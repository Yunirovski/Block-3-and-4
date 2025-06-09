using System.Collections.Generic;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fixed Photo Book Controller: Auto-adapts to page changes and ensures double-click delete works properly
/// Font colours have been updated so ALL text now displays in black.
/// </summary>
public class FixedPhotoBookController : MonoBehaviour
{
    [System.Serializable]
    public class AnimalPage
    {
        [Header("Basic Info")]
        public string animalName;           // Animal name (like "Camel", "Donkey", etc.)
        public int pageIndex;               // Page index (starts from 0)

        [Header("UI Components")]
        public List<Image> photoSlots;      // Photo slots (max 5)
        public GameObject photoLayer;       // Photo layer GameObject
        public TMP_Text starText;           // Star display text (like "2/3")

        [Header("Data")]
        public List<string> photoFilePaths; // Photo file paths

        [Header("Auto Detection")]
        [Tooltip("If true, will auto-find pages containing this animal name in allPages")]
        public bool autoDetectPage = true;
    }

    // Double-click detection
    private Dictionary<Image, float> lastClickTimes = new Dictionary<Image, float>();
    private Dictionary<Image, AnimalPage> slotToAnimalPage = new Dictionary<Image, AnimalPage>();
    private Dictionary<Image, int> slotToIndex = new Dictionary<Image, int>();
    private float doubleClickTime = 0.8f; // Double-click time window

    [Header("Book Canvas")]
    public Canvas bookCanvas;

    [Header("Page Management")]
    public List<GameObject> allPages;       // All pages (including cover, index, etc.)
    public Button leftButton;
    public Button rightButton;

    [Header("Animal Pages Setup")]
    [Tooltip("Configure photo display for each animal page")]
    public List<AnimalPage> animalPages = new List<AnimalPage>();

    [Header("Photo Display Settings")]
    [Tooltip("Default placeholder sprite")]
    public Sprite placeholderSprite;

    [Tooltip("Error sprite when photo fails to load")]
    public Sprite errorSprite;

    [Header("Photo Slot Prefab")]
    [Tooltip("Photo slot prefab")]
    public GameObject photoSlotPrefab;

    [Header("Star Display Settings")]
    [Tooltip("Star text position offset")]
    public Vector2 starTextOffset = new Vector2(0, -200);

    [Tooltip("Total score display text (like 8/24)")]
    public TMP_Text totalScoreText;

    [Header("Debug Settings")]
    [Tooltip("Enable detailed debug logs")]
    public bool enableDebugLogs = true;

    // Current page index
    private int currentPageIndex = 0;
    private bool isBookOpen = false;
    private float savedTimeScale;
    public List<Behaviour> componentsToDisable;
    private Dictionary<string, List<Sprite>> loadedPhotos = new Dictionary<string, List<Sprite>>();

    [Header("Photo Refresh Settings")]
    public bool refreshOnOpen = true;
    public bool enableAutoRefresh = true;
    public float autoRefreshInterval = 2f;
    private float lastRefreshTime = 0f;

    void Start()
    {
        LogDebug("=== Photo Book Starting ===");

        if (bookCanvas != null)
            bookCanvas.gameObject.SetActive(false);

        if (leftButton != null)
            leftButton.onClick.AddListener(PreviousPage);

        if (rightButton != null)
            rightButton.onClick.AddListener(NextPage);

        // Key fix: Auto-detect and fix page indices
        AutoDetectAnimalPages();

        InitializeAnimalPages();
        StartCoroutine(PreloadAllPhotos());

        LogDebug("=== Photo Book Ready ===");
    }

    /// <summary>
    /// Auto-detect correct page indices for animal pages
    /// </summary>
    void AutoDetectAnimalPages()
    {
        LogDebug("Starting auto page detection...");

        foreach (var animalPage in animalPages)
        {
            if (!animalPage.autoDetectPage) continue;

            // Look for pages containing animal name
            for (int i = 0; i < allPages.Count; i++)
            {
                if (allPages[i] == null) continue;

                string pageName = allPages[i].name.ToLower();
                string animalNameLower = animalPage.animalName.ToLower();

                // Check if page name contains animal name
                if (pageName.Contains(animalNameLower))
                {
                    int oldIndex = animalPage.pageIndex;
                    animalPage.pageIndex = i;
                    LogDebug($"Auto-detect: {animalPage.animalName} page index updated from {oldIndex} to {i} (page: {allPages[i].name})");
                    break;
                }
            }
        }

        // Check detection results
        ValidatePageIndices();
    }

    /// <summary>
    /// Check if page index setup is correct
    /// </summary>
    void ValidatePageIndices()
    {
        LogDebug("Checking page index setup...");

        bool hasErrors = false;

        foreach (var animalPage in animalPages)
        {
            if (animalPage.pageIndex < 0 || animalPage.pageIndex >= allPages.Count)
            {
                Debug.LogError($"ERROR: {animalPage.animalName} page index {animalPage.pageIndex} is out of range (0-{allPages.Count - 1})");
                hasErrors = true;
            }
            else if (allPages[animalPage.pageIndex] == null)
            {
                Debug.LogError($"ERROR: {animalPage.animalName} page index {animalPage.pageIndex} points to null page");
                hasErrors = true;
            }
            else
            {
                LogDebug($"OK: {animalPage.animalName} -> page {animalPage.pageIndex} ({allPages[animalPage.pageIndex].name})");
            }
        }

        if (!hasErrors)
        {
            LogDebug("All page indices are correct");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isBookOpen)
                CloseBook();
            else
                OpenBook();
        }

        if (isBookOpen)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                PreviousPage();

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                NextPage();

            if (Input.GetKeyDown(KeyCode.R))
                RefreshCurrentPagePhotos();

            if (enableAutoRefresh && Time.unscaledTime - lastRefreshTime > autoRefreshInterval)
            {
                lastRefreshTime = Time.unscaledTime;
                RefreshCurrentPagePhotos();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                CloseBook();
        }
    }

    void InitializeAnimalPages()
    {
        LogDebug("Setting up animal pages...");

        foreach (var animalPage in animalPages)
        {
            LogDebug($"Setting up {animalPage.animalName} page (index: {animalPage.pageIndex})");

            if (animalPage.photoFilePaths == null)
                animalPage.photoFilePaths = new List<string>();

            if (animalPage.photoLayer == null && animalPage.pageIndex < allPages.Count && allPages[animalPage.pageIndex] != null)
            {
                GameObject pageObj = allPages[animalPage.pageIndex];

                // Look for existing photo layer first
                Transform existingLayer = pageObj.transform.Find($"{animalPage.animalName}_PhotoLayer");
                if (existingLayer != null)
                {
                    animalPage.photoLayer = existingLayer.gameObject;
                    LogDebug($"Found existing photo layer: {animalPage.animalName}_PhotoLayer");
                }
                else
                {
                    // Create new photo layer
                    GameObject photoLayer = new GameObject($"{animalPage.animalName}_PhotoLayer");
                    photoLayer.transform.SetParent(pageObj.transform, false);

                    RectTransform rectTransform = photoLayer.AddComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;

                    animalPage.photoLayer = photoLayer;
                    LogDebug($"Created new photo layer: {animalPage.animalName}_PhotoLayer");
                }
            }

            if (animalPage.photoSlots.Count == 0 && animalPage.photoLayer != null)
            {
                CreatePhotoSlots(animalPage);
            }

            CreateStarText(animalPage);
        }
    }

    void CreateStarText(AnimalPage animalPage)
    {
        if (animalPage.starText != null) return;

        if (animalPage.pageIndex >= allPages.Count || allPages[animalPage.pageIndex] == null)
        {
            LogDebug($"Cannot create star text for {animalPage.animalName}: invalid page index");
            return;
        }

        GameObject pageObj = allPages[animalPage.pageIndex];

        // Look for existing star text first
        Transform existingText = pageObj.transform.Find($"{animalPage.animalName}_StarText");
        if (existingText != null && existingText.GetComponent<TMP_Text>() != null)
        {
            animalPage.starText = existingText.GetComponent<TMP_Text>();
            LogDebug($"Found existing star text: {animalPage.animalName}_StarText");
            // Make sure the colour is black even if it existed before
            animalPage.starText.color = Color.black;
            return;
        }

        // Create new star text
        GameObject textObj = new GameObject($"{animalPage.animalName}_StarText");
        textObj.transform.SetParent(pageObj.transform, false);

        TMP_Text starText = textObj.AddComponent<TMP_Text>();
        starText.text = "0/3";
        starText.fontSize = 36;
        starText.color = Color.black; // *** Now BLACK ***
        starText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(200, 50);
        textRect.anchoredPosition = starTextOffset;

        animalPage.starText = starText;
        LogDebug($"Created new star text: {animalPage.animalName}_StarText (black colour)");
    }

    void CreatePhotoSlots(AnimalPage animalPage)
    {
        LogDebug($"Creating photo slots for {animalPage.animalName}...");

        Vector2[] slotPositions = GetPhotoPositionsForAnimal(animalPage.animalName);

        for (int i = 0; i < Mathf.Min(slotPositions.Length, 5); i++)
        {
            GameObject slot;

            if (photoSlotPrefab != null)
            {
                slot = Instantiate(photoSlotPrefab, animalPage.photoLayer.transform);
            }
            else
            {
                slot = new GameObject($"PhotoSlot_{i}");
                slot.transform.SetParent(animalPage.photoLayer.transform, false);

                Image img = slot.AddComponent<Image>();
                img.sprite = placeholderSprite;
                img.preserveAspect = true;

                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 150);
            }

            RectTransform rectTransform = slot.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = slotPositions[i];

            Image imageComponent = slot.GetComponent<Image>();
            if (imageComponent != null)
            {
                animalPage.photoSlots.Add(imageComponent);
                AddDoubleClickHandler(imageComponent, animalPage, i);
                LogDebug($"Created photo slot {i} for {animalPage.animalName}");
            }
        }

        LogDebug($"Created {animalPage.photoSlots.Count} photo slots for {animalPage.animalName}");
    }

    void AddDoubleClickHandler(Image photoSlot, AnimalPage animalPage, int slotIndex)
    {
        // Save mapping info
        slotToAnimalPage[photoSlot] = animalPage;
        slotToIndex[photoSlot] = slotIndex;

        Button button = photoSlot.gameObject.GetComponent<Button>();
        if (button == null)
        {
            button = photoSlot.gameObject.AddComponent<Button>();
        }

        // Remove old listeners to avoid duplicates
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnPhotoSlotClicked(photoSlot));

        lastClickTimes[photoSlot] = 0f;

        LogDebug($"Added double-click handler for {animalPage.animalName} slot {slotIndex}");
    }

    void OnPhotoSlotClicked(Image photoSlot)
    {
        if (!slotToAnimalPage.ContainsKey(photoSlot) || !slotToIndex.ContainsKey(photoSlot))
        {
            LogDebug("ERROR: Photo slot mapping info lost!");
            return;
        }

        AnimalPage animalPage = slotToAnimalPage[photoSlot];
        int slotIndex = slotToIndex[photoSlot];

        float currentTime = Time.realtimeSinceStartup;
        LogDebug($"Clicked {animalPage.animalName} photo {slotIndex + 1}");

        if (lastClickTimes.ContainsKey(photoSlot))
        {
            float timeSinceLastClick = currentTime - lastClickTimes[photoSlot];
            LogDebug($"Time since last click: {timeSinceLastClick:F2}s");

            if (timeSinceLastClick <= doubleClickTime)
            {
                LogDebug($"DOUBLE-CLICK detected! Deleting {animalPage.animalName} photo {slotIndex + 1}");
                DeletePhoto(animalPage, slotIndex);
                lastClickTimes[photoSlot] = 0f;
            }
            else
            {
                lastClickTimes[photoSlot] = currentTime;
                LogDebug("Single click, waiting for possible second click");
            }
        }
        else
        {
            lastClickTimes[photoSlot] = currentTime;
            LogDebug("First click on this photo");
        }
    }

    void DeletePhoto(AnimalPage animalPage, int slotIndex)
    {
        LogDebug($"Trying to delete {animalPage.animalName} photo {slotIndex + 1}");

        if (slotIndex < 0 || slotIndex >= animalPage.photoFilePaths.Count)
        {
            LogDebug($"Delete failed: index {slotIndex} invalid, have {animalPage.photoFilePaths.Count} photos");
            return;
        }

        if (!loadedPhotos.ContainsKey(animalPage.animalName) || slotIndex >= loadedPhotos[animalPage.animalName].Count)
        {
            LogDebug($"Delete failed: no actual photo at position {slotIndex}");
            return;
        }

        string filePath = animalPage.photoFilePaths[slotIndex];

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                LogDebug($"Successfully deleted photo file: {filePath}");
            }
            else
            {
                LogDebug($"File does not exist: {filePath}");
            }

            animalPage.photoFilePaths.RemoveAt(slotIndex);

            // Reload photos for this animal
            StartCoroutine(LoadPhotosForAnimal(animalPage.animalName));

            LogDebug($"Successfully deleted {animalPage.animalName} photo {slotIndex + 1}, reloading photo list");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error deleting photo: {e.Message}");
        }
    }

    // Other methods remain the same...
    Vector2[] GetPhotoPositionsForAnimal(string animalName)
    {
        switch (animalName)
        {
            case "Camel":
                return new Vector2[] {
                    new Vector2(-150, 100), new Vector2(150, 100),
                    new Vector2(-150, -50), new Vector2(150, -50),
                    new Vector2(0, -150)
                };
            case "Donkey":
                return new Vector2[] {
                    new Vector2(-200, 80), new Vector2(0, 80), new Vector2(200, 80),
                    new Vector2(-100, -100), new Vector2(100, -100)
                };
            default:
                return new Vector2[] {
                    new Vector2(-150, 100), new Vector2(150, 100),
                    new Vector2(-150, -50), new Vector2(150, -50),
                    new Vector2(0, -150)
                };
        }
    }

    void UpdateAnimalStarDisplay(AnimalPage animalPage)
    {
        if (ProgressionManager.Instance == null || animalPage.starText == null)
            return;

        int currentStars = ProgressionManager.Instance.GetAnimalStars(animalPage.animalName);
        int maxStars = ProgressionManager.Instance.maxStarsPerAnimal;

        animalPage.starText.text = $"{currentStars}/{maxStars}";

        if (currentStars == 0)
            animalPage.starText.color = Color.black;
        else if (currentStars >= maxStars)
            animalPage.starText.color = Color.yellow;
        else
            animalPage.starText.color = Color.green;
    }

    void UpdateTotalScoreDisplay()
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = GetTotalScore();
        }
    }

    public string GetTotalScore()
    {
        if (ProgressionManager.Instance == null)
            return "0/0";

        int currentTotal = ProgressionManager.Instance.TotalStars;
        int maxPossible = animalPages.Count * ProgressionManager.Instance.maxStarsPerAnimal;

        return $"{currentTotal}/{maxPossible}";
    }

    IEnumerator PreloadAllPhotos()
    {
        LogDebug("Starting to load all animal photos...");
        foreach (var animalPage in animalPages)
        {
            yield return StartCoroutine(LoadPhotosForAnimal(animalPage.animalName));
        }
        LogDebug("All animal photos loaded");
    }

    IEnumerator LoadPhotosForAnimal(string animalName)
    {
        List<Sprite> photos = new List<Sprite>();
        List<string> filePaths = new List<string>();
        string folderPath = Path.Combine(Application.persistentDataPath, animalName);

        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, "*.png");
            System.Array.Sort(files, (x, y) => File.GetLastWriteTime(y).CompareTo(File.GetLastWriteTime(x)));

            for (int i = 0; i < Mathf.Min(files.Length, 5); i++)
            {
                bool photoLoaded = false;
                yield return StartCoroutine(LoadPhotoFromFile(files[i], (sprite) => {
                    if (sprite != null)
                    {
                        photos.Add(sprite);
                        filePaths.Add(files[i]);
                        photoLoaded = true;
                    }
                }));

                if (!photoLoaded)
                {
                    LogDebug($"Could not load photo: {files[i]}");
                }
            }
        }

        loadedPhotos[animalName] = photos;

        AnimalPage animalPage = animalPages.Find(p => p.animalName == animalName);
        if (animalPage != null)
        {
            animalPage.photoFilePaths = filePaths;
        }

        LogDebug($"Loaded {photos.Count} photos for {animalName}");

        AnimalPage currentPage = animalPages.Find(p => p.pageIndex == currentPageIndex);
        if (currentPage != null && currentPage.animalName == animalName)
        {
            UpdatePhotosForCurrentPage();
        }
    }

    IEnumerator LoadPhotoFromFile(string filePath, System.Action<Sprite> callback)
    {
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(fileData))
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                callback(sprite);
            }
            else
            {
                callback(null);
            }
        }
        else
        {
            callback(null);
        }
        yield return null;
    }

    void OpenBook()
    {
        if (bookCanvas == null) return;

        LogDebug("Opening photo book");
        isBookOpen = true;
        bookCanvas.gameObject.SetActive(true);

        savedTimeScale = Time.timeScale;
        Time.timeScale = 0;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (var component in componentsToDisable)
        {
            if (component != null)
                component.enabled = false;
        }

        if (refreshOnOpen)
        {
            LogDebug("Refreshing all photos...");
            StartCoroutine(PreloadAllPhotos());
        }

        currentPageIndex = 0;
        ShowPage();
        UpdateTotalScoreDisplay();
    }

    void CloseBook()
    {
        if (bookCanvas == null) return;

        LogDebug("Closing photo book");
        isBookOpen = false;
        bookCanvas.gameObject.SetActive(false);

        Time.timeScale = savedTimeScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (var component in componentsToDisable)
        {
            if (component != null)
                component.enabled = true;
        }
    }

    void ShowPage()
    {
        for (int i = 0; i < allPages.Count; i++)
        {
            if (allPages[i] != null)
                allPages[i].SetActive(i == currentPageIndex);
        }

        UpdatePhotosForCurrentPage();
        UpdateButtons();
        UpdateTotalScoreDisplay();
    }

    void UpdatePhotosForCurrentPage()
    {
        AnimalPage currentAnimalPage = animalPages.Find(p => p.pageIndex == currentPageIndex);

        if (currentAnimalPage != null)
        {
            LogDebug($"Updating page {currentPageIndex} photos: {currentAnimalPage.animalName}");

            UpdateAnimalStarDisplay(currentAnimalPage);

            if (loadedPhotos.ContainsKey(currentAnimalPage.animalName))
            {
                List<Sprite> photos = loadedPhotos[currentAnimalPage.animalName];

                for (int i = 0; i < currentAnimalPage.photoSlots.Count; i++)
                {
                    if (i < photos.Count && photos[i] != null)
                    {
                        currentAnimalPage.photoSlots[i].sprite = photos[i];
                        currentAnimalPage.photoSlots[i].gameObject.SetActive(true);

                        Button button = currentAnimalPage.photoSlots[i].GetComponent<Button>();
                        if (button != null)
                        {
                            button.interactable = true;
                        }
                        else
                        {
                            // Re-add double-click handler
                            AddDoubleClickHandler(currentAnimalPage.photoSlots[i], currentAnimalPage, i);
                        }
                    }
                    else
                    {
                        if (placeholderSprite != null)
                        {
                            currentAnimalPage.photoSlots[i].sprite = placeholderSprite;
                            currentAnimalPage.photoSlots[i].gameObject.SetActive(true);

                            Button button = currentAnimalPage.photoSlots[i].GetComponent<Button>();
                            if (button != null)
                            {
                                button.interactable = false;
                            }
                        }
                        else
                        {
                            currentAnimalPage.photoSlots[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage();
            LogDebug($"Went to previous page: {currentPageIndex}");
        }
    }

    public void NextPage()
    {
        if (currentPageIndex < allPages.Count - 1)
        {
            currentPageIndex++;
            ShowPage();
            LogDebug($"Went to next page: {currentPageIndex}");
        }
    }

    void UpdateButtons()
    {
        if (leftButton != null)
            leftButton.interactable = (currentPageIndex > 0);

        if (rightButton != null)
            rightButton.interactable = (currentPageIndex < allPages.Count - 1);
    }

    public void RefreshAnimalPhotos(string animalName)
    {
        StartCoroutine(LoadPhotosForAnimal(animalName));

        AnimalPage currentAnimalPage = animalPages.Find(p => p.pageIndex == currentPageIndex);
        if (currentAnimalPage != null && currentAnimalPage.animalName == animalName)
        {
            UpdatePhotosForCurrentPage();
        }
    }

    public void RefreshCurrentPagePhotos()
    {
        AnimalPage currentAnimalPage = animalPages.Find(p => p.pageIndex == currentPageIndex);
        if (currentAnimalPage != null)
        {
            LogDebug($"Refreshing {currentAnimalPage.animalName} photos and stars...");
            StartCoroutine(LoadPhotosForAnimal(currentAnimalPage.animalName));
            UpdateAnimalStarDisplay(currentAnimalPage);
            UpdateTotalScoreDisplay();
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PhotoBook] {message}");
        }
    }
}