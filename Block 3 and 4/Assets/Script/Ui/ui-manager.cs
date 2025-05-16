// Assets/Scripts/UI/UIManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 中央UI管理器：统一控制所有界面元素与交互
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("画布引用")]
    [Tooltip("主界面画布")]
    public Canvas mainHUDCanvas;

    [Tooltip("相机模式画布")]
    public Canvas cameraHUDCanvas;

    [Header("物品栏UI")]
    [Tooltip("圆形物品选择UI")]
    public RadialInventoryUI radialInventoryUI;

    [Tooltip("资源显示HUD")]
    public ResourceHUD resourceHUD;

    [Header("相机UI")]
    [Tooltip("相机模式调试文本")]
    public TMP_Text cameraDebugText;

    [Tooltip("相机模式结果文本")]
    public TMP_Text cameraResultText;

    [Header("弹窗预制体")]
    [Tooltip("简单弹窗预制体")]
    public PopupController popupPrefab;

    [Tooltip("确认对话框预制体")]
    public ConfirmationDialog confirmationDialogPrefab;

    [Tooltip("照片已满警告预制体")]
    public FullPageAlertPopup fullPageAlertPopupPrefab;

    [Tooltip("照片替换选择器预制体")]
    public ReplacePhotoSelector replacePhotoSelectorPrefab;

    [Header("物品冷却UI")]
    [Tooltip("冷却指示器图像")]
    public Image cooldownIndicator;

    [Tooltip("冷却文本")]
    public TMP_Text cooldownText;

    // UI状态标记
    private bool isInventoryOpen = false;
    private bool isCameraMode = false;

    // 物品冷却计时器
    private float currentCooldownTime = 0f;
    private float maxCooldownTime = 0f;
    private bool isCooldownActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始化UI状态
        if (cooldownIndicator != null)
            cooldownIndicator.gameObject.SetActive(false);

        if (cameraHUDCanvas != null)
            cameraHUDCanvas.enabled = false;
    }

    private void Update()
    {
        // 更新物品冷却UI
        if (isCooldownActive)
        {
            currentCooldownTime -= Time.deltaTime;

            if (currentCooldownTime <= 0)
            {
                // 冷却结束
                isCooldownActive = false;
                if (cooldownIndicator != null)
                    cooldownIndicator.gameObject.SetActive(false);
                if (cooldownText != null)
                    cooldownText.text = "";
            }
            else
            {
                // 更新冷却显示
                if (cooldownIndicator != null)
                {
                    cooldownIndicator.fillAmount = currentCooldownTime / maxCooldownTime;
                }

                if (cooldownText != null)
                {
                    cooldownText.text = currentCooldownTime.ToString("F1") + "s";
                }
            }
        }
    }

    #region 通用UI控制

    /// <summary>
    /// 显示或隐藏主HUD
    /// </summary>
    public void SetMainHUDVisible(bool visible)
    {
        if (mainHUDCanvas != null)
            mainHUDCanvas.enabled = visible;
    }

    /// <summary>
    /// 显示或隐藏相机HUD
    /// </summary>
    public void SetCameraHUDVisible(bool visible)
    {
        if (cameraHUDCanvas != null)
            cameraHUDCanvas.enabled = visible;
    }

    #endregion

    #region 物品栏UI

    /// <summary>
    /// 显示圆形物品选择UI
    /// </summary>
    public void ShowInventoryRadial(bool[] unlockedSlots, int currentSlot)
    {
        if (radialInventoryUI != null)
        {
            radialInventoryUI.SetUnlockedStates(unlockedSlots, currentSlot);
            radialInventoryUI.Show();
            isInventoryOpen = true;
        }
    }

    /// <summary>
    /// 隐藏圆形物品选择UI
    /// </summary>
    public void HideInventoryRadial()
    {
        if (radialInventoryUI != null)
        {
            radialInventoryUI.Hide();
            isInventoryOpen = false;
        }
    }

    /// <summary>
    /// 获取当前选中的物品栏索引
    /// </summary>
    public int GetSelectedInventorySlot()
    {
        return radialInventoryUI != null ? radialInventoryUI.CurrentIndex : -1;
    }

    /// <summary>
    /// 使用鼠标滚轮在物品栏中循环选择
    /// </summary>
    public void StepInventorySelection(int direction)
    {
        if (radialInventoryUI != null)
        {
            radialInventoryUI.Step(direction);
        }
    }

    /// <summary>
    /// 开始物品冷却显示
    /// </summary>
    public void StartItemCooldown(BaseItem item, float duration)
    {
        isCooldownActive = true;
        currentCooldownTime = duration;
        maxCooldownTime = duration;

        if (cooldownIndicator != null)
        {
            cooldownIndicator.gameObject.SetActive(true);
            cooldownIndicator.fillAmount = 1.0f;
        }

        if (cooldownText != null)
        {
            cooldownText.text = duration.ToString("F1") + "s";
        }
    }

    #endregion

    #region 资源HUD管理

    // 资源HUD引用
    private ResourceHUD resourceHUDInstance;

    /// <summary>
    /// 注册ResourceHUD实例
    /// </summary>
    public void RegisterResourceHUD(ResourceHUD hud)
    {
        resourceHUDInstance = hud;
        resourceHUD = hud;  // 更新Inspector引用
    }

    /// <summary>
    /// 注销ResourceHUD实例
    /// </summary>
    public void UnregisterResourceHUD(ResourceHUD hud)
    {
        if (resourceHUDInstance == hud)
        {
            resourceHUDInstance = null;
        }
    }

    /// <summary>
    /// 刷新资源HUD显示
    /// </summary>
    public void RefreshResourceHUD()
    {
        if (resourceHUDInstance != null)
        {
            resourceHUDInstance.Refresh();
        }
    }

    /// <summary>
    /// 更新特定资源文本
    /// </summary>
    public void UpdateResourceText(string type, string value)
    {
        if (resourceHUDInstance != null)
        {
            resourceHUDInstance.UpdateText(type, value);
        }
    }

    #endregion

    #region 相机UI

    /// <summary>
    /// 进入相机模式
    /// </summary>
    public void EnterCameraMode()
    {
        isCameraMode = true;
        SetMainHUDVisible(false);
        SetCameraHUDVisible(true);
        UpdateCameraDebugText("相机已开启");
        UpdateCameraResultText("");
    }

    /// <summary>
    /// 退出相机模式
    /// </summary>
    public void ExitCameraMode()
    {
        if (!isCameraMode) return;

        isCameraMode = false;
        SetCameraHUDVisible(false);
        SetMainHUDVisible(true);
        UpdateCameraDebugText("相机已关闭");
    }

    /// <summary>
    /// 更新相机调试文本
    /// </summary>
    public void UpdateCameraDebugText(string message)
    {
        if (cameraDebugText != null)
            cameraDebugText.text = message;
    }

    /// <summary>
    /// 更新相机结果文本
    /// </summary>
    public void UpdateCameraResultText(string message)
    {
        if (cameraResultText != null)
            cameraResultText.text = message;
    }

    /// <summary>
    /// 检查是否处于相机模式
    /// </summary>
    public bool IsCameraMode()
    {
        return isCameraMode;
    }

    #endregion

    #region 弹窗系统

    /// <summary>
    /// 显示简单弹窗
    /// </summary>
    public PopupController ShowPopup(string message)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("弹窗预制体未设置");
            return null;
        }

        PopupController popup = Instantiate(popupPrefab);
        popup.Show(message);
        return popup;
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public ConfirmationDialog ShowConfirmation(string title, string message,
                                              Action onConfirm, Action onCancel)
    {
        if (confirmationDialogPrefab == null)
        {
            Debug.LogWarning("确认对话框预制体未设置");
            return null;
        }

        ConfirmationDialog dialog = Instantiate(confirmationDialogPrefab);
        dialog.Initialize(title, message, onConfirm, onCancel);
        return dialog;
    }

    /// <summary>
    /// 显示照片已满警告
    /// </summary>
    public FullPageAlertPopup ShowPhotoFullAlert(string animalId, string photoPath, int stars)
    {
        if (fullPageAlertPopupPrefab == null)
        {
            Debug.LogWarning("照片已满警告预制体未设置");
            return null;
        }

        FullPageAlertPopup alert = Instantiate(fullPageAlertPopupPrefab);
        alert.Initialize(animalId, photoPath, stars);
        return alert;
    }

    /// <summary>
    /// 显示照片替换选择器
    /// </summary>
    public ReplacePhotoSelector ShowReplaceSelector(string animalId, string photoPath,
                                                   int stars, Action onComplete)
    {
        if (replacePhotoSelectorPrefab == null)
        {
            Debug.LogWarning("照片替换选择器预制体未设置");
            return null;
        }

        ReplacePhotoSelector selector = Instantiate(replacePhotoSelectorPrefab);
        selector.Initialize(animalId, photoPath, stars, onComplete);
        return selector;
    }

    #endregion

    #region 食物UI

    /// <summary>
    /// 更新当前选择的食物类型
    /// </summary>
    public void UpdateFoodTypeText(FoodType foodType)
    {
        if (cameraDebugText != null)
        {
            cameraDebugText.text = $"已选择食物: {foodType}";
        }
    }

    #endregion
}