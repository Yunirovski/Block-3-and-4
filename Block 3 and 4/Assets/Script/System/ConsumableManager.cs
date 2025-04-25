using UnityEngine;
using System;

/// <summary>
/// Singleton manager for the player¡¯s consumable resources: film and food.
/// Tracks current counts, enforces capacity limits, and provides methods for use and replenishment.
/// Fires an event whenever any consumable count changes.
/// </summary>
public class ConsumableManager : MonoBehaviour
{
    /// <summary>
    /// Global access to the ConsumableManager instance.
    /// </summary>
    public static ConsumableManager Instance { get; private set; }

    [Header("Capacity Limits")]
    [Tooltip("Maximum number of film units the player can hold.")]
    public int filmCap = 20;

    [Tooltip("Maximum number of food units the player can hold.")]
    public int foodCap = 20;

    [Header("Initial Counts")]
    [Tooltip("Starting number of film units.")]
    [SerializeField]
    private int film = 5;

    [Tooltip("Starting number of food units.")]
    [SerializeField]
    private int food = 3;

    /// <summary>
    /// Current number of film units available for use.
    /// </summary>
    public int Film => film;

    /// <summary>
    /// Current number of food units available for use.
    /// </summary>
    public int Food => food;

    /// <summary>
    /// Event invoked whenever film or food counts are modified.
    /// Useful for updating UI or triggering game logic when consumables change.
    /// </summary>
    public event Action OnConsumableChanged;

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Public API

    /// <summary>
    /// Attempts to consume a specified amount of film.
    /// </summary>
    /// <param name="amount">Number of film units to use (default 1).</param>
    /// <returns>True if enough film was available and consumed; false otherwise.</returns>
    public bool UseFilm(int amount = 1)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"UseFilm called with non-positive amount: {amount}");
            return false;
        }

        if (film < amount)
        {
            // Not enough film available
            return false;
        }

        film -= amount;
        OnConsumableChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Attempts to consume a specified amount of food.
    /// </summary>
    /// <param name="amount">Number of food units to use (default 1).</param>
    /// <returns>True if enough food was available and consumed; false otherwise.</returns>
    public bool UseFood(int amount = 1)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"UseFood called with non-positive amount: {amount}");
            return false;
        }

        if (food < amount)
        {
            // Not enough food available
            return false;
        }

        food -= amount;
        OnConsumableChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Adds the specified amount of film, clamped to [0, filmCap].
    /// </summary>
    /// <param name="amount">Number of film units to add.</param>
    public void AddFilm(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"AddFilm called with non-positive amount: {amount}");
            return;
        }

        film = Mathf.Clamp(film + amount, 0, filmCap);
        OnConsumableChanged?.Invoke();
    }

    /// <summary>
    /// Adds the specified amount of food, clamped to [0, foodCap].
    /// </summary>
    /// <param name="amount">Number of food units to add.</param>
    public void AddFood(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"AddFood called with non-positive amount: {amount}");
            return;
        }

        food = Mathf.Clamp(food + amount, 0, foodCap);
        OnConsumableChanged?.Invoke();
    }

    #endregion
}
