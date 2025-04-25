using UnityEngine;

public enum ShopItemType { Consumable, Equipment }

[CreateAssetMenu(menuName = "Shop/ItemData")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public ShopItemType type;
    public int priceQuota;          // 花费配额★
    public int unlockNeedTotal;     // 需要的总收藏★（Consumable 可填 0）
    public BaseItem linkedItem;     // 若是 Equipment，则指向对应 ScriptableObject
    public int amount = 1;          // 若是 Consumable，购买获得数量
}
