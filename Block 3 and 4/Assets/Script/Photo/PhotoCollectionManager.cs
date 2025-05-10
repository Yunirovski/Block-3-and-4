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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 订阅PhotoLibrary的数据变化事件
        if (PhotoLibrary.Instance != null)
        {
            PhotoLibrary.Instance.OnPhotoDatabaseChanged += UpdateCollectionText;
            UpdateCollectionText(); // 初始更新
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
        return PhotoLibrary.Instance.DeletePhoto(animalName, photoIndex);
        // 不需要在这里调用UpdateCollectionText，因为PhotoLibrary会触发事件
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
        if (collectionText == null || PhotoLibrary.Instance == null)
        {
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
    }
}