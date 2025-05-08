// Assets/Scripts/UI/AnimalStarUI.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 日志面板或动物一览用的 UI 控制脚本：
/// 在 Inspector 填写所有动物的 key 和对应的星级文本元素，
/// 脚本会根据 ProgressionManager 的 BestStars 数据初始化显示，
/// 并订阅 OnAnimalStarUpdated 实时刷新最高星级显示。
/// </summary>
public class AnimalStarUI : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("与 AnimalEvent.animalName 对应的唯一键")]
        public string animalKey;
        [Tooltip("用于显示该动物最高星级的 TextMeshProUGUI")]
        public TMP_Text starText;
    }

    [Header("在此列表中添加所有动物条目")]
    public List<Entry> entries = new List<Entry>();

    // 快速查找表
    Dictionary<string, Entry> entryMap;

    void Awake()
    {
        entryMap = new Dictionary<string, Entry>(entries.Count);
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.animalKey) || e.starText == null)
            {
                Debug.LogWarning($"AnimalStarUI: 忽略无效条目 '{e.animalKey}'");
                continue;
            }
            if (!entryMap.ContainsKey(e.animalKey))
                entryMap[e.animalKey] = e;
            else
                Debug.LogWarning($"AnimalStarUI: 重复的 animalKey '{e.animalKey}'");
        }
    }

    void OnEnable()
    {
        // 1) 初始化已有数据
        var best = ProgressionManager.Instance.BestStars;
        foreach (var kvp in best)
            UpdateStar(kvp.Key, kvp.Value);

        // 2) 订阅后续更新
        ProgressionManager.Instance.OnAnimalStarUpdated += OnStarUpdated;
    }

    void OnDisable()
    {
        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.OnAnimalStarUpdated -= OnStarUpdated;
    }

    // 回调：某个动物的最高星级更新了
    void OnStarUpdated(string animalKey, int newStars)
    {
        UpdateStar(animalKey, newStars);
    }

    // 根据 key 更新对应文本
    void UpdateStar(string animalKey, int stars)
    {
        if (entryMap != null && entryMap.TryGetValue(animalKey, out var entry))
        {
            entry.starText.text = $"{stars} ★";
        }
    }
}
