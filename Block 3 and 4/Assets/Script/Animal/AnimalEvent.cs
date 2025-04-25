using UnityEngine;
using UnityEngine.Events;

public class AnimalEvent : MonoBehaviour
{
    public string animalName;
    [Tooltip("If TRUE, this rare creature grants +1 bonus star")]
    public bool isEasterEgg;

    [System.Serializable] public class PhotoEvent : UnityEvent<string, int> { }
    public PhotoEvent onDetected;

    public void TriggerEvent(string photoPath, int stars)
    {
        if (isEasterEgg) stars += 1;
        stars = Mathf.Clamp(stars, 1, 5);   // 1-5бя

        onDetected?.Invoke(photoPath, stars);
        CurrencyManager.Instance?.AddStars(stars);

        Debug.Log($"{animalName} detected б· {stars}бя  ({photoPath})");
    }
}
