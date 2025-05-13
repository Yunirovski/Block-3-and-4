// Assets/Scripts/Log/AnimalInfo.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Data/AnimalInfo")]
public class AnimalInfo : ScriptableObject
{
    public string animalId;          // ������ AnimalEvent.animalName ��ȫһ��
    public Region region;            // Polar / Savanna / Jungle
    public string displayName;       // ʨ�ӡ�Emperor Penguin ...
    [TextArea(3, 8)]
    public string description;       // Ȥζ����
}

public enum Region { Tutorial, Polar, Savanna, Jungle }
