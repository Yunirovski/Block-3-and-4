using UnityEngine;
using TMPro;

/// <summary>
/// Singleton component that tracks the player's total score,
/// updates the on‑screen UI, and provides a global access point.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>
    /// Global instance of the ScoreManager.
    /// </summary>
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Text component used to display the total score value.")]
    [SerializeField] private TMP_Text scoreText;

    private int totalScore = 0;  // Accumulated score

    private void Awake()
    {
        // Enforce singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Uncomment the next line if you want this object to persist across scenes:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds points to the total score, updates the UI display, and logs the change.
    /// </summary>
    /// <param name="points">The number of points to add.</param>
    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreText();
        Debug.Log($"ScoreManager: Added {points} points. New total: {totalScore}");
    }

    /// <summary>
    /// Updates the TMP_Text component to reflect the current total score.
    /// Logs a warning if the text reference is missing.
    /// </summary>
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {totalScore}";
        }
        else
        {
            Debug.LogWarning("ScoreManager: 'scoreText' reference is not assigned. Cannot update score display.");
        }
    }
}
