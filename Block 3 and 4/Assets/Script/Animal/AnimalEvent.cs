using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles the detection of an animal in a photo: invokes configured events,
/// awards stars via the CurrencyManager (both quota¡ï and total¡ï), and logs the event.
/// </summary>
public class AnimalEvent : MonoBehaviour
{
    [Tooltip("The display name or identifier for this animal.")]
    public string animalName;

    [Tooltip("Rarity level of the animal (1¨C3), used as part of the star rating.")]
    public int rarityLevel = 1;

    /// <summary>
    /// UnityEvent that passes the photo path and star rating to any listeners.
    /// Configure additional reactions (e.g. sounds, VFX) in the Inspector.
    /// </summary>
    [System.Serializable]
    public class PhotoEvent : UnityEvent<string, int> { }

    [Tooltip("Invoked when this animal is detected; provides (photoPath, starRating).")]
    public PhotoEvent onDetected;

    /// <summary>
    /// Call this to trigger the detection workflow for this animal.
    /// Invokes any configured events, awards stars, and logs details.
    /// </summary>
    /// <param name="photoPath">Filesystem path or identifier of the captured photo.</param>
    /// <param name="starRating">Number of stars awarded (1¨C3).</param>
    public void TriggerEvent(string photoPath, int starRating)
    {
        // 1) Invoke any listeners configured in the Inspector
        onDetected?.Invoke(photoPath, starRating);

        // 2) Award stars via CurrencyManager (updates both quota¡ï and total¡ï)
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddStars(starRating);
        }
        else
        {
            Debug.LogWarning($"AnimalEvent: CurrencyManager instance not found; stars not added for '{animalName}'.");
        }

        // 3) Log formatted detection info for debugging and analytics
        Debug.LogFormat("Detected Animal: {0} | Stars: {1} | Photo: {2}",
                        animalName, starRating, photoPath);
    }
}

