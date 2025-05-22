using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动物信息数据库，存储所有动物的显示信息
/// </summary>
public class AnimalInfoDB : MonoBehaviour
{
    [Tooltip("动物信息列表")]
    public List<AnimalInfo> animalInfos = new List<AnimalInfo>();

    // 缓存字典，提高查找性能
    private Dictionary<string, AnimalInfo> infoCache;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitializeCache();
    }

    private void InitializeCache()
    {
        infoCache = new Dictionary<string, AnimalInfo>();
        foreach (var info in animalInfos)
        {
            if (!string.IsNullOrEmpty(info.animalId))
            {
                infoCache[info.animalId] = info;
            }
        }
    }

    /// <summary>
    /// 获取动物信息
    /// </summary>
    public AnimalInfo GetAnimalInfo(string animalId)
    {
        if (infoCache == null)
        {
            InitializeCache();
        }

        if (infoCache.TryGetValue(animalId, out AnimalInfo info))
        {
            return info;
        }
        return null;
    }
}