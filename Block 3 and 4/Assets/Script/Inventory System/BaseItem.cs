// Assets/Scripts/Items/BaseItem.cs
using UnityEngine;

/// <summary>
/// 所有道具 ScriptableObject 的基类。  
/// 子类只需重写需要的回调（OnUse / OnReady …）。
/// </summary>
public abstract class BaseItem : ScriptableObject
{
    [Tooltip("显示名称，亦作唯一 ID")]
    public string itemName = "New Item";

    /* ───── 手持偏移 ───── */
    [Header("Hold Offset (在 ItemAnchor 的局部坐标)")]
    public Vector3 holdPosition = Vector3.zero;         // 位置偏移
    public Vector3 holdRotation = Vector3.zero;         // 欧拉角（度）

    /// <summary>由 <see cref="InventorySystem"/> 在实例化模型后调用。</summary>
    public virtual void ApplyHoldTransform(Transform modelTf)
    {
        modelTf.localPosition = holdPosition;
        modelTf.localRotation = Quaternion.Euler(holdRotation);
    }

    /* ────────── 回调接口 ────────── */
    public virtual void OnSelect(GameObject model) { }
    public virtual void OnDeselect() { }
    public virtual void OnReady() { }
    public virtual void OnUnready() { }
    public virtual void OnUse() { }
}
