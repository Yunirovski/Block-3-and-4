// Assets/Scripts/Systems/PhotoCollectionManager.cs
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// 照片收集管理器：负责UI显示和用户交互
/// 与PhotoLibrary协作，提供照片收集状态的可视化
/// </summary>
public class PhotoCollectionManager : MonoBehaviour
{
    public static PhotoCollectionManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("用于显示照片收集摘要的UI文本")]
    [SerializeField] private TMP_Text collectionText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log($"PhotoCollectionManager: 发现重复实例，禁用此组件 {gameObject.name}");
            this.enabled = false;
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("PhotoCollectionManager: 单例实例已初始化");
    }

    private void Start()
    {
        Debug.Log("PhotoCollectionManager: Start被调用，准备订阅PhotoLibrary事件");

        // 订阅PhotoLibrary的数据变化事件
        if (PhotoLibrary.Instance != null)
        {
            PhotoLibrary.Instance.OnPhotoDatabaseChanged += UpdateCollectionText;
            UpdateCollectionText(); // 初始更新
            Debug.Log("PhotoCollectionManager: 成功订阅PhotoLibrary事件");
        }
        else
        {
            Debug.LogError("PhotoCollectionManager: 未找到PhotoLibrary实例");
        }
    }

    /// <summary>
    /// 添加照片到收集中（委托给PhotoLibrary）
    /// </summary>
    public bool AddPhoto(string animalName, string photoPath, int stars)
    {
        if (PhotoLibrary.Instance == null)
        {
            Debug.LogError("PhotoCollectionManager: 尝试添加照片时找不到PhotoLibrary实例");
            return false;
        }

        // 委托给PhotoLibrary执行实际添加操作
        bool success = PhotoLibrary.Instance.AddPhoto(animalName, photoPath, stars);

        // 如果照片达到上限，返回失败
        if (!success)
        {
            Debug.Log($"照片添加失败: {animalName}的照片数量已达上限({PhotoLibrary.MaxPerAnimal})");
        }
        else
        {
            Debug.Log($"成功添加照片: {animalName}, 路径: {photoPath}, 星级: {stars}");
        }

        // 不需要在这里调用UpdateCollectionText，因为PhotoLibrary会触发事件
        return success;
    }

    /// <summary>
    /// 删除照片
    /// </summary>
    public bool DeletePhoto(string animalName, int photoIndex)
    {
        if (PhotoLibrary.Instance == null)
        {
            Debug.LogError("PhotoCollectionManager: 尝试删除照片时找不到PhotoLibrary实例");
            return false;
        }

        // 委托给PhotoLibrary执行删除操作
        bool success = PhotoLibrary.Instance.DeletePhoto(animalName, photoIndex);

        if (success)
        {
            Debug.Log($"成功删除照片: {animalName}, 索引: {photoIndex}");
        }
        else
        {
            Debug.LogError($"删除照片失败: {animalName}, 索引: {photoIndex}");
        }

        // 不需要在这里调用UpdateCollectionText，因为PhotoLibrary会触发事件
        return success;
    }

    /// <summary>
    /// 获取指定动物的照片列表
    /// </summary>
    public IReadOnlyList<PhotoLibrary.PhotoEntry> GetPhotos(string animalName)
    {
        if (PhotoLibrary.Instance == null)
        {
            Debug.LogError("PhotoCollectionManager: 尝试获取照片时找不到PhotoLibrary实例");
            return new List<PhotoLibrary.PhotoEntry>();
        }

        return PhotoLibrary.Instance.GetPhotos(animalName);
    }

    /// <summary>
    /// 更新UI文本，显示当前照片收集状态
    /// </summary>
    private void UpdateCollectionText()
    {
        if (collectionText == null)
        {
            Debug.LogWarning("PhotoCollectionManager: collectionText为null，无法更新UI");
            return;
        }

        if (PhotoLibrary.Instance == null)
        {
            Debug.LogWarning("PhotoCollectionManager: PhotoLibrary实例为null，无法更新UI");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("照片收集:");

        foreach (string animalId in PhotoLibrary.Instance.GetAnimalIds())
        {
            int count = PhotoLibrary.Instance.GetPhotoCount(animalId);
            sb.AppendLine($"{animalId}: {count}/{PhotoLibrary.MaxPerAnimal} 张");
        }

        int total = PhotoLibrary.Instance.GetTotalPhotoCount();
        sb.AppendLine($"总计: {total} 张照片");

        collectionText.text = sb.ToString();
        Debug.Log("PhotoCollectionManager: UI文本已更新");
    }

    private void OnDisable()
    {
        // 只有在组件被禁用且为当前实例时，清除静态引用
        if (Instance == this && !this.enabled)
        {
            Debug.Log("PhotoCollectionManager: 单例实例被禁用");

            // 取消订阅事件以防止空引用异常
            if (PhotoLibrary.Instance != null)
            {
                PhotoLibrary.Instance.OnPhotoDatabaseChanged -= UpdateCollectionText;
                Debug.Log("PhotoCollectionManager: 已取消订阅PhotoLibrary事件");
            }

            // 不清除静态引用，保持实例
            // Instance = null;
        }
    }
}