using System.Collections.Generic;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 增强版照片书控制器：支持从文件系统加载动物照片并显示每个动物的星级
/// </summary>
public class EnhancedPhotoBookController : MonoBehaviour
{
    [System.Serializable]
    public class AnimalPage
    {
        public string animalName;           // 动物名称（如 "Camel", "Donkey" 等）
        public int pageIndex;               // 页面索引（从0开始）
        public List<Image> photoSlots;      // 照片槽位（最多5个）
        public GameObject photoLayer;       // 照片层GameObject
        public TMP_Text starText;           // 显示星级的文本组件 (如: "2/3")
    }

    [Header("Book Canvas")]
    public Canvas bookCanvas;

    [Header("Page Management")]
    public List<GameObject> allPages;       // 所有页面（包括封面、目录等）
    public Button leftButton;
    public Button rightButton;

    [Header("Animal Pages Configuration")]
    [Tooltip("配置每个动物页面的照片显示")]
    public List<AnimalPage> animalPages = new List<AnimalPage>
    {
        new AnimalPage { animalName = "Camel", pageIndex = 3 },     // 第4页（索引3）
        new AnimalPage { animalName = "Donkey", pageIndex = 4 },    // 第5页（索引4）
        new AnimalPage { animalName = "Giraffe", pageIndex = 5 },   // 第6页（索引5）
        new AnimalPage { animalName = "Goat", pageIndex = 6 },      // 第7页（索引6）
        new AnimalPage { animalName = "Hippo", pageIndex = 7 },     // 第8页（索引7）
        new AnimalPage { animalName = "Lion", pageIndex = 8 },      // 第9页（索引8）
        new AnimalPage { animalName = "Pigeon", pageIndex = 9 },    // 第10页（索引9）
        new AnimalPage { animalName = "Rhino", pageIndex = 10 }     // 第11页（索引10）
    };

    [Header("Photo Display Settings")]
    [Tooltip("默认照片占位符")]
    public Sprite placeholderSprite;

    [Tooltip("照片加载失败时显示的图片")]
    public Sprite errorSprite;

    [Header("Photo Slot Prefab")]
    [Tooltip("照片槽位预制体")]
    public GameObject photoSlotPrefab;

    [Header("Star Display Settings")]
    [Tooltip("星级文字显示位置偏移")]
    public Vector2 starTextOffset = new Vector2(0, -200);

    [Tooltip("总得分显示文本组件 (如: 8/24)")]
    public TMP_Text totalScoreText;

    // 当前页面索引
    private int currentPageIndex = 0;

    // 书是否打开
    private bool isBookOpen = false;

    // 保存的游戏时间缩放
    private float savedTimeScale;

    // 需要在打开书时禁用的组件
    public List<Behaviour> componentsToDisable;

    // 缓存加载的照片
    private Dictionary<string, List<Sprite>> loadedPhotos = new Dictionary<string, List<Sprite>>();

    [Header("Photo Refresh Settings")]
    [Tooltip("每次打开书时刷新照片")]
    public bool refreshOnOpen = true;
    [Tooltip("启用自动检测新照片")]
    public bool enableAutoRefresh = true;
    [Tooltip("自动检测间隔（秒）")]
    public float autoRefreshInterval = 2f;

    private float lastRefreshTime = 0f;

    void Start()
    {
        // 初始化时关闭书
        if (bookCanvas != null)
            bookCanvas.gameObject.SetActive(false);

        // 设置按钮点击事件
        if (leftButton != null)
            leftButton.onClick.AddListener(PreviousPage);

        if (rightButton != null)
            rightButton.onClick.AddListener(NextPage);

        // 初始化动物页面的照片层
        InitializeAnimalPages();

        // 预加载所有动物的照片
        StartCoroutine(PreloadAllPhotos());
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
            // 翻页控制
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PreviousPage();
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextPage();
            }

            // 手动刷新当前页面的照片
            if (Input.GetKeyDown(KeyCode.R))
            {
                RefreshCurrentPagePhotos();
            }

