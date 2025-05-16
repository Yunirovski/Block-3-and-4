// Assets/Scripts/UI/ResourceHUD.cs
using UnityEngine;
using TMPro;

/// <summary>
/// 持久化资源HUD，显示在屏幕右上角：
/// - 可用星星 (Quota★)
/// - 总收集星星 (Total★)
/// - 可用胶卷数量
/// - 可用食物数量
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    [Header("UI Text References")]
    [Tooltip("显示当前可用星星的文本 (Quota★)")]
    [SerializeField] private TMP_Text quotaText;

    [Tooltip("显示总收集星星的文本 (Σ★)")]
    [SerializeField] private TMP_Text totalText;

    [Tooltip("显示当前胶卷数量的文本")]
    [SerializeField] private TMP_Text filmText;

    [Tooltip("显示当前食物数量的文本")]
    [SerializeField] private TMP_Text foodText;

    [Header("自定义显示")]
    [Tooltip("是否在食物数量旁显示具体食物类型")]
    public bool showFoodTypes = true;

    private void Start()
    {
        // 执行所有HUD字段的初始更新
        Refresh();

        // 注册到UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterResourceHUD(this);
        }

        // 订阅货币变化事件
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += Refresh;
        }

        // 订阅消耗品变化事件
        if (ConsumableManager.Instance != null)
        {
            ConsumableManager.Instance.OnConsumableChanged += Refresh;
            ConsumableManager.Instance.OnFoodChanged += UpdateSpecificFood;
        }
    }

    private void OnDestroy()
    {
        // 从UIManager注销
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterResourceHUD(this);
        }

        // 取消订阅货币变化事件，避免场景卸载后的回调
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= Refresh;
        }

        // 取消订阅消耗品变化事件，防止空引用异常
        if (ConsumableManager.Instance != null)
        {
            ConsumableManager.Instance.OnConsumableChanged -= Refresh;
            ConsumableManager.Instance.OnFoodChanged -= UpdateSpecificFood;
        }
    }

    /// <summary>
    /// 根据管理器的最新值更新所有HUD文本字段
    /// </summary>
    public void Refresh()
    {
        // 更新可用星星 (Quota★)
        if (quotaText != null && CurrencyManager.Instance != null)
        {
            quotaText.text = $"★ {CurrencyManager.Instance.QuotaStar}";
        }

        // 更新总收集星星 (Σ★)
        if (totalText != null && CurrencyManager.Instance != null)
        {
            totalText.text = $"Σ {CurrencyManager.Instance.TotalStar}";
        }
        else if (totalText != null && ProgressionManager.Instance != null)
        {
            // 如果没有CurrencyManager，则尝试从ProgressionManager获取
            totalText.text = $"Σ {ProgressionManager.Instance.TotalStars}";
        }

        // 更新胶卷数量
        if (filmText != null && ConsumableManager.Instance != null)
        {
            filmText.text = $"胶卷: {ConsumableManager.Instance.Film}";
        }

        // 更新食物总数量
        if (foodText != null && ConsumableManager.Instance != null && !showFoodTypes)
        {
            foodText.text = $"食物: {ConsumableManager.Instance.Food}";
        }
        else if (foodText != null && ConsumableManager.Instance != null && showFoodTypes)
        {
            // 如果启用显示食物类型，这里只显示标题，具体数量由UpdateSpecificFood更新
            foodText.text = "食物:";
            UpdateAllFoodTypes();
        }
    }

    /// <summary>
    /// 更新特定食物类型的显示
    /// </summary>
    private void UpdateSpecificFood(FoodType type, int count, int max)
    {
        if (!showFoodTypes || foodText == null) return;

        // 这里我们假设foodText是一个较大的文本区域，可以显示多行
        // 为每种食物类型构建显示
        UpdateAllFoodTypes();
    }

    /// <summary>
    /// 更新所有食物类型的显示
    /// </summary>
    private void UpdateAllFoodTypes()
    {
        if (!showFoodTypes || foodText == null || ConsumableManager.Instance == null) return;

        // 构建食物类型显示字符串
        string foodDisplay = "食物:\n";

        // 获取各类食物数量
        if (ConsumableManager.Instance != null)
        {
            int foodCap = ConsumableManager.Instance.foodCap;

            // 使用扩展方法获取各类食物数量
            int meatCount = ConsumableManager.Instance.GetFoodCount(FoodType.Meat);
            int leavesCount = ConsumableManager.Instance.GetFoodCount(FoodType.Leaves);
            int fruitCount = ConsumableManager.Instance.GetFoodCount(FoodType.Fruit);

            // 如果获取失败（返回-1），则使用总量
            if (meatCount < 0 || leavesCount < 0 || fruitCount < 0)
            {
                foodDisplay = $"食物: {ConsumableManager.Instance.Food}";
                foodText.text = foodDisplay;
                return;
            }

            // 构建显示字符串
            foodDisplay += $"  肉类: {meatCount}/{foodCap}\n";
            foodDisplay += $"  草叶: {leavesCount}/{foodCap}\n";
            foodDisplay += $"  水果: {fruitCount}/{foodCap}";
        }

        foodText.text = foodDisplay;
    }

    /// <summary>
    /// 更新指定文本组件的内容
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