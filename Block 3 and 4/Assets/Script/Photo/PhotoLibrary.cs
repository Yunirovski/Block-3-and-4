using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// ��Ƭ�⣺�洢�������������Ƭ��¼
/// </summary>
public class PhotoLibrary : MonoBehaviour
{
    public static PhotoLibrary Instance { get; private set; }

    // ÿ�ֶ�����ౣ�����Ƭ��
    public const int MaxPerAnimal = 8;

    /// <summary>
    /// ��Ƭ��Ŀ���ݽṹ
    /// </summary>
    [System.Serializable]
    public class PhotoEntry
    {
        public string path;   // �ļ�·��
        public int stars;     // �Ǽ�����
        public long timestamp; // ����ʱ���

        public PhotoEntry(string path, int stars)
        {
            this.path = path;
            this.stars = stars;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// ��Ƭ����
    /// </summary>
    [Serializable]
    public class PhotoData
    {
        public Dictionary<string, List<PhotoEntry>> animalPhotos = new Dictionary<string, List<PhotoEntry>>();
    }

    private PhotoData photoData = new PhotoData();

    // ��Ƭ���ݱ���¼�
    public event Action OnPhotoDatabaseChanged;

    // �����ļ�·��
    private string dataFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ���������ļ�·��
            dataFilePath = Path.Combine(Application.persistentDataPath, "photo_database.json");

            // ��������
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ��ȡ���ж���ID�б�
    /// </summary>
    public IReadOnlyList<string> GetAnimalIds()
    {
        return new List<string>(photoData.animalPhotos.Keys);
    }

    /// <summary>
    /// ��ȡָ���������Ƭ����
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
    /// ��ȡ����Ƭ����
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
    /// ��ȡ���������Ǽ�
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
    /// ��ȡָ���������Ƭ�б�
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
    /// �����Ƭ
    /// </summary>
    public bool AddPhoto(string animalName, string photoPath, int stars)
    {
        if (string.IsNullOrEmpty(animalName) || string.IsNullOrEmpty(photoPath))
        {
            Debug.LogError("PhotoLibrary: �����Ƭʧ�ܣ���Ч�Ķ������ƻ���Ƭ·��");
            return false;
        }

        // ȷ�������¼����
        if (!photoData.animalPhotos.ContainsKey(animalName))
        {
            photoData.animalPhotos[animalName] = new List<PhotoEntry>();
        }

        var photos = photoData.animalPhotos[animalName];

        // ����Ƿ�ﵽ����
        if (photos.Count >= MaxPerAnimal)
        {
            Debug.Log($"PhotoLibrary: {animalName}����Ƭ�����Ѵ�����({MaxPerAnimal})");
            return false;
        }

        // �������Ƭ
        photos.Add(new PhotoEntry(photoPath, stars));

        // ��������
        SaveData();

        // �����¼�
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// ɾ����Ƭ
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

        // ��ȡ��Ƭ·��
        string photoPath = photos[photoIndex].path;

        // ���б����Ƴ�
        photos.RemoveAt(photoIndex);

        // �����Ƭ�б�Ϊ�գ��Ƴ������¼
        if (photos.Count == 0)
        {
            photoData.animalPhotos.Remove(animalName);
        }

        // ����ɾ���ļ�
        try
        {
            if (File.Exists(photoPath))
            {
                File.Delete(photoPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PhotoLibrary: ɾ����Ƭ�ļ�ʧ��: {e.Message}");
        }

        // ��������
        SaveData();

        // �����¼�
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// �滻��Ƭ
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

        // ��ȡ����Ƭ·��
        string oldPhotoPath = photos[photoIndex].path;

        // ɾ�����ļ�
        try
        {
            if (File.Exists(oldPhotoPath))
            {
                File.Delete(oldPhotoPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PhotoLibrary: ɾ������Ƭ�ļ�ʧ��: {e.Message}");
        }

        // �滻��Ƭ����
        photos[photoIndex] = new PhotoEntry(newPhotoPath, stars);

        // ��������
        SaveData();

        // �����¼�
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// �������ݵ��ļ�
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
            Debug.LogError($"PhotoLibrary: ��������ʧ��: {e.Message}");
        }
    }

    /// <summary>
    /// ���ļ���������
    /// </summary>
    private void LoadData()
    {
        try
        {
            if (File.Exists(dataFilePath))
            {
                string json = File.ReadAllText(dataFilePath);
                photoData = JsonUtility.FromJson<PhotoData>(json);

                // ��֤������Ƭ�ļ��Ƿ����
                ValidatePhotoFiles();
            }
            else
            {
                photoData = new PhotoData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"PhotoLibrary: ��������ʧ��: {e.Message}");
            photoData = new PhotoData();
        }
    }

    /// <summary>
    /// ��֤��Ƭ�ļ��Ƿ���ڣ�ɾ����Ч��Ŀ
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

            // �Ӻ���ǰɾ��
            for (int i = indexesToRemove.Count - 1; i >= 0; i--)
            {
                photos.RemoveAt(indexesToRemove[i]);
            }

            // �������û����Ƭ�ˣ����Ϊɾ��
            if (photos.Count == 0)
            {
                animalsToRemove.Add(animalName);
                dataChanged = true;
            }
        }

        // ɾ��û����Ƭ�Ķ���
        foreach (string animalName in animalsToRemove)
        {
            photoData.animalPhotos.Remove(animalName);
        }

        // ��������б仯������
        if (dataChanged)
        {
            SaveData();
        }
    }
}