using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;  // 添加这一行引用

/// <summary>
/// 负责在日志(Journal)中展示动物照片。
/// 每个区域标签页会包含一个此组件，用于显示该区域的动物照片。
/// </summary>
public class PhotoLogUI : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("此组件展示的区域/分类名称 (polar/savanna/jungle/tutorial)")]
    public string regionKey;

    [Tooltip("动物信息数据库，用于获取动物名称和描述")]
    public AnimalInfoDB animalInfoDB;

    [Header("UI 引用")]
    [Tooltip("内容区域，用于生成动物条目")]
    public Transform contentParent;

    [Tooltip("动物条目预制体")]
    public GameObject animalEntryPrefab;

    [Tooltip("照片弹窗预制体")]
    public PhotoPopup photoPopupPrefab;

    [Header("分页设置")]
    [Tooltip("每页显示的照片数量")]
    public int photosPerPage = 4;

    [Tooltip("分页按钮和指示器")]
    public Button prevPageButton;
    public Button nextPageButton;
    public TMP_Text pageIndicatorText;

    // 当前显示的动物列表
    private List<string> displayedAnimals = new List<string>();

    // 条目UI引用字典
    private Dictionary<string, GameObject> animalEntries = new Dictionary<string, GameObject>();

    // 每个动物当前显示的页码
    private Dictionary<string, int> currentPages = new Dictionary<string, int>();

    // 当前选中的动物
    private string currentSelectedAnimal;

    private void Start()
    {
        // 订阅PhotoLibrary的变更事件
        if (PhotoLibrary.Instance != null)
        {
            PhotoLibrary.Instance.OnPhotoDatabaseChanged += RefreshDisplay;
        }

        // 查找AnimalInfoDB如果尚未赋值
        if (animalInfoDB == null)
        {
            animalInfoDB = FindObjectOfType<AnimalInfoDB>();
        }

        // 设置分页按钮
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(() => ChangePage(false));

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(() => ChangePage(true));

        // 初始刷新
        RefreshDisplay();
    }

    private void OnEnable()
    {
        // 页面激活时刷新显示
        RefreshDisplay();
    }

    /// <summary>
    /// 切换页面
    /// </summary>
    public void ChangePage(bool next)
    {
        if (string.IsNullOrEmpty(currentSelectedAnimal)) return;

        if (!currentPages.ContainsKey(currentSelectedAnimal))
            currentPages[currentSelectedAnimal] = 0;

        int totalPhotos = PhotoLibrary.Instance.GetPhotoCount(currentSelectedAnimal);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalPhotos / photosPerPage));

        int currentPage = currentPages[currentSelectedAnimal];

        if (next)
            currentPages[currentSelectedAnimal] = (currentPage + 1) % totalPages;
        else
            currentPages[currentSelectedAnimal] = (currentPage - 1 + totalPages) % totalPages;

        UpdateAnimalEntry(currentSelectedAnimal);
    }

    /// <summary>
    /// 更新页码指示器
    /// </summary>
    private void UpdatePageIndicator(string animalId)
    {
        if (pageIndicatorText == null) return;

        if (!PhotoLibrary.Instance.GetAnimalIds().Contains(animalId))
        {
            pageIndicatorText.text = "无照片";

            if (prevPageButton != null)
                prevPageButton.interactable = false;

            if (nextPageButton != null)
                nextPageButton.interactable = false;

            return;
        }

        int totalPhotos = PhotoLibrary.Instance.GetPhotoCount(animalId);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalPhotos / photosPerPage));
        int currentPage = currentPages.ContainsKey(animalId) ? currentPages[animalId] + 1 : 1;

        pageIndicatorText.text = $"页 {currentPage}/{totalPages}";

        // 更新按钮状态
        if (prevPageButton != null)
            prevPageButton.interactable = totalPages > 1;

        if (nextPageButton != null)
            nextPageButton.interactable = totalPages > 1;
    }

    /// <summary>
    /// 刷新整个动物列表显示
    /// </summary>
    public void RefreshDisplay()
    {
        // 只有当游戏对象激活时才刷新
        if (!gameObject.activeInHierarchy) return;

        if (PhotoLibrary.Instance == null) return;

        // 获取属于此区域的所有动物ID
        List<string> regionAnimals = new List<string>();
        foreach (string animalId in PhotoLibrary.Instance.GetAnimalIds())
        {
            // 检查动物是否属于当前区域
            if (BelongsToRegion(animalId, regionKey))
            {
                regionAnimals.Add(animalId);
            }
        }

        // 删除不再存在的条目
        List<string> toRemove = new List<string>();
        foreach (string displayedAnimal in displayedAnimals)
        {
            if (!regionAnimals.Contains(displayedAnimal) &&
                !IsUnknownAnimalEntry(displayedAnimal))
            {
                toRemove.Add(displayedAnimal);
            }
        }

        foreach (string animal in toRemove)
        {
            RemoveAnimalEntry(animal);
        }

        // 添加新条目并更新现有条目
        foreach (string animalId in regionAnimals)
        {
            if (!displayedAnimals.Contains(animalId))
            {
                CreateAnimalEntry(animalId);

                // 设置为当前选中动物（如果是第一个）
                if (string.IsNullOrEmpty(currentSelectedAnimal))
                {
                    currentSelectedAnimal = animalId;
                }
            }
            else
            {
                UpdateAnimalEntry(animalId);
            }
        }

        // 添加未解锁的动物
        AddUnknownAnimals();

        // 更新页码指示器
        if (!string.IsNullOrEmpty(currentSelectedAnimal))
        {
            UpdatePageIndicator(currentSelectedAnimal);
        }
        else if (pageIndicatorText != null)
        {
            pageIndicatorText.text = "无照片";
        }
    }

    /// <summary>
    /// 判断动物是否属于当前区域
    /// </summary>
    private bool BelongsToRegion(string animalId, string region)
    {
        // 从AnimalInfoDB获取区域信息
        if (animalInfoDB != null)
        {
            AnimalInfo info = animalInfoDB.GetAnimalInfo(animalId);
            if (info != null)
            {
                return info.region.ToLower() == region.ToLower();
            }
        }

        // 如果没有数据库匹配，通过命名约定判断
        // 例如: polar_bear, savanna_lion, jungle_parrot
        return animalId.ToLower().StartsWith(region.ToLower() + "_");
    }

    /// <summary>
    /// 判断是否为未解锁的动物条目
    /// </summary>
    private bool IsUnknownAnimalEntry(string animalId)
    {
        if (animalInfoDB == null) return false;

        AnimalInfo info = animalInfoDB.GetAnimalInfo(animalId);
        return info != null && info.region.ToLower() == regionKey.ToLower();
    }

    /// <summary>
    /// 创建新的动物条目
    /// </summary>
    private void CreateAnimalEntry(string animalId)
    {
        if (animalEntryPrefab == null || contentParent == null) return;

        GameObject entryObj = Instantiate(animalEntryPrefab, contentParent);
        entryObj.name = "Entry_" + animalId;

        // 设置动物名称
        TMP_Text nameText = entryObj.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
        {
            string displayName = GetDisplayName(animalId);
            nameText.text = displayName;
        }

        // 初始化照片显示区域
        Transform photoContainer = entryObj.transform.Find("PhotoContainer");
        if (photoContainer != null)
        {
            // 初始化照片格子
            for (int i = 0; i < photosPerPage; i++)
            {
                Transform photoSlot = photoContainer.Find($"PhotoSlot{i + 1}");
                if (photoSlot != null)
                {
                    // 清空默认图片
                    Image photoImage = photoSlot.GetComponent<Image>();
                    if (photoImage != null)
                    {
                        photoImage.sprite = null;
                        photoImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                    }

                    // 禁用点击按钮
                    Button photoButton = photoSlot.GetComponent<Button>();
                    if (photoButton != null)
                    {
                        photoButton.interactable = false;
                    }
                }
            }
        }

        // 设置星级显示
        Transform starsParent = entryObj.transform.Find("Stars");
        if (starsParent != null)
        {
            int maxStars = PhotoLibrary.Instance.GetMaxStars(animalId);
            SetStarsVisibility(starsParent, maxStars);
        }

        // 设置选择事件
        Button selectButton = entryObj.GetComponentInChildren<Button>();
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() => {
                SelectAnimal(animalId);
            });
        }

        // 添加到条目字典和显示列表
        animalEntries[animalId] = entryObj;
        displayedAnimals.Add(animalId);

        // 初始化页码
        if (!currentPages.ContainsKey(animalId))
        {
            currentPages[animalId] = 0;
        }

        // 加载照片
        UpdateAnimalEntry(animalId);
    }

    /// <summary>
    /// 选择动物并更新显示
    /// </summary>
    private void SelectAnimal(string animalId)
    {
        currentSelectedAnimal = animalId;
        UpdatePageIndicator(animalId);

        // 高亮选中条目
        foreach (var entry in animalEntries)
        {
            Image background = entry.Value.GetComponent<Image>();
            if (background != null)
            {
                background.color = entry.Key == animalId
                    ? new Color(0.3f, 0.6f, 0.9f, 0.5f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }
    }

    /// <summary>
    /// 创建未解锁的动物条目
    /// </summary>
    private void CreateUnknownAnimalEntry(string animalId)
    {
        if (animalEntryPrefab == null || contentParent == null) return;

        // 检查是否已存在
        if (displayedAnimals.Contains(animalId)) return;

        GameObject entryObj = Instantiate(animalEntryPrefab, contentParent);
        entryObj.name = "Unknown_" + animalId;

        // 设置为问号
        TMP_Text nameText = entryObj.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = "???";
        }

        // 照片容器显示为灰色锁定状态
        Transform photoContainer = entryObj.transform.Find("PhotoContainer");
        if (photoContainer != null)
        {
            Image containerImage = photoContainer.GetComponent<Image>();
            if (containerImage != null)
            {
                containerImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }

            // 禁用所有照片槽
            for (int i = 0; i < photosPerPage; i++)
            {
                Transform photoSlot = photoContainer.Find($"PhotoSlot{i + 1}");
                if (photoSlot != null)
                {
                    photoSlot.gameObject.SetActive(false);
                }
            }

            // 添加锁定图标
            GameObject lockIcon = new GameObject("LockIcon");
            lockIcon.transform.SetParent(photoContainer, false);
            Image lockImage = lockIcon.AddComponent<Image>();
            lockImage.sprite = Resources.Load<Sprite>("UI/lock_icon");
            lockImage.rectTransform.sizeDelta = new Vector2(64, 64);
            lockImage.rectTransform.anchoredPosition = Vector2.zero;
        }

        // 隐藏星级
        Transform starsParent = entryObj.transform.Find("Stars");
        if (starsParent != null)
        {
            starsParent.gameObject.SetActive(false);
        }

        // 禁用所有按钮
        Button[] buttons = entryObj.GetComponentsInChildren<Button>();
        foreach (var button in buttons)
        {
            button.interactable = false;
        }

        // 添加到字典和列表
        animalEntries[animalId] = entryObj;
        displayedAnimals.Add(animalId);
    }

    /// <summary>
    /// 添加区域中未拍摄的动物作为问号条目
    /// </summary>
    private void AddUnknownAnimals()
    {
        if (animalInfoDB == null) return;

        foreach (AnimalInfo info in animalInfoDB.animalInfos)
        {
            if (info.region.ToLower() == regionKey.ToLower())
            {
                bool hasPhotos = PhotoLibrary.Instance != null &&
                                PhotoLibrary.Instance.GetPhotoCount(info.animalId) > 0;

                if (!hasPhotos && !displayedAnimals.Contains(info.animalId))
                {
                    CreateUnknownAnimalEntry(info.animalId);
                }
            }
        }
    }

    /// <summary>
    /// 更新现有动物条目
    /// </summary>
    private void UpdateAnimalEntry(string animalId)
    {
        if (!animalEntries.TryGetValue(animalId, out GameObject entryObj)) return;

        // 获取照片列表
        var allPhotos = PhotoLibrary.Instance.GetPhotos(animalId);

        // 获取当前页
        int currentPage = currentPages.ContainsKey(animalId) ? currentPages[animalId] : 0;
        int startIndex = currentPage * photosPerPage;

        // 更新照片显示
        Transform photoContainer = entryObj.transform.Find("PhotoContainer");
        if (photoContainer != null)
        {
            // 更新每个照片槽
            for (int i = 0; i < photosPerPage; i++)
            {
                int photoIndex = startIndex + i;
                Transform photoSlot = photoContainer.Find($"PhotoSlot{i + 1}");

                if (photoSlot == null) continue;

                Image photoImage = photoSlot.GetComponent<Image>();
                Button photoButton = photoSlot.GetComponent<Button>();

                if (photoIndex < allPhotos.Count)
                {
                    // 有照片，加载缩略图
                    photoSlot.gameObject.SetActive(true);

                    // 加载照片
                    StartCoroutine(LoadThumbnail(allPhotos[photoIndex].path, photoImage));

                    // 设置按钮点击事件
                    if (photoButton != null)
                    {
                        int index = photoIndex; // 创建局部变量以在Lambda中使用
                        photoButton.onClick.RemoveAllListeners();
                        photoButton.onClick.AddListener(() => {
                            ShowPhotoPopup(animalId, index);
                        });
                        photoButton.interactable = true;
                    }

                    // 设置照片星级
                    Transform starIndicator = photoSlot.Find("StarIndicator");
                    if (starIndicator != null)
                    {
                        TMP_Text starText = starIndicator.GetComponent<TMP_Text>();
                        if (starText != null)
                        {
                            starText.text = $"{allPhotos[photoIndex].stars}★";
                            starText.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    // 无照片，显示空槽
                    if (photoImage != null)
                    {
                        photoImage.sprite = null;
                        photoImage.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
                    }

                    if (photoButton != null)
                    {
                        photoButton.onClick.RemoveAllListeners();
                        photoButton.interactable = false;
                    }

                    // 隐藏星级
                    Transform starIndicator = photoSlot.Find("StarIndicator");
                    if (starIndicator != null)
                    {
                        starIndicator.gameObject.SetActive(false);
                    }
                }
            }
        }

        // 更新星级显示
        Transform starsParent = entryObj.transform.Find("Stars");
        if (starsParent != null)
        {
            int maxStars = PhotoLibrary.Instance.GetMaxStars(animalId);
            SetStarsVisibility(starsParent, maxStars);
        }
    }

    /// <summary>
    /// 设置星级显示
    /// </summary>
    private void SetStarsVisibility(Transform starsParent, int count)
    {
        for (int i = 0; i < starsParent.childCount; i++)
        {
            Transform star = starsParent.GetChild(i);
            if (star != null)
            {
                star.gameObject.SetActive(i < count);
            }
        }
    }

    /// <summary>
    /// 移除动物条目
    /// </summary>
    private void RemoveAnimalEntry(string animalId)
    {
        if (animalEntries.TryGetValue(animalId, out GameObject entryObj))
        {
            Destroy(entryObj);
            animalEntries.Remove(animalId);
            displayedAnimals.Remove(animalId);

            // 如果删除的是当前选中的，重新选择
            if (animalId == currentSelectedAnimal)
            {
                currentSelectedAnimal = displayedAnimals.Count > 0 ? displayedAnimals[0] : "";
                UpdatePageIndicator(currentSelectedAnimal);
            }
        }
    }

    /// <summary>
    /// 显示照片弹窗
    /// </summary>
    private void ShowPhotoPopup(string animalId, int photoIndex)
    {
        if (photoPopupPrefab == null) return;

        PhotoPopup popup = Instantiate(photoPopupPrefab);
        popup.Initialize(animalId, photoIndex, animalInfoDB);
    }

    /// <summary>
    /// 获取动物的显示名称
    /// </summary>
    private string GetDisplayName(string animalId)
    {
        // 优先从动物信息数据库获取
        if (animalInfoDB != null)
        {
            AnimalInfo info = animalInfoDB.GetAnimalInfo(animalId);
            if (info != null && !string.IsNullOrEmpty(info.displayName))
            {
                return info.displayName;
            }
        }

        // 格式化ID作为备选名称
        string name = animalId.Replace('_', ' ');
        // 首字母大写
        if (name.Length > 0)
        {
            name = char.ToUpper(name[0]) + name.Substring(1);
        }
        return name;
    }

    /// <summary>
    /// 加载照片缩略图
    /// </summary>
    private IEnumerator LoadThumbnail(string path, Image targetImage)
    {
        if (string.IsNullOrEmpty(path) || targetImage == null) yield break;

        // 重置图像
        targetImage.sprite = null;
        targetImage.color = Color.white;

        // 创建文件URL
        string url = "file://" + path;

        // 加载图像
        using (UnityEngine.Networking.UnityWebRequest request =
               UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError($"加载照片缩略图失败: {request.error}");
                targetImage.color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // 显示错误颜色
                yield break;
            }

            // 创建精灵
            Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
            if (texture != null)
            {
                // 缩小纹理以提升性能
                int thumbnailSize = 256;
                TextureScale.Bilinear(texture, thumbnailSize, thumbnailSize);

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * 0.5f
                );

                // 设置图像
                targetImage.sprite = sprite;
                targetImage.preserveAspect = true;
            }
        }
    }
}