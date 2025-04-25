using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 商店系统：按 B 打开，暂停游戏，列出 catalog 中的物品，
/// 支持购买 Consumable（胶卷/食物）与 Equipment（注册到槽3循环）。
/// 在销毁时解绑 CurrencyManager 的回调。
/// </summary>
public class ShopManager : MonoBehaviour
{
    // —— 单例实例 —— 
    public static ShopManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject shopRoot;        // ShopPanel 根，Start 时保持 inactive
    public Transform contentHolder;    // ScrollView/Viewport/Content
    public GameObject itemEntryPrefab; // Button+Text 的预制
    public TMP_Text quotaText;         // 显示当前配额★

    [Header("Catalog Data")]
    public ShopItemData[] catalog;     // 在 Inspector 中填入多个 ShopItemData

    private void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        BuildUI();
        UpdateQuotaLabel();

        // 订阅 CurrencyManager 回调
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += UpdateQuotaLabel;
    }

    private void OnDestroy()
    {
        // 解绑事件
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateQuotaLabel;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            ToggleShop();
    }

    /// <summary>
    /// 构建滚动列表的按钮条目，只执行一次。
    /// </summary>
    private void BuildUI()
    {
        foreach (var data in catalog)
        {
            GameObject go = Instantiate(itemEntryPrefab, contentHolder);
            var entry = go.GetComponent<ShopEntryUI>();
            entry.Init(data);
        }
    }

    /// <summary>
    /// 打开/关闭商店面板，并暂停/恢复游戏时间与鼠标状态。
    /// 打开时刷新所有条目的状态。
    /// </summary>
    private void ToggleShop()
    {
        bool active = !shopRoot.activeSelf;
        shopRoot.SetActive(active);
        Time.timeScale = active ? 0f : 1f;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;

        if (active)
        {
            // 打开时逐条刷新可购买状态
            foreach (Transform t in contentHolder)
            {
                var entry = t.GetComponent<ShopEntryUI>();
                if (entry != null)
                    entry.Refresh();
            }
        }
    }

    /// <summary>
    /// 更新顶部配额★ 文本显示。
    /// </summary>
    private void UpdateQuotaLabel()
    {
        if (quotaText != null)
            quotaText.text = $"Quota★: {CurrencyManager.Instance.QuotaStar}";
    }

    /// <summary>
    /// 供 ShopEntryUI 调用的购买尝试逻辑。
    /// </summary>
    public void TryBuy(ShopItemData data)
    {
        if (CurrencyManager.Instance.TotalStar < data.unlockNeedTotal)
        {
            Debug.Log($"未满足收藏★需求 ({data.unlockNeedTotal})");
            return;
        }
        if (!CurrencyManager.Instance.TrySpendStars(data.priceQuota))
        {
            Debug.Log("配额★不足！");
            return;
        }

        switch (data.type)
        {
            case ShopItemType.Consumable:
                if (data.itemName.Contains("Film"))
                    ConsumableManager.Instance.AddFilm(data.amount);
                else
                    ConsumableManager.Instance.AddFood(data.amount);
                break;

            case ShopItemType.Equipment:
                InventoryCycler.RegisterItem(data.linkedItem);
                Debug.Log($"已购买装备 {data.itemName}，可用 Q/E 切换！");
                break;
        }
    }
}
