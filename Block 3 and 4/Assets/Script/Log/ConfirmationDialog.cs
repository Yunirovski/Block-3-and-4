using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 通用确认对话框
/// </summary>
public class ConfirmationDialog : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("标题文本")]
    public TMP_Text titleText;

    [Tooltip("消息文本")]
    public TMP_Text messageText;

    [Tooltip("确认按钮")]
    public Button confirmButton;

    [Tooltip("取消按钮")]
    public Button cancelButton;

    // 回调函数
    private System.Action onConfirm;
    private System.Action onCancel;

    /// <summary>
    /// 初始化对话框
    /// </summary>
    public void Initialize(string title, string message, System.Action confirmCallback, System.Action cancelCallback)
    {
        // 设置文本
        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        // 保存回调
        onConfirm = confirmCallback;
        onCancel = cancelCallback;

        // 设置按钮
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    /// <summary>
    /// 确认按钮点击
    /// </summary>
    private void OnConfirmClicked()
    {
        onConfirm?.Invoke();
        Close();
    }

    /// <summary>
    /// 取消按钮点击
    /// </summary>
    private void OnCancelClicked()
    {
        onCancel?.Invoke();
        Close();
    }

    /// <summary>
    /// 关闭对话框
    /// </summary>
    private void Close()
    {
        Destroy(gameObject);
    }
}