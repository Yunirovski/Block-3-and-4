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
    private static PhotoCollectionManager _instance;

    public static PhotoCollectionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PhotoCollectionManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("PhotoCollectionManager");
                    _instance = go.AddComponent<PhotoCollectionManager>();
                    Debug.Log("PhotoCollectionManager: 自动创建实例");
                }
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [Tooltip("用于显示照片收集摘要的UI文本")]
    [SerializeField] private TMP_Text collectionText;

    private void Awake()
    {
        Debug.Log($"PhotoCollectionManager: Awake被调用 - {gameObject.name}");

        if (_instance != null && _instance != this)
        {
            Debug.Log($"PhotoCollectionManager: 发现重复实例，禁用此组件 {gameObject.name}");
            this.enabled = false;
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("PhotoCollectionManager: 单例实例已初始化");
    }

    private void Start()
    {
        Debug.Log("PhotoCollectionManager: Start被调用，准备订阅PhotoLibrary事件");

        // 尝试多次查找PhotoLibrary，因为它可能还没初始化
        StartCoroutine(FindPhotoLibraryWithRetry(5));
    }

    // 使用协程带重试机制查找PhotoLibrary
    private System.Collections.IEnumerator FindPhotoLibraryWithRetry(int maxRetries)
    {
        int retries = 0;
        while (retries < maxRetries)
        {
            if (PhotoLibrary.Instance != null)
            {
                PhotoLibrary.Instance.OnPhotoDatabaseChanged += UpdateCollectionText;
                UpdateCollectionText(); // 初始更新
                Debug.Log("PhotoCollectionManager: 成功订阅PhotoLibrary事件");
                yield break;
            }

            Debug.Log($"PhotoCollectionManager: 未找到PhotoLibrary，等待重试 ({retries + 1}/{maxRetries})");
            retries++;
            yield return new WaitForSeconds(0.5f); // 等待半秒后重试
        }

        Debug.LogError("PhotoCollectionManager: 经过多次尝试仍未找到PhotoLibrary实例");
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

        try
        {
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
        catch (System.Exception e)
        {
            Debug.LogError($"PhotoCollectionManager: 添加照片时发生错误: {e.Message}");
            return false;
        }
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

        try
        {
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
        catch (System.Exception e)
        {
            Debug.LogError($"PhotoCollectionManager: 删除照片时发生错误: {e.Message}");
            return false;
        }
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

        try
        {
            return PhotoLibrary.Instance.GetPhotos(animalName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PhotoCollectionManager: 获取照片列表时发生错误: {e.Message}");
            return new List<PhotoLibrary.PhotoEntry>();
        }
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

        try
        {
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
        catch (System.Exception e)
        {
            Debug.LogError($"PhotoCollectionManager: 更新UI文本时发生错误: {e.Message}");
        }
    }

    private void OnDisable()
    {
        Debug.Log($"PhotoCollectionManager: OnDisable被调用 - {gameObject.name}");

        // 取消订阅事件以防止空引用异常
        if (PhotoLibrary.Instance != null)
        {
            try
            {
                PhotoLibrary.Instance.OnPhotoDatabaseChanged -= UpdateCollectionText;
                Debug.Log("PhotoCollectionManager: 已取消订阅PhotoLibrary事件");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PhotoCollectionManager: 取消订阅事件时发生错误: {e.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        Debug.Log($"PhotoCollectionManager: OnDestroy被调用 - {gameObject.name}");

        // 取消订阅事件以防止空引用异常
        if (PhotoLibrary.Instance != null)
        {
            try
            {
                PhotoLibrary.Instance.OnPhotoDatabaseChanged -= UpdateCollectionText;
                Debug.Log("PhotoCollectionManager: 已取消订阅PhotoLibrary事件");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PhotoCollectionManager: 取消订阅事件时发生错误: {e.Message}");
            }
        }

        // 只有当当前实例被销毁时才清除静态引用
        if (_instance == this)
        {
            Debug.Log("PhotoCollectionManager: 单例实例被销毁");
            _instance = null;
        }
    }
}