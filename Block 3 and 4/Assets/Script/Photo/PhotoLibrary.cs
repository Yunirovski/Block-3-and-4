using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// ��Ƭ��������ݲ㣺����洢�͹�����Ƭ���ݣ��ṩ�־û�
/// </summary>
public class PhotoLibrary : MonoBehaviour
{
    public static PhotoLibrary Instance { get; private set; }

    [System.Serializable]
    public class PhotoEntry
    {
        public string path;        // ��Ƭ�ļ�·��
        public int stars;          // ��Ƭ�Ǽ�
        public System.DateTime timestamp = System.DateTime.Now;  // ����ʱ��
    }

    // ÿ�ֶ������洢����Ƭ��
    public const int MaxPerAnimal = 8;
    private const string JsonFile = "photos.json";

    // ��Ƭ���ݽṹ������ID -> ��Ƭ�б�
    private Dictionary<string, List<PhotoEntry>> photoDatabase = new();

    // ��Ƭ���ݱ仯�¼�
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
    /// �������Ƭ������
    /// </summary>
    /// <param name="animalId">����ΨһID</param>
    /// <param name="path">��Ƭ·��</param>
    /// <param name="stars">�Ǽ�����</param>
    /// <returns>�Ƿ���ӳɹ���ʧ�ܱ�ʾ��Ƭ�ﵽ���ޣ�</returns>
    public bool AddPhoto(string animalId, string path, int stars)
    {
        if (!photoDatabase.TryGetValue(animalId, out var list))
        {
            list = new List<PhotoEntry>();
            photoDatabase[animalId] = list;
        }

        if (list.Count >= MaxPerAnimal)
            return false;  // �Ѵﵽ����

        list.Add(new PhotoEntry { path = path, stars = stars });
        SaveDatabase();

        // ֪ͨ�����������Ѹ���
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// ɾ��ָ�������ָ����Ƭ
    /// </summary>
    public bool DeletePhoto(string animalId, int photoIndex)
    {
        if (!photoDatabase.TryGetValue(animalId, out var list) ||
            photoIndex < 0 || photoIndex >= list.Count)
            return false;

        // ���ļ�ϵͳ��ɾ����Ƭ
        try
        {
            if (File.Exists(list[photoIndex].path))
                File.Delete(list[photoIndex].path);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ɾ����Ƭ�ļ�ʧ��: {e.Message}");
        }

        // �����ݿ����Ƴ�
        list.RemoveAt(photoIndex);
        SaveDatabase();

        // ֪ͨ�����������Ѹ���
        OnPhotoDatabaseChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// ��ȡָ�������������Ƭ
    /// </summary>
    public IReadOnlyList<PhotoEntry> GetPhotos(string animalId) =>
        photoDatabase.TryGetValue(animalId, out var list) ? list : new List<PhotoEntry>();

    /// <summary>
    /// ��ȡ����ID�б�
    /// </summary>
    public IEnumerable<string> GetAnimalIds() => photoDatabase.Keys;

    /// <summary>
    /// ��ȡ�ض��������Ƭ����
    /// </summary>
    public int GetPhotoCount(string animalId) =>
        photoDatabase.TryGetValue(animalId, out var list) ? list.Count : 0;

    /// <summary>
    /// ��ȡ����Ƭ����
    /// </summary>
    public int GetTotalPhotoCount()
    {
        int count = 0;
        foreach (var list in photoDatabase.Values)
            count += list.Count;
        return count;
    }

    /// <summary>
    /// ������Ƭ���ݿ�
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
            Debug.LogError($"������Ƭ���ݿ�ʧ��: {e.Message}");
            photoDatabase = new Dictionary<string, List<PhotoEntry>>();
        }
    }

    /// <summary>
    /// ������Ƭ���ݿ�
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
            Debug.LogError($"������Ƭ���ݿ�ʧ��: {e.Message}");
        }
    }
}