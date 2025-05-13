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

    [Header("Pause Targets")]
    [Tooltip("日志打开时要临时禁用的脚本（玩家移动、道具系统等）")]
    public List<Behaviour> scriptsToDisable = new();

    // ────────── 内部状态 ──────────
    int currentTab = 0;          // 0-3
    bool isLogOpen = false;
    float cachedTimeScale = 1f;  // 记住打开前的 Time.timeScale

    /* ===================================================================== */
    /*                              生命周期                                 */
    /* ===================================================================== */

    void Start()
    {
        if (logCanvas != null) logCanvas.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isLogOpen) CloseLog();
            else OpenLog();
        }
    }

    /* ===================================================================== */
    /*                           打开 / 关闭 日志                            */
    /* ===================================================================== */

    void OpenLog()
    {
        if (logCanvas == null) { Debug.LogError("LogController: logCanvas 未赋值"); return; }

        isLogOpen = true;
        logCanvas.enabled = true;
        ShowTab(currentTab);

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

    /* ===================================================================== */
    /*                             标签页切换                                */
    /* ===================================================================== */

    public void ShowTab(int idx)
    {
        currentTab = Mathf.Clamp(idx, 0, 3);

        if (tutorialTab) tutorialTab.SetActive(idx == 0);
        if (polarTab) polarTab.SetActive(idx == 1);
        if (savannaTab) savannaTab.SetActive(idx == 2);
        if (jungleTab) jungleTab.SetActive(idx == 3);
    }
}
