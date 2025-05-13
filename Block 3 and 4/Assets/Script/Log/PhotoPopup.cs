using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 照片详情弹窗，显示动物的所有照片和信息
/// </summary>
public class PhotoPopup : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("主面板")]
    public GameObject popupPanel;

    [Tooltip("动物名称文本")]
    public TMP_Text animalNameText;

    [Tooltip("动物描述文本")]
    public TMP_Text descriptionText;

    [Tooltip("照片显示图像")]
    public Image photoImage;

    [Tooltip("星级图标数组")]
    public Image[] starImages;

    [Tooltip("翻页按钮 - 上一张")]
    public Button prevButton;

    [Tooltip("翻页按钮 - 下一张")]
    public Button nextButton;

    [Tooltip("照片索引文本 (例如 2/5)")]
    public TMP_Text indexText;

    [Tooltip("关闭按钮")]
    public Button closeButton;

    [Header("管理按钮")]
    public Button deleteButton;
    public Button saveButton;

    // 照片数据
    private string animalId;
    private List<PhotoLibrary.PhotoEntry> photos = new List<PhotoLibrary.PhotoEntry>();
    private int currentPhotoIndex = 0;

    private void Start()
    {
        // 设置按钮事件
        if (prevButton != null)
            prevButton.onClick.AddListener(ShowPreviousPhoto);

        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextPhoto);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(DeleteCurrentPhoto);

        if (saveButton != null)
            saveButton.onClick.AddListener(SaveCurrentPhoto);
    }

    /// <summary>
    /// 初始化弹窗
    /// </summary>
    public void Initialize(string animalId, int initialPhotoIndex, AnimalInfoDB infoDatabase)
    {
        this.animalId = animalId;

        // 获取照片列表
        if (PhotoLibrary.Instance != null)
        {
            photos = new List<PhotoLibrary.PhotoEntry>(PhotoLibrary.Instance.GetPhotos(animalId));
        }

        // 设置初始照片索引
        currentPhotoIndex = Mathf.Clamp(initialPhotoIndex, 0, photos.Count - 1);

        // 设置动物信息
        if (animalNameText != null)
        {
            animalNameText.text = GetDisplayName(animalId, infoDatabase);
        }

        if (descriptionText != null && infoDatabase != null)
        {
            AnimalInfo info = infoDatabase.GetAnimalInfo(animalId);
            descriptionText.text = info != null ? info.description : "没有可用的描述信息。";
        }

        // 显示照片
        UpdatePhotoDisplay();
    }

    /// <summary>
    /// 获取动物显示名称
    /// </summary>
    private string GetDisplayName(string animalId, AnimalInfoDB infoDatabase)
    {
        // 从数据库获取
        if (infoDatabase != null)
        {
            AnimalInfo info = infoDatabase.GetAnimalInfo(animalId);
            if (info != null && !string.IsNullOrEmpty(info.displayName))
            {
                return info.displayName;
            }
        }

        // 格式化ID为显示名称
        string name = animalId.Replace('_', ' ');
        if (name.Length > 0)
        {
            name = char.ToUpper(name[0]) + name.Substring(1);
        }
        return name;
    }

    /// <summary>
    /// 显示上一张照片
    /// </summary>
    public void ShowPreviousPhoto()
    {
        if (photos.Count == 0) return;

        currentPhotoIndex--;
        if (currentPhotoIndex < 0)
            currentPhotoIndex = photos.Count - 1;

        UpdatePhotoDisplay();
    }

    /// <summary>
    /// 显示下一张照片
    /// </summary>
    public void ShowNextPhoto()
    {
        if (photos.Count == 0) return;

        currentPhotoIndex++;
        if (currentPhotoIndex >= photos.Count)
            currentPhotoIndex = 0;

        UpdatePhotoDisplay();
    }

    /// <summary>
    /// 删除当前照片
    /// </summary>
    public void DeleteCurrentPhoto()
    {
        if (photos.Count == 0) return;

        // 确认删除对话框
        ConfirmationDialog dialog = Instantiate(Resources.Load<GameObject>("Prefabs/ConfirmationDialog"))
            .GetComponent<ConfirmationDialog>();

        if (dialog != null)
        {
            dialog.Initialize(
                "确认删除",
                "确定要删除这张照片吗？此操作无法撤销。",
                () => {
                    // 确认删除
                    PerformDelete();
                },
                null // 取消操作
            );
        }
        else
        {
            // 如果没有对话框预制体，直接删除
            PerformDelete();
        }
    }

    /// <summary>
    /// 执行删除操作
    /// </summary>
    private void PerformDelete()
    {
        if (PhotoLibrary.Instance == null || photos.Count == 0) return;

        // 执行删除
        if (PhotoLibrary.Instance.DeletePhoto(animalId, currentPhotoIndex))
        {
            // 更新本地数组
            photos.RemoveAt(currentPhotoIndex);

            // 调整索引
            if (currentPhotoIndex >= photos.Count && photos.Count > 0)
                currentPhotoIndex = photos.Count - 1;

            // 如果没有照片了就关闭弹窗
            if (photos.Count == 0)
            {
                Close();
            }
            else
            {
                // 更新显示
                UpdatePhotoDisplay();
            }
        }
    }

    /// <summary>
    /// 保存当前照片到设备
    /// </summary>
    public void SaveCurrentPhoto()
    {
        if (photos.Count == 0) return;

        string sourcePath = photos[currentPhotoIndex].path;
        if (!File.Exists(sourcePath))
        {
            Debug.LogError($"找不到照片文件: {sourcePath}");

            if (descriptionText != null)
            {
                string originalText = descriptionText.text;
                descriptionText.text = "错误：找不到照片文件";
                StartCoroutine(RestoreTextAfterDelay(descriptionText, originalText, 3f));
            }

            return;
        }

        // 创建保存目录
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string saveFolder = Path.Combine(desktopPath, "AnimalPhotos");

        if (!Directory.Exists(saveFolder))
        {
            try
            {
                Directory.CreateDirectory(saveFolder);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建保存目录失败: {e.Message}");

                if (descriptionText != null)
                {
                    string originalText = descriptionText.text;
                    descriptionText.text = "错误：无法创建保存目录";
                    StartCoroutine(RestoreTextAfterDelay(descriptionText, originalText, 3f));
                }

                return;
            }
        }

        // 创建文件名
        string fileName = $"{animalId}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.png";
        string destinationPath = Path.Combine(saveFolder, fileName);

        try
        {
            File.Copy(sourcePath, destinationPath);
            Debug.Log($"照片已保存到: {destinationPath}");

            // 显示成功消息
            if (descriptionText != null)
            {
                string originalText = descriptionText.text;
                descriptionText.text = $"照片已保存到：\n{destinationPath}";
                StartCoroutine(RestoreTextAfterDelay(descriptionText, originalText, 3f));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存照片失败: {e.Message}");

            if (descriptionText != null)
            {
                string originalText = descriptionText.text;
                descriptionText.text = $"保存失败: {e.Message}";
                StartCoroutine(RestoreTextAfterDelay(descriptionText, originalText, 3f));
            }
        }
    }

    /// <summary>
    /// 延迟后恢复文本
    /// </summary>
    private IEnumerator RestoreTextAfterDelay(TMP_Text textComponent, string originalText, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (textComponent != null)
            textComponent.text = originalText;
    }

    /// <summary>
    /// 更新照片显示
    /// </summary>
    private void UpdatePhotoDisplay()
    {
        if (photos.Count == 0)
        {
            if (photoImage != null)
                photoImage.gameObject.SetActive(false);

            if (indexText != null)
                indexText.text = "无照片";

            if (prevButton != null)
                prevButton.interactable = false;

            if (nextButton != null)
                nextButton.interactable = false;

            if (deleteButton != null)
                deleteButton.interactable = false;

            if (saveButton != null)
                saveButton.interactable = false;

            // 隐藏所有星星
            if (starImages != null)
            {
                foreach (var star in starImages)
                {
                    if (star != null) star.gameObject.SetActive(false);
                }
            }

            return;
        }

        // 启用UI元素
        if (photoImage != null)
            photoImage.gameObject.SetActive(true);

        if (prevButton != null)
            prevButton.interactable = photos.Count > 1;

        if (nextButton != null)
            nextButton.interactable = photos.Count > 1;

        if (deleteButton != null)
            deleteButton.interactable = true;

        if (saveButton != null)
            saveButton.interactable = true;

        // 获取当前照片
        PhotoLibrary.PhotoEntry currentPhoto = photos[currentPhotoIndex];

        // 显示索引信息
        if (indexText != null)
            indexText.text = $"{currentPhotoIndex + 1}/{photos.Count}";

        // 显示星级
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                    starImages[i].gameObject.SetActive(i < currentPhoto.stars);
            }
        }

        // 加载照片
        if (photoImage != null)
        {
            StartCoroutine(LoadPhoto(currentPhoto.path, photoImage));
        }
    }

    /// <summary>
    /// 加载照片
    /// </summary>
    private IEnumerator LoadPhoto(string path, Image targetImage)
    {
        if (string.IsNullOrEmpty(path) || targetImage == null) yield break;

        // 重置图像
        targetImage.sprite = null;
        targetImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 加载中灰色

        // 创建文件URL
        string url = "file://" + path;

        // 加载图像
        using (UnityEngine.Networking.UnityWebRequest request =
               UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError($"加载照片失败: {request.error}");
                targetImage.color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // 错误红色
                yield break;
            }

            // 创建精灵
            Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
            if (texture != null)
            {
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * 0.5f
                );

                // 设置图像
                targetImage.sprite = sprite;
                targetImage.color = Color.white;
                targetImage.preserveAspect = true;
            }
        }
    }

    /// <summary>
    /// 关闭弹窗
    /// </summary>
    public void Close()
    {
        Destroy(gameObject);
    }
}