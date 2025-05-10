using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// 照片库核心数据层：负责存储和管理照片数据，提供持久化
/// </summary>
public class PhotoLibrary : MonoBehaviour
{
    public static PhotoLibrary Instance { get; private set; }

    [System.Serializable]
    public class PhotoEntry
    {
        public string path;        // 照片文件路径
        public int stars;          // 照片星级
        public System.DateTime timestamp = System.DateTime.Now;  // 拍摄时间
    }

    // 每种动物最多存储的照片数
    public const int MaxPerAnimal = 8;
    private const string JsonFile = "photos.json";

    // 照片数据结构：动物ID -> 照片列表
    private Dictionary<string, List<PhotoEntry>> photoDatabase = new();

    // 照片数据变化事件
    public System.Action OnPhotoDatabaseChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadDatabase();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 添加新照片到库中
    /// </summary>
    /// <param name="animalId">动物唯一ID</param>
    /// <param name="path">照片路径</param>
    /// <param name="stars">星级评分</param>
    /// <returns>是否添加成功（失败表示照片达到上限）</returns>
    public bool AddPhoto(string animalId, string path, int stars)
    {
        if (!photoDatabase.TryGetValue(animalId, out var list))
        {
            list = new List<PhotoEntry>();
            photoDatabase[animalId] = list;
        }

        if (list.Count >= MaxPerAnimal)
            return false;  // 已达到上限

        list.Add(new PhotoEntry { path = path, stars = stars });
        SaveDatabase();

        // 通知监听者数据已更改
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 删除指定动物的指定照片
    /// </summary>
    public bool DeletePhoto(string animalId, int photoIndex)
    {
        if (!photoDatabase.TryGetValue(animalId, out var list) ||
            photoIndex < 0 || photoIndex >= list.Count)
            return false;

        // 从文件系统中删除照片
        try
        {
            if (File.Exists(list[photoIndex].path))
                File.Delete(list[photoIndex].path);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"删除照片文件失败: {e.Message}");
        }

        // 从数据库中移除
        list.RemoveAt(photoIndex);
        SaveDatabase();

        // 通知监听者数据已更改
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 获取指定动物的所有照片
    /// </summary>
    public IReadOnlyList<PhotoEntry> GetPhotos(string animalId) =>
        photoDatabase.TryGetValue(animalId, out var list) ? list : new List<PhotoEntry>();

    /// <summary>
    /// 获取动物ID列表
    /// </summary>
    public IEnumerable<string> GetAnimalIds() => photoDatabase.Keys;

    /// <summary>
    /// 获取特定动物的照片数量
    /// </summary>
    public int GetPhotoCount(string animalId) =>
        photoDatabase.TryGetValue(animalId, out var list) ? list.Count : 0;

    /// <summary>
    /// 获取总照片数量
    /// </summary>
    public int GetTotalPhotoCount()
    {
        int count = 0;
        foreach (var list in photoDatabase.Values)
            count += list.Count;
        return count;
    }

    /// <summary>
    /// 加载照片数据库
    /// </summary>
    private void LoadDatabase()
    {
        string path = Path.Combine(Application.persistentDataPath, JsonFile);
        if (!File.Exists(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            photoDatabase = JsonConvert.DeserializeObject<Dictionary<string, List<PhotoEntry>>>(json)
                          ?? new Dictionary<string, List<PhotoEntry>>();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载照片数据库失败: {e.Message}");
            photoDatabase = new Dictionary<string, List<PhotoEntry>>();
        }
    }

    /// <summary>
    /// 保存照片数据库
    /// </summary>
    private void SaveDatabase()
    {
        string path = Path.Combine(Application.persistentDataPath, JsonFile);
        try
        {
            string json = JsonConvert.SerializeObject(photoDatabase, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存照片数据库失败: {e.Message}");
        }
    }
}