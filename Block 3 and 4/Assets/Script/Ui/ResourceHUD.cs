using UnityEngine;
using TMPro;

/// <summary>
/// 常驻右上角资源显示：Quota★、Total★、Film、Food。
/// 订阅并响应 CurrencyManager 与 ConsumableManager 的变化事件，
/// 并在销毁时解绑，防止跨场景残留回调导致空引用异常。
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    [Header("Text References (assign in Inspector)")]
    [SerializeField] private TMP_Text quotaText;
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private TMP_Text filmText;
    [SerializeField] private TMP_Text foodText;

    private void Start()
    {
        // 初次刷新
        Refresh();

        // 订阅变化事件
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += Refresh;
        if (ConsumableManager.Instance != null)
            ConsumableManager.Instance.OnConsumableChanged += Refresh;
    }

    private void OnDestroy()
    {
        // 解绑，防止场景卸载后回调空引用
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= Refresh;
        if (ConsumableManager.Instance != null)
            ConsumableManager.Instance.OnConsumableChanged -= Refresh;
    }

    /// <summary>
    /// 刷新所有文本字段。
    /// </summary>
    private void Refresh()
    {
        if (quotaText != null)
            quotaText.text = $"★ {CurrencyManager.Instance.QuotaStar}";
        if (totalText != null)
            totalText.text = $"Σ {CurrencyManager.Instance.TotalStar}";
        if (filmText != null)
            filmText.text = $"Film: {ConsumableManager.Instance.Film}";
        if (foodText != null)
            foodText.text = $"Food: {ConsumableManager.Instance.Food}";
    }
}
