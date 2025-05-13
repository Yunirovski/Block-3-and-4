using UnityEngine;

/// <summary>
/// 存储动物的显示信息
/// </summary>
[System.Serializable]
public class AnimalInfo
{
    [Tooltip("动物唯一ID (与AnimalEvent中的animalName匹配)")]
    public string animalId;

    [Tooltip("显示名称")]
    public string displayName;

    [Tooltip("区域 (polar/savanna/jungle/tutorial)")]
    public string region;

    [TextArea(3, 10)]
    [Tooltip("动物描述")]
    public string description;
}