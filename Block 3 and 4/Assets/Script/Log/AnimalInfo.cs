// Assets/Scripts/Log/AnimalInfo.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Data/AnimalInfo")]
public class AnimalInfo : ScriptableObject
{
    public string animalId;          // 必须与 AnimalEvent.animalName 完全一致
    public Region region;            // Polar / Savanna / Jungle
    public string displayName;       // 狮子、Emperor Penguin ...
    [TextArea(3, 8)]
    public string description;       // 趣味介绍
}

public enum Region { Tutorial, Polar, Savanna, Jungle }
