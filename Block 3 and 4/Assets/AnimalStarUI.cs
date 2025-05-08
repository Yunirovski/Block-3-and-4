// Assets/Scripts/UI/AnimalStarUI.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 自动生成 & 更新“动物-最高星级”列表的 UI。
/// 需要在 Inspector 里指定：
/// • container      —— 条目实例化到哪个父节点
/// • entryPrefab    —— 一个只有 TMP_Text 的简单预制体
/// </summary>
public class AnimalStarUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("所有条目实例化到这里之下（可以挂 VerticalLayoutGroup）")]
    public Transform container;

    [Tooltip("预制体：里边放一个 TMP_Text，用来显示“企鹅：3 ★”这类文字")]
    public GameObject entryPrefab;

    // 内部字典：animalKey → TMP_Text
    readonly Dictionary<string, TMP_Text> starTexts = new();

    /* ---------------- 生命周期 ---------------- */
    void OnEnable()
    {
        // 初始化已有数据
        foreach (var kvp in ProgressionManager.Instance.BestStars)
            CreateOrUpdate(kvp.Key, kvp.Value);

        // 订阅实时更新
        ProgressionManager.Instance.OnAnimalStarUpdated += CreateOrUpdate;
    }

    void OnDisable()
    {
        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.OnAnimalStarUpdated -= CreateOrUpdate;
    }

    /* ---------------- 主逻辑 ---------------- */
    void CreateOrUpdate(string animalKey, int stars)
    {
        if (string.IsNullOrEmpty(animalKey)) return;

        // 1) 若不存在，就实例化一行
        if (!starTexts.TryGetValue(animalKey, out TMP_Text text))
        {
            GameObject go = Instantiate(entryPrefab, container);
            text = go.GetComponentInChildren<TMP_Text>();
            if (text == null)
            {
                Debug.LogError("AnimalStarUI: entryPrefab 必须带 TMP_Text！");
                return;
            }
            starTexts[animalKey] = text;
        }

        // 2) 更新显示
        text.text = $"{animalKey}：{stars} ★";
    }
}
