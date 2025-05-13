using UnityEngine;

[CreateAssetMenu(menuName = "Data/AnimalInfo")]
public class AnimalInfo : ScriptableObject
{
    [Tooltip("必须与 AnimalEvent.animalName 完全一致")]
    public string animalId;

    public Region region = Region.Polar;

    public string displayName = "???";
    [TextArea(3, 8)]
    public string description;
}

public enum Region { Tutorial, Polar, Savanna, Jungle }
