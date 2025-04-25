using UnityEngine;

public enum ShopItemType { Consumable, Equipment }

[CreateAssetMenu(menuName = "Shop/ItemData")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public ShopItemType type;
    public int priceQuota;          // ��������
    public int unlockNeedTotal;     // ��Ҫ�����ղءConsumable ���� 0��
    public BaseItem linkedItem;     // ���� Equipment����ָ���Ӧ ScriptableObject
    public int amount = 1;          // ���� Consumable������������
}
