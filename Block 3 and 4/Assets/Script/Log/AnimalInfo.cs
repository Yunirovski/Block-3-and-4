using UnityEngine;

[CreateAssetMenu(menuName = "Data/AnimalInfo")]
public class AnimalInfo : ScriptableObject
{
    [Tooltip("������ AnimalEvent.animalName ��ȫһ��")]
    public string animalId;

    public Region region = Region.Polar;

    public string displayName = "???";
    [TextArea(3, 8)]
    public string description;
}

public enum Region { Tutorial, Polar, Savanna, Jungle }
