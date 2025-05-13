// Assets/Scripts/Log/LogController.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 日志（J 键）开关与分页控制：
/// • 打开时暂停游戏 TimeScale = 0、解锁鼠标、禁用指定脚本  
/// • 关闭时恢复原 TimeScale、重新锁鼠标、重新启用脚本
/// </summary>
public class LogController : MonoBehaviour
{
    [Header("Canvas & Tabs")]
    [Tooltip("整本日志用的 Canvas")]
    public Canvas logCanvas;

    [Tooltip("Tutorial / Polar / Savanna / Jungle 四个标签页根节点")]
    public GameObject tutorialTab;
    public GameObject polarTab;
    public GameObject savannaTab;
    public GameObject jungleTab;

    [Header("Photo UI Components")]
    [Tooltip("各区域的PhotoLogUI组件引用")]
    public PhotoLogUI tutorialLogUI;
    public PhotoLogUI polarLogUI;
    public PhotoLogUI savannaLogUI;
    public PhotoLogUI jungleLogUI;

    [Header("Pause Targets")]
    [Tooltip("日志打开时要临时禁用的脚本（玩家移动、道具系统等）")]
    public List<Behaviour> scriptsToDisable = new();

    // ────────── 内部状态 ──────────
    int currentTab = 0;          // 0-3
    bool isLogOpen = false;
    float cachedTimeScale = 1f;  // 记住打开前的 Time.timeScale

    // 修改LogController.cs的Start方法，确保设置正确的regionKey
    void Start()
    {
        if (logCanvas != null) logCanvas.enabled = false;

        // 找到PhotoLogUI组件（如果尚未赋值）
        if (tutorialLogUI == null && tutorialTab != null)
            tutorialLogUI = tutorialTab.GetComponent<PhotoLogUI>();

        if (polarLogUI == null && polarTab != null)
            polarLogUI = polarTab.GetComponent<PhotoLogUI>();

        if (savannaLogUI == null && savannaTab != null)
            savannaLogUI = savannaTab.GetComponent<PhotoLogUI>();

        if (jungleLogUI == null && jungleTab != null)
            jungleLogUI = jungleTab.GetComponent<PhotoLogUI>();

        // 确保各PhotoLogUI设置了正确的regionKey
        if (tutorialLogUI != null) tutorialLogUI.regionKey = "tutorial";
        if (polarLogUI != null) polarLogUI.regionKey = "polar";
        if (savannaLogUI != null) savannaLogUI.regionKey = "savanna";
        if (jungleLogUI != null) jungleLogUI.regionKey = "jungle";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isLogOpen) CloseLog();
            else OpenLog();
        }
    }

    void OpenLog()
    {
        if (logCanvas == null) { Debug.LogError("LogController: logCanvas 未赋值"); return; }

        isLogOpen = true;
        logCanvas.enabled = true;
        ShowTab(currentTab);

        // — 刷新当前页面的照片显示 —
        RefreshCurrentTab();

        // — 暂停时间 —
        cachedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // — 解锁鼠标 —
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // — 禁用脚本 —
        foreach (var b in scriptsToDisable)
            if (b != null) b.enabled = false;
    }

    void CloseLog()
    {
        if (logCanvas == null) return;

        isLogOpen = false;
        logCanvas.enabled = false;

        // — 恢复时间 —
        Time.timeScale = cachedTimeScale;

        // — 重新锁鼠标 —
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // — 重新启用脚本 —
        foreach (var b in scriptsToDisable)
            if (b != null) b.enabled = true;
    }

    public void ShowTab(int idx)
    {
        currentTab = Mathf.Clamp(idx, 0, 3);

        if (tutorialTab) tutorialTab.SetActive(idx == 0);
        if (polarTab) polarTab.SetActive(idx == 1);
        if (savannaTab) savannaTab.SetActive(idx == 2);
        if (jungleTab) jungleTab.SetActive(idx == 3);

        // 刷新当前页面的照片显示
        RefreshCurrentTab();
    }

    /// <summary>
    /// 刷新当前标签页的照片显示
    /// </summary>
    private void RefreshCurrentTab()
    {
        switch (currentTab)
        {
            case 0:
                if (tutorialLogUI != null) tutorialLogUI.RefreshDisplay();
                break;
            case 1:
                if (polarLogUI != null) polarLogUI.RefreshDisplay();
                break;
            case 2:
                if (savannaLogUI != null) savannaLogUI.RefreshDisplay();
                break;
            case 3:
                if (jungleLogUI != null) jungleLogUI.RefreshDisplay();
                break;
        }
    }
}