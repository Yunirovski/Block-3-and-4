// Assets/Scripts/UI/AnimalStarUI.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// ��־������һ���õ� UI ���ƽű���
/// �� Inspector ��д���ж���� key �Ͷ�Ӧ���Ǽ��ı�Ԫ�أ�
/// �ű������ ProgressionManager �� BestStars ���ݳ�ʼ����ʾ��
/// ������ OnAnimalStarUpdated ʵʱˢ������Ǽ���ʾ��
/// </summary>
public class AnimalStarUI : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("�� AnimalEvent.animalName ��Ӧ��Ψһ��")]
        public string animalKey;
        [Tooltip("������ʾ�ö�������Ǽ��� TextMeshProUGUI")]
        public TMP_Text starText;
    }

    [Header("�ڴ��б���������ж�����Ŀ")]
    public List<Entry> entries = new List<Entry>();

    // ���ٲ��ұ�
    Dictionary<string, Entry> entryMap;

    void Awake()
    {
        entryMap = new Dictionary<string, Entry>(entries.Count);
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.animalKey) || e.starText == null)
            {
                Debug.LogWarning($"AnimalStarUI: ������Ч��Ŀ '{e.animalKey}'");
                continue;
            }
            if (!entryMap.ContainsKey(e.animalKey))
                entryMap[e.animalKey] = e;
            else
                Debug.LogWarning($"AnimalStarUI: �ظ��� animalKey '{e.animalKey}'");
        }
    }

    void OnEnable()
    {
        // 1) ��ʼ����������
        var best = ProgressionManager.Instance.BestStars;
        foreach (var kvp in best)
            UpdateStar(kvp.Key, kvp.Value);

        // 2) ���ĺ�������
        ProgressionManager.Instance.OnAnimalStarUpdated += OnStarUpdated;
    }

    void OnDisable()
    {
        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.OnAnimalStarUpdated -= OnStarUpdated;
    }

    // �ص���ĳ�����������Ǽ�������
    void OnStarUpdated(string animalKey, int newStars)
    {
        UpdateStar(animalKey, newStars);
    }

    // ���� key ���¶�Ӧ�ı�
    void UpdateStar(string animalKey, int stars)
    {
        if (entryMap != null && entryMap.TryGetValue(animalKey, out var entry))
        {
            entry.starText.text = $"{stars} ��";
        }
    }
}
