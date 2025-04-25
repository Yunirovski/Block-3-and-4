// Assets/Scripts/AnimalEvent.cs

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles detection of an animal in a photo:
/// - Invokes configured PhotoEvent (photoPath, stars)
/// - Adds stars to the player¡¯s currency
/// - If this is an Easter-egg animal, gives +1 extra star
/// </summary>
public class AnimalEvent : MonoBehaviour
{
    [Tooltip("Display name for this animal.")]
    public string animalName;

    [Tooltip("If true, this is a 'Easter-egg' animal and grants +1 bonus star.")]
    public bool isEasterEgg;

    /// <summary>
    /// PhotoEvent passes back the saved photo path and the final star count.
    /// </summary>
    [System.Serializable]
    public class PhotoEvent : UnityEvent<string, int> { }

    [Tooltip("Configure additional reactions (VFX, sounds, etc.) in the Inspector.")]
    public PhotoEvent onDetected;

    /// <summary>
    /// Called by the photo-capture system when this animal is detected.
    /// </summary>
    /// <param name="photoPath">Filesystem path of the captured image.</param>
    /// <param name="stars">Base star rating computed by the detector.</param>
    public void TriggerEvent(string photoPath, int stars)
    {
        // If this is an Easter-egg animal, grant one extra star
        if (isEasterEgg)
        {
            stars += 1;
        }

        // Invoke any configured UnityEvent listeners
        onDetected?.Invoke(photoPath, stars);

        // Award the stars via the currency system
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddStars(stars);
        }
        else
        {
            Debug.LogWarning($"AnimalEvent: CurrencyManager not found; stars not added for '{animalName}'.");
        }

        // Debug log
        Debug.LogFormat(
            "AnimalEvent: {0} detected ¡ú {1}¡ï (EasterEgg: {2}) | Photo: {3}",
            animalName, stars, isEasterEgg, photoPath
        );
    }
}
