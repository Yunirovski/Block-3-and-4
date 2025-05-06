using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class PhotoLibrary : MonoBehaviour
{
    public static PhotoLibrary Instance { get; private set; }

    [System.Serializable]
    public class PhotoEntry
    {
        public string path;
        public int stars;
    }

    const int MaxPerAnimal = 8;
    const string JsonFile = "photos.json";

    Dictionary<string, List<PhotoEntry>> dict = new();

    /* ---------------- Lifecycle ---------------- */
    void Awake()
    {
        if (Instance == null) { Instance = this; Load(); }
        else Destroy(gameObject);
    }

    /* ---------------- Public API ---------------- */
    public bool AddPhoto(string animalId, string path, int stars)
    {
        if (!dict.TryGetValue(animalId, out var list))
        {
            list = new List<PhotoEntry>();
            dict[animalId] = list;
        }

        if (list.Count >= MaxPerAnimal) return false;  // ÒÑÂú

        list.Add(new PhotoEntry { path = path, stars = stars });
        Save();
        return true;
    }

    public IReadOnlyList<PhotoEntry> GetPhotos(string animalId) =>
        dict.TryGetValue(animalId, out var l) ? l : new List<PhotoEntry>();

    /* ---------------- Persist ---------------- */
    void Load()
    {
        string p = Path.Combine(Application.persistentDataPath, JsonFile);
        if (!File.Exists(p)) return;

        dict = JsonConvert.DeserializeObject<Dictionary<string, List<PhotoEntry>>>
               (File.ReadAllText(p)) ?? new();
    }
    void Save()
    {
        string p = Path.Combine(Application.persistentDataPath, JsonFile);
        File.WriteAllText(p, JsonConvert.SerializeObject(dict, Formatting.Indented));
    }
}
