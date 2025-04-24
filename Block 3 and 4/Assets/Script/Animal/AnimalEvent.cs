using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles detection of an animal in a photo: invokes configured responses,
/// awards score based on star rarity, and logs the event.
/// </summary>
public class AnimalEvent : MonoBehaviour
{
    [Tooltip("The display name or identifier for this animal.")]
    public string animalName;

    [Tooltip("Star rating (rarity level) used as the score value when detected.")]
    public int rarityLevel = 1;

    /// <summary>
    /// UnityEvent that passes the photo path and star rating to any listeners.
    /// Configure additional reactions (e.g. sounds, VFX) in the Inspector.
    /// </summary>
    [System.Serializable]
    public class PhotoEvent : UnityEvent<string, int> { }

    [Tooltip("Event invoked when this animal is detected; provides photoPath and star rating.")]
    public PhotoEvent onDetected;

    /// <summary>
    /// Call this to trigger the detection workflow for this animal.
    /// Invokes any configured events, adds to the player's score, and logs details.
    /// </summary>
    /// <param name="photoPath">File path or identifier of the captured photo.</param>
    /// <param name="star">Number of stars awarded (also used as points).</param>
    public void TriggerEvent(string photoPath, int star)
    {
        // Invoke any listeners set up in the Inspector
        onDetected?.Invoke(photoPath, star);

        // Award the star count as points to the player
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(star);
        }
        else
        {
            Debug.LogWarning("AnimalEvent: ScoreManager instance not found; score not added.");
        }

        // Log formatted detection info for debugging and analytics
        Debug.LogFormat("{0} бя{1} detected (photo: {2})", animalName, star, photoPath);
    }
}
