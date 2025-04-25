using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject shopRoot;
    public Transform contentHolder;
    public GameObject itemEntryPrefab;
    public TMP_Text quotaText;

    [Header("Catalog")]
    public ShopItemData[] catalog;

    /* ---------- 新增 ---------- */
    void Awake()
    {
        if (shopRoot != null)
            shopRoot.SetActive(false);
    }

    void Start()
    {
        BuildUI();
        UpdateQuotaLabel();
        CurrencyManager.Instance.OnCurrencyChanged += UpdateQuotaLabel;
    }
    /* --------------------------- */


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            ToggleShop();
    }

    void ToggleShop()
    {
        bool active = !shopRoot.activeSelf;
        shopRoot.SetActive(active);
        Time.timeScale = active ? 0f : 1f;   // 暂停/恢复游戏
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;
    }

    void BuildUI()
    {
        foreach (var data in catalog)
        {
            GameObject go = Instantiate(itemEntryPrefab, contentHolder);
            go.GetComponentInChildren<TMP_Text>().text = $"{data.itemName}  - {data.priceQuota}★";
            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => TryBuy(data));
        }
    }

    void TryBuy(ShopItemData data)
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

    void UpdateQuotaLabel()
    {
        quotaText.text = $"Quota★: {CurrencyManager.Instance.QuotaStar}";
    }
}
