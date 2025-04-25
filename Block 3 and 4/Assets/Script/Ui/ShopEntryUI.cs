using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopEntryUI : MonoBehaviour
{
    public TMP_Text label;
    public Button buyButton;
    ShopItemData data;

    public void Init(ShopItemData d)
    {
        data = d;
        buyButton.onClick.AddListener(() => ShopManager.Instance.TryBuy(data));
        Refresh();
    }

    public void Refresh()
    {
        bool canAfford = CurrencyManager.Instance.QuotaStar >= data.priceQuota;
        bool unlocked = CurrencyManager.Instance.TotalStar >= data.unlockNeedTotal;
        buyButton.interactable = unlocked && canAfford;
        label.text = unlocked
            ? $"{data.itemName} - {data.priceQuota}°Ô"
            : $"{data.itemName} - Ω‚À¯–Ë¶≤°Ô{data.unlockNeedTotal}";
    }
}
