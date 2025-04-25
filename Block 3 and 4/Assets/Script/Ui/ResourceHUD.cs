using UnityEngine;
using TMPro;

public class ResourceHUD : MonoBehaviour
{
    [SerializeField] TMP_Text quotaText;
    [SerializeField] TMP_Text totalText;
    [SerializeField] TMP_Text filmText;
    [SerializeField] TMP_Text foodText;
    [SerializeField] TMP_Text cooldownText;
    float nextReadyTime = 0f;
    BaseItem coolingItem;

    void Start()
    {
        // ³õ´ÎË¢ÐÂ
        Refresh();
        CurrencyManager.Instance.OnCurrencyChanged += Refresh;
        ConsumableManager.Instance.OnConsumableChanged += Refresh;
        InventorySystemEvents.OnItemCooldownStart += BeginCooldownDisplay;
    }
    void BeginCooldownDisplay(BaseItem item, float cd)
    {
        coolingItem = item;
        nextReadyTime = Time.time + cd;
        StartCoroutine(CooldownRoutine());
    }
    System.Collections.IEnumerator CooldownRoutine()
    {
        while (Time.time < nextReadyTime)
        {
            float remain = nextReadyTime - Time.time;
            cooldownText.text = $"{coolingItem.itemName} CD: {remain:F1}s";
            yield return null;
        }
        cooldownText.text = "";
    }
    void Refresh()
    {
        quotaText.text = $"¡ï {CurrencyManager.Instance.QuotaStar}";
        totalText.text = $"¦² {CurrencyManager.Instance.TotalStar}";
        filmText.text = $"Film: {ConsumableManager.Instance.Film}";
        foodText.text = $"Food: {ConsumableManager.Instance.Food}";
    }
}
