// Assets/Scripts/UI/PopupController.cs
using UnityEngine;
using TMPro;

/// <summary>
/// 极简弹窗：在实例化后调用 Show(msg)，显示文本并在 lifetime 秒后自毁。<br/>
/// 如果你没有 UI 需求，保留脚本即可满足编译；Inspector 中可不指定 prefab。
/// </summary>
public class PopupController : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float lifetime = 3f;

    private void Awake()
    {
        Debug.Log($"PopupController: 弹窗创建 - {gameObject.name}");
    }

    /// <summary>显示一条信息；可多次调用重复改文本。</summary>
    public void Show(string msg)
    {
        Debug.Log($"PopupController: 显示消息 - \"{msg}\"");

        if (messageText != null)
            messageText.text = msg;

        // 若没有 Text 组件，也保持沉默
        CancelInvoke(nameof(AutoDestroy));
        Invoke(nameof(AutoDestroy), lifetime);
    }

    private void AutoDestroy()
    {
        Debug.Log($"PopupController: 自动销毁 - {gameObject.name}");
        // 弹窗实例可以销毁，因为它是临时的，不影响主游戏对象
        Destroy(gameObject);
    }
}