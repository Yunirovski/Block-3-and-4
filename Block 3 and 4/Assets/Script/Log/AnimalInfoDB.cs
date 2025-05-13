using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在启动时自动扫描 Resources/AnimalInfo 下的所有 ScriptableObject，
/// 供 UI 通过 animalId → 获取 displayName / description / region 等静态数据。
/// </summary>
public static class AnimalInfoDB
{
    static readonly Dictionary<string, AnimalInfo> map = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        foreach (var info in Resources.LoadAll<AnimalInfo>("AnimalInfo"))
        {
            if (!string.IsNullOrEmpty(info.animalId))
                map[info.animalId] = info;
        }
    }

    public static AnimalInfo Lookup(string id) =>
        id != null && map.TryGetValue(id, out var info) ? info : null;
}
