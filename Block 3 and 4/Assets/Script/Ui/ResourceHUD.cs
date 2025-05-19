// Assets/Scripts/UI/ResourceHUD.cs
using UnityEngine;
using TMPro;

/// <summary>
/// �־û���ԴHUD����ʾ����Ļ���Ͻǣ�
/// - �������� (Quota��)
/// - ���ռ����� (Total��)
/// - ���ý�������
/// - ����ʳ������
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    [Header("UI Text References")]
    [Tooltip("��ʾ��ǰ�������ǵ��ı� (Quota��)")]
    [SerializeField] private TMP_Text quotaText;

    [Tooltip("��ʾ���ռ����ǵ��ı� (����)")]
    [SerializeField] private TMP_Text totalText;

    [Tooltip("��ʾ��ǰ�����������ı�")]
    [SerializeField] private TMP_Text filmText;

    [Tooltip("��ʾ��ǰʳ���������ı�")]
    [SerializeField] private TMP_Text foodText;

    [Header("�Զ�����ʾ")]
    [Tooltip("�Ƿ���ʳ����������ʾ����ʳ������")]
    public bool showFoodTypes = true;

    private void Start()
    {
        // ִ������HUD�ֶεĳ�ʼ����
        Refresh();

        // ע�ᵽUIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterResourceHUD(this);
        }

        // ���Ļ��ұ仯�¼�
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += Refresh;
        }

        // ��������Ʒ�仯�¼�
        if (ConsumableManager.Instance != null)
        {
            ConsumableManager.Instance.OnConsumableChanged += Refresh;
            ConsumableManager.Instance.OnFoodChanged += UpdateSpecificFood;
        }
    }

    private void OnDestroy()
    {
        // ��UIManagerע��
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterResourceHUD(this);
        }

        // ȡ�����Ļ��ұ仯�¼������ⳡ��ж�غ�Ļص�
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= Refresh;
        }

        // ȡ����������Ʒ�仯�¼�����ֹ�������쳣
        if (ConsumableManager.Instance != null)
        {
            ConsumableManager.Instance.OnConsumableChanged -= Refresh;
            ConsumableManager.Instance.OnFoodChanged -= UpdateSpecificFood;
        }
    }

    /// <summary>
    /// ���ݹ�����������ֵ��������HUD�ı��ֶ�
    /// </summary>
    public void Refresh()
    {
        // ���¿������� (Quota��)
        if (quotaText != null && CurrencyManager.Instance != null)
        {
            quotaText.text = $"�� {CurrencyManager.Instance.QuotaStar}";
        }

        // �������ռ����� (����)
        if (totalText != null && CurrencyManager.Instance != null)
        {
            totalText.text = $"�� {CurrencyManager.Instance.TotalStar}";
        }
        else if (totalText != null && ProgressionManager.Instance != null)
        {
            // ���û��CurrencyManager�����Դ�ProgressionManager��ȡ
            totalText.text = $"�� {ProgressionManager.Instance.TotalStars}";
        }

        // ���½�������
        if (filmText != null && ConsumableManager.Instance != null)
        {
            filmText.text = $"����: {ConsumableManager.Instance.Film}";
        }

        // ����ʳ��������
        if (foodText != null && ConsumableManager.Instance != null && !showFoodTypes)
        {
            foodText.text = $"ʳ��: {ConsumableManager.Instance.Food}";
        }
        else if (foodText != null && ConsumableManager.Instance != null && showFoodTypes)
        {
            // ���������ʾʳ�����ͣ�����ֻ��ʾ���⣬����������UpdateSpecificFood����
            foodText.text = "ʳ��:";
            UpdateAllFoodTypes();
        }
    }

    /// <summary>
    /// �����ض�ʳ�����͵���ʾ
    /// </summary>
    private void UpdateSpecificFood(FoodType type, int count, int max)
    {
        if (!showFoodTypes || foodText == null) return;

        // �������Ǽ���foodText��һ���ϴ���ı����򣬿�����ʾ����
        // Ϊÿ��ʳ�����͹�����ʾ
        UpdateAllFoodTypes();
    }

    /// <summary>
    /// ��������ʳ�����͵���ʾ
    /// </summary>
    private void UpdateAllFoodTypes()
    {
        if (!showFoodTypes || foodText == null || ConsumableManager.Instance == null) return;

        // ����ʳ��������ʾ�ַ���
        string foodDisplay = "ʳ��:\n";

        // ��ȡ����ʳ������
        if (ConsumableManager.Instance != null)
        {
            int foodCap = ConsumableManager.Instance.foodCap;

            // ʹ����չ������ȡ����ʳ������
            int meatCount = ConsumableManager.Instance.GetFoodCount(FoodType.Meat);
            int leavesCount = ConsumableManager.Instance.GetFoodCount(FoodType.Leaves);
            int fruitCount = ConsumableManager.Instance.GetFoodCount(FoodType.Fruit);

            // �����ȡʧ�ܣ�����-1������ʹ������
            if (meatCount < 0 || leavesCount < 0 || fruitCount < 0)
            {
                foodDisplay = $"ʳ��: {ConsumableManager.Instance.Food}";
                foodText.text = foodDisplay;
                return;
            }

            // ������ʾ�ַ���
            foodDisplay += $"  ����: {meatCount}/{foodCap}\n";
            foodDisplay += $"  ��Ҷ: {leavesCount}/{foodCap}\n";
            foodDisplay += $"  ˮ��: {fruitCount}/{foodCap}";
        }

        foodText.text = foodDisplay;
    }

    /// <summary>
    /// ����ָ���ı����������
    /// </summary>
    public void UpdateText(string type, string value)
    {
        switch (type.ToLower())
        {
            case "quota":
            case "star":
            case "stars":
                if (quotaText != null) quotaText.text = value;
                break;
            case "total":
                if (totalText != null) totalText.text = value;
                break;
            case "film":
            case "films":
                if (filmText != null) filmText.text = value;
                break;
            case "food":
            case "foods":
                if (foodText != null) foodText.text = value;
                break;
        }
    }
}