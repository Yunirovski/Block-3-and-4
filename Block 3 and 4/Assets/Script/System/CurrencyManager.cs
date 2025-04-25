using UnityEngine;
using System;

/// <summary>
/// Singleton manager for the player¡¯s star currency.
/// Tracks both the current spendable stars (QuotaStar) and the lifetime collected stars (TotalStar).
/// Fires an event whenever currency values change.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    /// <summary>
    /// Global access to the CurrencyManager instance.
    /// </summary>
    public static CurrencyManager Instance { get; private set; }

    #region Inspector Fields
    [Header("Currency Settings")]

    [Tooltip("Stars currently available to spend in the shop.")]
    [SerializeField]
    private int quotaStar = 0;

    [Tooltip("Total stars collected over the entire gameplay session (never decreases).")]
    [SerializeField]
    private int totalStar = 0;
    #endregion

    #region Public Properties
    /// <summary>
    /// Gets the number of stars the player can currently spend.
    /// </summary>
    public int QuotaStar => quotaStar;

    /// <summary>
    /// Gets the total number of stars the player has ever collected.
    /// This value only ever increases.
    /// </summary>
    public int TotalStar => totalStar;
    #endregion

    #region Events
    /// <summary>
    /// Event invoked whenever quotaStar or totalStar changes.
    /// Useful for updating UI or triggering other game logic on currency updates.
    /// </summary>
    public event Action OnCurrencyChanged;
    #endregion

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

    /// <summary>
    /// Adds the specified number of stars to both the current quota and the total collected.
    /// Invokes OnCurrencyChanged after updating.
    /// </summary>
    /// <param name="amount">Number of stars to add (must be positive).</param>
    public void AddStars(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"CurrencyManager.AddStars called with non-positive amount: {amount}");
            return;
        }

        quotaStar += amount;
        totalStar += amount;
        OnCurrencyChanged?.Invoke();
    }

    /// <summary>
    /// Attempts to spend the specified number of stars from the current quota.
    /// If successful, decreases quotaStar and invokes OnCurrencyChanged.
    /// </summary>
    /// <param name="amount">Number of stars to spend (must be positive).</param>
    /// <returns>
    /// True if the spend succeeded (enough stars available); false otherwise.
    /// </returns>
    public bool TrySpendStars(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"CurrencyManager.TrySpendStars called with non-positive amount: {amount}");
            return false;
        }

        if (quotaStar < amount)
        {
            Debug.Log($"CurrencyManager: Insufficient stars. Requested {amount}, but only {quotaStar} available.");
            return false;
        }

        quotaStar -= amount;
        OnCurrencyChanged?.Invoke();
        return true;
    }
}
