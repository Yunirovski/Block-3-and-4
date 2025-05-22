using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;

/// <summary>
/// 照片已满提示弹窗，当拍摄照片达到上限时显示
/// </summary>
public class FullPageAlertPopup : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("照片图像")]
    public Image photoImage;

    [Tooltip("提示消息文本")]
    public TMP_Text messageText;

    [Tooltip("保存按钮")]
    public Button saveButton;

    [Tooltip("替换按钮")]
    public Button replaceButton;

    [Tooltip("取消按钮")]
    public Button cancelButton;

    // 内部数据
    private string animalId;
    private string photoPath;
    private int stars;

    private void Start()
    {
        // 设置按钮事件
        if (saveButton != null)
            saveButton.onClick.AddListener(SavePhoto);

        if (replaceButton != null)
            replaceButton.onClick.AddListener(OpenReplaceSelector);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
    }

    /// <summary>
    /// 初始化弹窗
    /// </summary>
    public void Initialize(string animalId, string photoPath, int stars)
    {
        this.animalId = animalId;
        this.photoPath = photoPath;
        this.stars = stars;

        // 设置消息
        if (messageText != null)
        {
            messageText.text = $"{animalId}的照片已达上限({PhotoLibrary.MaxPerAnimal}张)。\n" +
                               "请选择：保存到设备、替换现有照片或取消。";
        }

        // 加载照片
        if (photoImage != null)
        {
            StartCoroutine(LoadPhoto(photoPath, photoImage));
        }
    }

    /// <summary>
    /// 保存照片到设备
    /// </summary>
    private void SavePhoto()
    {
        if (string.IsNullOrEmpty(photoPath) || !File.Exists(photoPath))
        {
            ShowError("无效的照片路径");
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
                ShowError($"创建保存目录失败: {e.Message}");
                return;
            }
        }

        // 创建文件名
        string fileName = $"{animalId}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.png";
        string destinationPath = Path.Combine(saveFolder, fileName);

        try
        {
            File.Copy(photoPath, destinationPath);
            Debug.Log($"照片已保存到: {destinationPath}");

            // 显示成功消息并关闭
            if (messageText != null)
            {
                messageText.text = $"照片已保存到：\n{destinationPath}";

                // 禁用按钮
                DisableAllButtons();

                // 3秒后关闭
                Invoke("Close", 3f);
            }
        }
        catch (System.Exception e)
        {
            ShowError($"保存照片失败: {e.Message}");
        }
    }

    /// <summary>
    /// 打开替换选择器
    /// </summary>
    private void OpenReplaceSelector()
    {
        var selectorPrefab = Resources.Load<GameObject>("Prefabs/ReplacePhotoSelector");
        if (selectorPrefab == null)
        {
            ShowError("找不到替换选择器预制体");
            return;
        }

        var selector = Instantiate(selectorPrefab).GetComponent<ReplacePhotoSelector>();
        if (selector != null)
        {
            selector.Initialize(animalId, photoPath, stars, () => {
                // 替换成功回调
                Close();
            });
        }
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    private void ShowError(string error)
    {
        if (messageText != null)
        {
            messageText.text = $"错误: {error}";
            messageText.color = Color.red;
        }

        Debug.LogError(error);
    }

    /// <summary>
    /// 禁用所有按钮
    /// </summary>
    private void DisableAllButtons()
    {
        if (saveButton != null)
            saveButton.interactable = false;

        if (replaceButton != null)
            replaceButton.interactable = false;

        if (cancelButton != null)
            cancelButton.interactable = false;
    }

    /// <summary>
    /// 关闭弹窗
    /// </summary>
    private void Close()
    {
        Destroy(gameObject);
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
}