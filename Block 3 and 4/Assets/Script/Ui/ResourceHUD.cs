using UnityEngine;
using TMPro;

/// <summary>
/// Persistent HUD in the top-right corner displaying player resources:
/// - Spendable stars (Quota¡ï)
/// - Total stars collected (Total¡ï)
/// - Available film rolls
/// - Available food units
/// <para>
/// Subscribes to CurrencyManager and ConsumableManager change events to update in real time,
/// and unsubscribes on destroy to prevent null-reference callbacks across scene loads.
/// </para>
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    [Header("UI Text References (assign in Inspector)")]
    [Tooltip("Text element showing current spendable stars (Quota¡ï).")]
    [SerializeField] private TMP_Text quotaText;

    [Tooltip("Text element showing total stars collected (¦²¡ï).")]
    [SerializeField] private TMP_Text totalText;

    [Tooltip("Text element showing current film count.")]
    [SerializeField] private TMP_Text filmText;

    [Tooltip("Text element showing current food count.")]
    [SerializeField] private TMP_Text foodText;

    private void Start()
    {
        // Perform an initial update of all HUD fields
        Refresh();

        // Subscribe to currency change events
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += Refresh;
        }

        // Subscribe to consumable change events
        if (ConsumableManager.Instance != null)
        {
            ConsumableManager.Instance.OnConsumableChanged += Refresh;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from currency change events to avoid callbacks after scene unload
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= Refresh;
        }

        // Unsubscribe from consumable change events to prevent null-reference exceptions
        if (ConsumableManager.Instance != null)
        {
            ConsumableManager.Instance.OnConsumableChanged -= Refresh;
        }
    }

    /// <summary>
    /// Updates all HUD text fields based on the latest values from the managers.
    /// </summary>
    private void Refresh()
    {
        // Update spendable stars (Quota¡ï)
        if (quotaText != null && CurrencyManager.Instance != null)
        {
            quotaText.text = $"¡ï {CurrencyManager.Instance.QuotaStar}";
        }

        // Update total stars collected (¦²¡ï)
        if (totalText != null && CurrencyManager.Instance != null)
        {
            totalText.text = $"¦² {CurrencyManager.Instance.TotalStar}";
        }

        // Update the film count
        if (filmText != null && ConsumableManager.Instance != null)
        {
            filmText.text = $"Film: {ConsumableManager.Instance.Film}";
        }

        // Update the food count
        if (foodText != null && ConsumableManager.Instance != null)
        {
            foodText.text = $"Food: {ConsumableManager.Instance.Food}";
        }
    }
}
