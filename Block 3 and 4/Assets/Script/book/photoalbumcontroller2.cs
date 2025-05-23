using System.Collections.Generic;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 增强版照片书控制器：支持从文件系统加载动物照片并显示
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
        // 按J键打开/关闭书
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isBookOpen)
                CloseBook();
            else
                OpenBook();
        }

        // 按ESC关闭书
        if (isBookOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseBook();
        }

        // 在书打开时处理翻页
        if (isBookOpen)
        {
            // 按A键或左箭头查看上一页
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PreviousPage();
            }

            // 按D键或右箭头查看下一页
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextPage();
            }
        }
    }

    /// <summary>
    /// 初始化动物页面的照片层
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
        }
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
                    new Vector2(0, -200)
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
                    new Vector2(0, -200)
                };
        }
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

        // 暂停游戏
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0;

        // 显示鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 禁用特定组件
        foreach (var component in componentsToDisable)
        {
            if (component != null)
                component.enabled = false;
        }

        // 显示第一页
        currentPageIndex = 0;
        ShowPage();
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

        // 如果是动物页面，更新照片
        UpdatePhotosForCurrentPage();

        // 更新按钮状态
        UpdateButtons();
    }

    /// <summary>
    /// 更新当前页面的照片
    /// </summary>
    void UpdatePhotosForCurrentPage()
    {
        // 查找当前页面是否是动物页面
        AnimalPage currentAnimalPage = animalPages.Find(p => p.pageIndex == currentPageIndex);

        if (currentAnimalPage != null && loadedPhotos.ContainsKey(currentAnimalPage.animalName))
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
}