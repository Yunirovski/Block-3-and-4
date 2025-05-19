using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 照片库：存储所有已拍摄的照片记录
/// </summary>
public class PhotoLibrary : MonoBehaviour
{
    public static PhotoLibrary Instance { get; private set; }

    // 每种动物最多保存的照片数
    public const int MaxPerAnimal = 8;

    /// <summary>
    /// 照片条目数据结构
    /// </summary>
    [System.Serializable]
    public class PhotoEntry
    {
        public string path;   // 文件路径
        public int stars;     // 星级评分
        public long timestamp; // 拍摄时间戳

        public PhotoEntry(string path, int stars)
        {
            this.path = path;
            this.stars = stars;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// 照片数据
    /// </summary>
    [Serializable]
    public class PhotoData
    {
        public Dictionary<string, List<PhotoEntry>> animalPhotos = new Dictionary<string, List<PhotoEntry>>();
    }

    private PhotoData photoData = new PhotoData();

    // 照片数据变更事件
    public event Action OnPhotoDatabaseChanged;

    // 数据文件路径
    private string dataFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 设置数据文件路径
            dataFilePath = Path.Combine(Application.persistentDataPath, "photo_database.json");

            // 加载数据
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 获取所有动物ID列表
    /// </summary>
    public IReadOnlyList<string> GetAnimalIds()
    {
        return new List<string>(photoData.animalPhotos.Keys);
    }

    /// <summary>
    /// 获取指定动物的照片数量
    /// </summary>
    public int GetPhotoCount(string animalName)
    {
        if (photoData.animalPhotos.TryGetValue(animalName, out var photos))
        {
            return photos.Count;
        }
        return 0;
    }

    /// <summary>
    /// 获取总照片数量
    /// </summary>
    public int GetTotalPhotoCount()
    {
        int count = 0;
        foreach (var photos in photoData.animalPhotos.Values)
        {
            count += photos.Count;
        }
        return count;
    }

    /// <summary>
    /// 获取动物的最高星级
    /// </summary>
    public int GetMaxStars(string animalName)
    {
        if (!photoData.animalPhotos.TryGetValue(animalName, out var photos) || photos.Count == 0)
        {
            return 0;
        }

        int maxStars = 0;
        foreach (var photo in photos)
        {
            maxStars = Mathf.Max(maxStars, photo.stars);
        }
        return maxStars;
    }

    /// <summary>
    /// 获取指定动物的照片列表
    /// </summary>
    public IReadOnlyList<PhotoEntry> GetPhotos(string animalName)
    {
        if (photoData.animalPhotos.TryGetValue(animalName, out var photos))
        {
            return photos.AsReadOnly();
        }
        return new List<PhotoEntry>().AsReadOnly();
    }

    /// <summary>
    /// 添加照片
    /// </summary>
    public bool AddPhoto(string animalName, string photoPath, int stars)
    {
        if (string.IsNullOrEmpty(animalName) || string.IsNullOrEmpty(photoPath))
        {
            Debug.LogError("PhotoLibrary: 添加照片失败，无效的动物名称或照片路径");
            return false;
        }

        // 确保动物记录存在
        if (!photoData.animalPhotos.ContainsKey(animalName))
        {
            photoData.animalPhotos[animalName] = new List<PhotoEntry>();
        }

        var photos = photoData.animalPhotos[animalName];

        // 检查是否达到上限
        if (photos.Count >= MaxPerAnimal)
        {
            Debug.Log($"PhotoLibrary: {animalName}的照片数量已达上限({MaxPerAnimal})");
            return false;
        }

        // 添加新照片
        photos.Add(new PhotoEntry(photoPath, stars));

        // 保存数据
        SaveData();

        // 触发事件
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 删除照片
    /// </summary>
    public bool DeletePhoto(string animalName, int photoIndex)
    {
        if (!photoData.animalPhotos.TryGetValue(animalName, out var photos))
        {
            return false;
        }

        if (photoIndex < 0 || photoIndex >= photos.Count)
        {
            return false;
        }

        // 获取照片路径
        string photoPath = photos[photoIndex].path;

        // 从列表中移除
        photos.RemoveAt(photoIndex);

        // 如果照片列表为空，移除动物记录
        if (photos.Count == 0)
        {
            photoData.animalPhotos.Remove(animalName);
        }

        // 尝试删除文件
        try
        {
            if (File.Exists(photoPath))
            {
                File.Delete(photoPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PhotoLibrary: 删除照片文件失败: {e.Message}");
        }

        // 保存数据
        SaveData();

        // 触发事件
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 替换照片
    /// </summary>
    public bool ReplacePhoto(string animalName, int photoIndex, string newPhotoPath, int stars)
    {
        if (!photoData.animalPhotos.TryGetValue(animalName, out var photos))
        {
            return false;
        }

        if (photoIndex < 0 || photoIndex >= photos.Count)
        {
            return false;
        }

        // 获取旧照片路径
        string oldPhotoPath = photos[photoIndex].path;

        // 删除旧文件
        try
        {
            if (File.Exists(oldPhotoPath))
            {
                File.Delete(oldPhotoPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PhotoLibrary: 删除旧照片文件失败: {e.Message}");
        }

        // 替换照片数据
        photos[photoIndex] = new PhotoEntry(newPhotoPath, stars);

        // 保存数据
        SaveData();

        // 触发事件
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 保存数据到文件
    /// </summary>
    private void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(photoData);
            File.WriteAllText(dataFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"PhotoLibrary: 保存数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从文件加载数据
    /// </summary>
    private void LoadData()
    {
        try
        {
            if (File.Exists(dataFilePath))
            {
                string json = File.ReadAllText(dataFilePath);
                photoData = JsonUtility.FromJson<PhotoData>(json);

                // 验证所有照片文件是否存在
                ValidatePhotoFiles();
            }
            else
            {
                photoData = new PhotoData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"PhotoLibrary: 加载数据失败: {e.Message}");
            photoData = new PhotoData();
        }
    }

    /// <summary>
    /// 验证照片文件是否存在，删除无效条目
    /// </summary>
    private void ValidatePhotoFiles()
    {
        bool dataChanged = false;
        List<string> animalsToRemove = new List<string>();

        foreach (var animalPair in photoData.animalPhotos)
        {
            string animalName = animalPair.Key;
            List<PhotoEntry> photos = animalPair.Value;

            List<int> indexesToRemove = new List<int>();
            for (int i = 0; i < photos.Count; i++)
            {
                if (!File.Exists(photos[i].path))
                {
                    indexesToRemove.Add(i);
                    dataChanged = true;
                }
            }

            // 从后向前删除
            for (int i = indexesToRemove.Count - 1; i >= 0; i--)
            {
                photos.RemoveAt(indexesToRemove[i]);
            }

            // 如果动物没有照片了，标记为删除
            if (photos.Count == 0)
            {
                animalsToRemove.Add(animalName);
                dataChanged = true;
            }
        }

        // 删除没有照片的动物
        foreach (string animalName in animalsToRemove)
        {
            photoData.animalPhotos.Remove(animalName);
        }

        // 如果数据有变化，保存
        if (dataChanged)
        {
            SaveData();
        }
    }
}