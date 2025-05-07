// Assets/Scripts/Items/BaseItem.cs
using UnityEngine;

/// <summary>所有道具 ScriptableObject 的基类。</summary>
public abstract class BaseItem : ScriptableObject
{
    [Tooltip("显示名称，也作为唯一 ID")] public string itemName = "New Item";

    /* ───── 手持模型 ───── */
    [Header("Model")]
    [Tooltip("若留空，将用默认立方体占位")]
    public GameObject modelPrefab;          // ← 可拖具体 FBX / Prefab

    [Header("Hold Offset (局部)")]
    public Vector3 holdPosition;
    public Vector3 holdRotation;

    /// <summary>由 InventorySystem 在实例化后调用。</summary>
    public virtual void ApplyHoldTransform(Transform tf)
    {
        tf.localPosition = holdPosition;
        tf.localRotation = Quaternion.Euler(holdRotation);
    }

    /* ───── 可选回调 ───── */
    public virtual void OnSelect(GameObject model) { }
    public virtual void OnDeselect() { }
    public virtual void OnReady() { }
    public virtual void OnUnready() { }
    public virtual void OnUse() { }
}
