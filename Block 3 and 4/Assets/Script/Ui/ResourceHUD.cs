using UnityEngine;
using TMPro;

/// <summary>
/// ��פ���Ͻ���Դ��ʾ��Quota�Total�Film��Food��
/// ���Ĳ���Ӧ CurrencyManager �� ConsumableManager �ı仯�¼���
/// ��������ʱ��󣬷�ֹ�糡�������ص����¿������쳣��
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
        // ����ˢ��
        Refresh();

        // ���ı仯�¼�
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += Refresh;
        if (ConsumableManager.Instance != null)
            ConsumableManager.Instance.OnConsumableChanged += Refresh;
    }

    private void OnDestroy()
    {
        // ��󣬷�ֹ����ж�غ�ص�������
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= Refresh;
        if (ConsumableManager.Instance != null)
            ConsumableManager.Instance.OnConsumableChanged -= Refresh;
    }

    /// <summary>
    /// ˢ�������ı��ֶΡ�
    /// </summary>
    private void Refresh()
    {
        if (quotaText != null)
            quotaText.text = $"�� {CurrencyManager.Instance.QuotaStar}";
        if (totalText != null)
            totalText.text = $"�� {CurrencyManager.Instance.TotalStar}";
        if (filmText != null)
            filmText.text = $"Film: {ConsumableManager.Instance.Film}";
        if (foodText != null)
            foodText.text = $"Food: {ConsumableManager.Instance.Food}";
    }
}
