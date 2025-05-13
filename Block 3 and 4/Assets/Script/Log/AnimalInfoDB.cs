// Assets/Scripts/Log/AnimalInfoDB.cs
using System.Collections.Generic;
using UnityEngine;

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
        id != null && map.TryGetValue(id, out var ai) ? ai : null;
}