            // 自动检测新照片
            if (enableAutoRefresh && Time.unscaledTime - lastRefreshTime > autoRefreshInterval)
            {
                lastRefreshTime = Time.unscaledTime;
                RefreshCurrentPagePhotos();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseBook();
            }
        }
    }

    /// <summary>
    /// 初始化动物页面的照片层和星级显示
    /// </summary>
    void InitializeAnimalPages()
    {
        foreach (var animalPage in animalPages)
        {
            // 如果还没有照片层，创建一个
            if (animalPage.photoLayer == null && animalPage.pageIndex < allPages.Count)
            {
                GameObject pageObj = allPages[animalPage.pageIndex];

                // 创建照片层
                GameObject photoLayer = new GameObject($"{animalPage.animalName}_PhotoLayer");
                photoLayer.transform.SetParent(pageObj.transform, false);

                RectTransform rectTransform = photoLayer.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;

                animalPage.photoLayer = photoLayer;
            }

            // 如果还没有照片槽位，创建它们
            if (animalPage.photoSlots.Count == 0 && animalPage.photoLayer != null)
            {
                CreatePhotoSlots(animalPage);
            }

            // 创建星级文字显示
            CreateStarText(animalPage);
        }
    }

    /// <summary>
    /// 为动物页面创建星级文字显示
    /// </summary>
    void CreateStarText(AnimalPage animalPage)
    {
        if (animalPage.starText != null) return;

        GameObject pageObj = allPages[animalPage.pageIndex];

        // 创建星级文字
        GameObject textObj = new GameObject($"{animalPage.animalName}_StarText");
        textObj.transform.SetParent(pageObj.transform, false);

        TMP_Text starText = textObj.AddComponent<TMP_Text>();
        starText.text = "0/3";
        starText.fontSize = 36;
        starText.color = Color.white;
        starText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(200, 50);
        textRect.anchoredPosition = starTextOffset;

        animalPage.starText = starText;
    }

    /// <summary>
    /// 为动物页面创建照片槽位
    /// </summary>
    void CreatePhotoSlots(AnimalPage animalPage)
    {
        // 定义每个动物页面的照片位置（你可以根据实际需要调整）
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
                // 如果没有预制体，创建基本的照片槽位
                slot = new GameObject($"PhotoSlot_{i}");
                slot.transform.SetParent(animalPage.photoLayer.transform, false);

                Image img = slot.AddComponent<Image>();
                img.sprite = placeholderSprite;
                img.preserveAspect = true;

                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 150); // 默认照片大小
            }

            // 设置位置
            RectTransform rectTransform = slot.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = slotPositions[i];

            // 添加到槽位列表
            Image imageComponent = slot.GetComponent<Image>();
            if (imageComponent != null)
            {
                animalPage.photoSlots.Add(imageComponent);
            }
        }
    }

    /// <summary>
    /// 获取特定动物的照片位置
    /// </summary>
    Vector2[] GetPhotoPositionsForAnimal(string animalName)
    {
        // 这里你可以为每个动物定制不同的照片布局
        // 现在使用通用布局
        switch (animalName)
        {
            case "Camel":
                return new Vector2[] {
                    new Vector2(-150, 100),
                    new Vector2(150, 100),
                    new Vector2(-150, -50),
                    new Vector2(150, -50),
                    new Vector2(0, -150)
                };

            case "Donkey":
                return new Vector2[] {
                    new Vector2(-200, 80),
                    new Vector2(0, 80),
                    new Vector2(200, 80),
                    new Vector2(-100, -100),
                    new Vector2(100, -100)
                };

            // 为其他动物添加自定义布局...
            default:
                // 默认布局：3行布局
                return new Vector2[] {
                    new Vector2(-150, 100),
                    new Vector2(150, 100),
                    new Vector2(-150, -50),
                    new Vector2(150, -50),
                    new Vector2(0, -150)
                };
        }
    }

    /// <summary>
    /// 更新动物的星级显示
    /// </summary>
    void UpdateAnimalStarDisplay(AnimalPage animalPage)
    {
        if (ProgressionManager.Instance == null || animalPage.starText == null)
            return;

        // 获取动物的当前星级和最大星级
        int currentStars = ProgressionManager.Instance.GetAnimalStars(animalPage.animalName);
        int maxStars = ProgressionManager.Instance.maxStarsPerAnimal;

        // 更新文本显示为 "当前/最大" 格式
        animalPage.starText.text = $"{currentStars}/{maxStars}";

        // 根据完成度调整颜色
        if (currentStars == 0)
        {
            animalPage.starText.color = Color.gray;          // 未发现 - 灰色
        }
        else if (currentStars >= maxStars)
        {
            animalPage.starText.color = Color.yellow;        // 完美 - 黄色
        }
        else
        {
            animalPage.starText.color = Color.white;         // 进行中 - 白色
        }
    }

    /// <summary>
    /// 更新总得分显示
    /// </summary>
    void UpdateTotalScoreDisplay()
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = GetTotalScore();
        }
    }

    /// <summary>
    /// 计算总得分 (当前总星级/最大可能总星级)
    /// </summary>
    /// <returns>格式为 "当前总星级/最大可能总星级" 的字符串，如 "8/24"</returns>
    public string GetTotalScore()
    {
        if (ProgressionManager.Instance == null)
            return "0/0";

        int currentTotal = ProgressionManager.Instance.TotalStars;
        int maxPossible = animalPages.Count * ProgressionManager.Instance.maxStarsPerAnimal;

        return $"{currentTotal}/{maxPossible}";
    }

    /// <summary>
    /// 预加载所有动物的照片
    /// </summary>
    IEnumerator PreloadAllPhotos()
    {
        foreach (var animalPage in animalPages)
        {
            yield return StartCoroutine(LoadPhotosForAnimal(animalPage.animalName));
        }
    }

    /// <summary>
    /// 加载特定动物的照片
    /// </summary>
    IEnumerator LoadPhotosForAnimal(string animalName)
    {
        List<Sprite> photos = new List<Sprite>();
        string folderPath = Path.Combine(Application.persistentDataPath, animalName);

        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, "*.png");

            // 只加载前5张照片
            for (int i = 0; i < Mathf.Min(files.Length, 5); i++)
            {
                yield return StartCoroutine(LoadPhotoFromFile(files[i], (sprite) => {
                    if (sprite != null)
                        photos.Add(sprite);
                }));
            }
        }

        loadedPhotos[animalName] = photos;
        Debug.Log($"加载了 {photos.Count} 张 {animalName} 的照片");
    }

    /// <summary>
    /// 从文件加载单张照片
    /// </summary>
    IEnumerator LoadPhotoFromFile(string filePath, System.Action<Sprite> callback)
    {
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(fileData))
            {
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                callback(sprite);
            }
            else
            {
                Debug.LogError($"无法加载图片: {filePath}");
                callback(null);
            }
        }
        else
        {
            Debug.LogError($"文件不存在: {filePath}");
            callback(null);
        }

        yield return null;
    }

    /// <summary>
    /// 打开书
    /// </summary>
    void OpenBook()
    {
        if (bookCanvas == null) return;

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

        // 如果启用了打开时刷新
        if (refreshOnOpen)
        {
            Debug.Log("打开书本，刷新所有照片...");
            StartCoroutine(PreloadAllPhotos());
        }

        currentPageIndex = 0;
        ShowPage();

        // 更新总得分显示
        UpdateTotalScoreDisplay();
    }

    /// <summary>
    /// 关闭书
    /// </summary>
    void CloseBook()
    {
        if (bookCanvas == null) return;

        isBookOpen = false;
        bookCanvas.gameObject.SetActive(false);

        // 恢复游戏速度
        Time.timeScale = savedTimeScale;

        // 隐藏鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 重新启用组件
        foreach (var component in componentsToDisable)
        {
            if (component != null)
                component.enabled = true;
        }
    }

    /// <summary>
    /// 显示当前页面
    /// </summary>
    void ShowPage()
    {
        // 隐藏所有页面
        for (int i = 0; i < allPages.Count; i++)
        {
            if (allPages[i] != null)
                allPages[i].SetActive(i == currentPageIndex);
        }

        // 如果是动物页面，更新照片和星级
        UpdatePhotosForCurrentPage();

        // 更新按钮状态
        UpdateButtons();

        // 更新总得分显示
        UpdateTotalScoreDisplay();
    }

    /// <summary>
    /// 更新当前页面的照片和星级显示
    /// </summary>
    void UpdatePhotosForCurrentPage()
    {
        // 查找当前页面是否是动物页面
        AnimalPage currentAnimalPage = animalPages.Find(p => p.pageIndex == currentPageIndex);

        if (currentAnimalPage != null)
        {
            // 更新星级显示
            UpdateAnimalStarDisplay(currentAnimalPage);

            // 更新照片显示
            if (loadedPhotos.ContainsKey(currentAnimalPage.animalName))
            {
                List<Sprite> photos = loadedPhotos[currentAnimalPage.animalName];

                // 更新照片槽位
                for (int i = 0; i < currentAnimalPage.photoSlots.Count; i++)
                {
                    if (i < photos.Count)
                    {
                        currentAnimalPage.photoSlots[i].sprite = photos[i];
                        currentAnimalPage.photoSlots[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        // 如果没有照片，隐藏槽位或显示占位符
                        currentAnimalPage.photoSlots[i].sprite = placeholderSprite;
                        currentAnimalPage.photoSlots[i].gameObject.SetActive(placeholderSprite != null);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 显示上一页
    /// </summary>
    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage();
        }
    }

    /// <summary>
    /// 显示下一页
    /// </summary>
    public void NextPage()
    {
        if (currentPageIndex < allPages.Count - 1)
        {
            currentPageIndex++;
            ShowPage();
        }
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    void UpdateButtons()
    {
        if (leftButton != null)
            leftButton.interactable = (currentPageIndex > 0);

        if (rightButton != null)
            rightButton.interactable = (currentPageIndex < allPages.Count - 1);
    }

    /// <summary>
    /// 刷新特定动物的照片（当有新照片时调用）
    /// </summary>
    public void RefreshAnimalPhotos(string animalName)
    {
        StartCoroutine(LoadPhotosForAnimal(animalName));

        // 如果当前显示的是这个动物的页面，立即更新
        AnimalPage currentAnimalPage = animalPages.Find(p => p.pageIndex == currentPageIndex);
        if (currentAnimalPage != null && currentAnimalPage.animalName == animalName)
        {
            UpdatePhotosForCurrentPage();
        }
    }

    /// <summary>
    /// 刷新当前页面的照片和星级
    /// </summary>
    public void RefreshCurrentPagePhotos()
    {
        AnimalPage currentAnimalPage = animalPages.Find(p => p.pageIndex == currentPageIndex);
        if (currentAnimalPage != null)
        {
            Debug.Log($"刷新 {currentAnimalPage.animalName} 的照片和星级...");
            StartCoroutine(LoadPhotosForAnimal(currentAnimalPage.animalName));

            // 立即更新星级显示
            UpdateAnimalStarDisplay(currentAnimalPage);

            // 更新总得分显示
            UpdateTotalScoreDisplay();
        }
    }
}