using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component for handling events when an animal is photographed/detected.
/// Attach this to any animal GameObject and configure via the Inspector.
/// </summary>
public class AnimalEvent : MonoBehaviour
{
    [Tooltip("The name of this animal, used for identification and categorization")]
    public string animalName;

    [Tooltip("Points awarded when this animal is successfully photographed")]
    public int scoreValue = 10;  // Default points; can be customized per animal in Inspector

    /// <summary>
    /// UnityEvent type that passes the photo path string to any listeners.
    /// You can hook up additional responses (e.g. animations, sounds) in the Inspector.
    /// </summary>
    [System.Serializable]
    public class PhotoEvent : UnityEvent<string> { }

    [Tooltip("Configure additional responses here (e.g. play sound or animation)")]
    public PhotoEvent onDetected;

    /// <summary>
    /// Call this method when the animal is detected in a photo.
    /// It will invoke any configured UnityEvents, update the score, and log details.
    /// </summary>
    /// <param name="photoPath">Filesystem path or identifier of the captured photo</param>
    public void TriggerEvent(string photoPath)
    {
        // Invoke any additional handlers set up in the Inspector.
        if (onDetected != null)
        {
            onDetected.Invoke(photoPath);
        }

        // Add points to the player's score via the ScoreManager singleton.
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }
        else
        {
            Debug.LogWarning("ScoreManager instance not found. Cannot add score.");
        }

        // Log detailed information for debugging and analytics.
        Debug.LogFormat(
            "{0} detected! Photo path: {1} | Points awarded: {2}",
            animalName, photoPath, scoreValue
        );
    }
}
