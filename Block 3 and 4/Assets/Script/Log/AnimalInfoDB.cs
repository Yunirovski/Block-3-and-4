using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������ʱ�Զ�ɨ�� Resources/AnimalInfo �µ����� ScriptableObject��
/// �� UI ͨ�� animalId �� ��ȡ displayName / description / region �Ⱦ�̬���ݡ�
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
