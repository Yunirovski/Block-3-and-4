using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 照片替换选择器，用于选择要替换的照片
/// </summary>
public class ReplacePhotoSelector : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("照片网格容器")]
    public Transform photoGrid;

    [Tooltip("照片项目预制体")]
    public GameObject photoItemPrefab;

    [Tooltip("标题文本")]
    public TMP_Text titleText;

    [Tooltip("取消按钮")]
    public Button cancelButton;

    // 内部数据
    private string animalId;
    private string newPhotoPath;
    private int newPhotoStars;
    private System.Action onReplaceComplete;

    private void Start()
    {
        // 设置取消按钮
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
    }

    /// <summary>
    /// 初始化选择器
    /// </summary>
    public void Initialize(string animalId, string photoPath, int stars, System.Action onCompleteCallback)
    {
        this.animalId = animalId;
        this.newPhotoPath = photoPath;
        this.newPhotoStars = stars;
        this.onReplaceComplete = onCompleteCallback;

        // 设置标题
        if (titleText != null)
        {
            titleText.text = $"选择要替换的{animalId}照片";
        }

        // 加载现有照片
        LoadExistingPhotos();
    }

    /// <summary>
    /// 加载现有照片
    /// </summary>
    private void LoadExistingPhotos()
    {
        if (PhotoLibrary.Instance == null || photoGrid == null) return;

        var photos = PhotoLibrary.Instance.GetPhotos(animalId);

        // 清空网格
        foreach (Transform child in photoGrid)
        {
            Destroy(child.gameObject);
        }

        // 加载照片
        for (int i = 0; i < photos.Count; i++)
        {
            int photoIndex = i; // 本地变量用于Lambda

            GameObject item = Instantiate(photoItemPrefab, photoGrid);
            if (item == null) continue;

            // 设置照片
            Image photoImage = item.GetComponentInChildren<Image>();
            if (photoImage != null)
            {
                StartCoroutine(LoadThumbnail(photos[i].path, photoImage));
            }

            // 设置星级
            Transform starsContainer = item.transform.Find("Stars");
            if (starsContainer != null)
            {
                SetStarsVisibility(starsContainer, photos[i].stars);
            }

            // 设置索引文本
            TMP_Text indexText = item.GetComponentInChildren<TMP_Text>();
            if (indexText != null)
            {
                indexText.text = $"#{photoIndex + 1} - {photos[i].stars}★";
            }

            // 设置选择按钮
            Button selectButton = item.GetComponent<Button>();
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => {
                    ReplacePhoto(photoIndex);
                });
            }
        }
    }

    /// <summary>
    /// 替换照片
    /// </summary>
    private void ReplacePhoto(int indexToReplace)
    {
        if (PhotoLibrary.Instance == null) return;

        // 显示确认对话框
        ConfirmationDialog dialog = Instantiate(Resources.Load<GameObject>("Prefabs/ConfirmationDialog"))
            .GetComponent<ConfirmationDialog>();

        if (dialog != null)
        {
            dialog.Initialize(
                "确认替换",
                $"确定要用新拍摄的{newPhotoStars}★照片替换第{indexToReplace + 1}张照片吗？",
                () => {
                    // 执行替换
                    PerformReplace(indexToReplace);
                },
                null // 取消不做任何事
            );
        }
        else
        {
            // 如果没有对话框预制体，直接替换
            PerformReplace(indexToReplace);
        }
    }

    /// <summary>
    /// 执行替换操作
    /// </summary>
    private void PerformReplace(int indexToReplace)
    {
        if (PhotoLibrary.Instance.ReplacePhoto(animalId, indexToReplace, newPhotoPath, newPhotoStars))
        {
            Debug.Log($"已将{animalId}的第{indexToReplace + 1}张照片替换为新照片");

            // 回调通知
            onReplaceComplete?.Invoke();

            // 关闭选择器
            Close();
        }
        else
        {
            Debug.LogError($"替换照片失败: {animalId} 索引 {indexToReplace}");
        }
    }

    /// <summary>
    /// 设置星级显示
    /// </summary>
    private void SetStarsVisibility(Transform starsParent, int count)
    {
        if (starsParent == null) return;

        for (int i = 0; i < starsParent.childCount; i++)
        {
            if (starsParent.GetChild(i) != null)
                starsParent.GetChild(i).gameObject.SetActive(i < count);
        }
    }

    /// <summary>
    /// 加载缩略图
    /// </summary>
    private IEnumerator LoadThumbnail(string path, Image targetImage)
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
                Debug.LogError($"加载照片缩略图失败: {request.error}");
                targetImage.color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // 错误红色
                yield break;
            }

            // 创建精灵
            Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
            if (texture != null)
            {
                // 缩小纹理
                int thumbnailSize = 128;
                TextureScale.Bilinear(texture, thumbnailSize, thumbnailSize);

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
    /// 关闭选择器
    /// </summary>
    private void Close()
    {
        Destroy(gameObject);
    }
}