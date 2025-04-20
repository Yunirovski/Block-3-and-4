using UnityEngine;

public abstract class BaseItem : ScriptableObject
{
    public string itemName;  // 物品名称，用于显示与标识

    public virtual void OnSelect(GameObject model) { }   // 物品被选中时调用
    public virtual void OnDeselect() { }                 // 物品被取消选中时调用
    public virtual void OnReady() { }                    // 物品准备使用时调用
    public virtual void OnUnready() { }                  // 取消准备使用时调用
    public abstract void OnUse();                        // 使用物品时调用（在准备状态并左键时）
}
