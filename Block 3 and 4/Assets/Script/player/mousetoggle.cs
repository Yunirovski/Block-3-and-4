// Modified PauseAndMouseManager.cs - Use + key for mouse toggle, work with other systems' auto mouse control
using UnityEngine;

public class PauseAndMouseManager : MonoBehaviour
{
    [Tooltip("Pause menu UI panel (optional)")]
    public GameObject pauseMenuUI;

    [Header("Mouse Toggle Settings")]
    [Tooltip("Use + key to manually toggle mouse visibility")]
    public KeyCode mouseToggleKey = KeyCode.Plus;

    private bool isPaused = false;
    private bool isCursorLocked = true;
    private bool isManualMouseToggle = false; // Track if mouse state is manually toggled

    private player_move2 playerMove;

    void Start()
    {
        LockCursor();
        playerMove = FindObjectOfType<player_move2>();
    }

    void Update()
    {
        HandlePauseInput();
        HandleMouseToggleInput();
    }

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Check if in camera mode - don't allow pause in camera mode
            if (UIManager.Instance != null && UIManager.Instance.IsCameraMode())
            {
                Debug.Log("Cannot pause in camera mode, please exit camera mode first");
                // Optional: Show hint
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateCameraDebugText("Cannot pause in camera mode, press Q or right-click to exit");
                }
                return; // Return directly, don't execute pause logic
            }

            // Only allow pause/resume when not in camera mode
            Debug.Log("ESC pressed, current pause state: " + isPaused);
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private void HandleMouseToggleInput()
    {
        // Don't allow manual mouse toggle when paused
        if (isPaused)
            return;

        // Use + key to manually toggle mouse visibility
        if (Input.GetKeyDown(mouseToggleKey))
        {
            // Toggle manual mouse state
            isManualMouseToggle = !isManualMouseToggle;

            if (isManualMouseToggle)
            {
                UnlockCursor();
                Debug.Log("Manual mouse unlock (Press " + mouseToggleKey + " to lock again)");
            }
            else
            {
                LockCursor();
                Debug.Log("Manual mouse lock");
            }
        }
    }

    public void PauseGame()
    {
        Debug.Log("PauseGame called - Setting timeScale to 0");
        Time.timeScale = 0f;
        UnlockCursor();
        isPaused = true;
        isManualMouseToggle = false; // Reset manual toggle state

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            Debug.Log("Pause menu UI activated");
        }
        else
        {
            Debug.LogWarning("pauseMenuUI is null! Please assign it in inspector");
        }

        if (playerMove != null)
            playerMove.enabled = false;

        Debug.Log("Game paused successfully");
    }

    public void ResumeGame()
    {
        Debug.Log("ResumeGame called - Setting timeScale to 1");
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
            Debug.Log("Pause menu UI deactivated");
        }
        else
        {
            Debug.LogWarning("pauseMenuUI is null! Cannot hide pause menu");
        }

        if (playerMove != null)
            playerMove.enabled = true;

        // When resuming, lock cursor if not manually toggled
        if (!isManualMouseToggle)
        {
            LockCursor();
        }

        Debug.Log("Game resumed successfully");
    }

    /// <summary>
    /// For other systems to call - Auto unlock cursor (journal, tool ring, etc.)
    /// </summary>
    public void AutoUnlockCursor()
    {
        if (!isPaused && !isManualMouseToggle)
        {
            UnlockCursor();
            Debug.Log("System auto unlock cursor");
        }
    }

    /// <summary>
    /// For other systems to call - Auto lock cursor (exit journal, tool ring, etc.)
    /// </summary>
    public void AutoLockCursor()
    {
        if (!isPaused && !isManualMouseToggle)
        {
            LockCursor();
            Debug.Log("System auto lock cursor");
        }
    }

    /// <summary>
    /// Check if currently in manual mouse toggle state
    /// </summary>
    public bool IsManualMouseToggle()
    {
        return isManualMouseToggle;
    }

    /// <summary>
    /// Force reset manual mouse state (for other systems to call when necessary)
    /// </summary>
    public void ResetManualMouseToggle()
    {
        isManualMouseToggle = false;
        if (!isPaused)
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isCursorLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isCursorLocked = false;
    }

    /// <summary>
    /// Get current cursor lock state
    /// </summary>
    public bool IsCursorLocked()
    {
        return isCursorLocked;
    }
}

/*
=== 使用教程（中文） ===

鼠标控制系统说明：

基本功能：
- 按 + 键：手动切换鼠标显示/隐藏状态
- 按 ESC 键：暂停游戏（自动显示鼠标）
- 系统会在进入照片册(J键)、工具环(E键)等界面时自动显示鼠标
- 退出这些界面时自动隐藏鼠标（变回准星）

使用方法：
1. 正常游戏：鼠标默认隐藏，显示准星用于瞄准
2. 临时显示鼠标：按 + 键显示鼠标指针
3. 恢复准星：再按一次 + 键隐藏鼠标，恢复准星
4. 自动控制：
   - 按 J 打开照片册 → 自动显示鼠标
   - 关闭照片册 → 自动隐藏鼠标
   - 按住 E 打开工具环 → 自动显示鼠标
   - 松开 E 关闭工具环 → 自动隐藏鼠标
   - 按 ESC 暂停 → 自动显示鼠标
   - 恢复游戏 → 自动隐藏鼠标

重要提示：
- 当你手动按 + 键切换鼠标状态后，系统不会自动改变鼠标状态
- 如果想恢复自动控制，再按一次 + 键即可
- 在相机模式下无法暂停游戏，需要先退出相机模式

=== Integration Guide for Other Systems ===

1. For PhotoBookController.cs (照片册控制器):

在 OpenBook() 方法中添加：
    var mouseManager = FindObjectOfType<PauseAndMouseManager>();
    if (mouseManager != null)
        mouseManager.AutoUnlockCursor();
    else
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

在 CloseBook() 方法中添加：
    var mouseManager = FindObjectOfType<PauseAndMouseManager>();
    if (mouseManager != null)
        mouseManager.AutoLockCursor();
    else
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

2. For InventorySystem.cs (物品栏系统):

在 HandleRing() 方法中修改：
    if (Input.GetKeyDown(KeyCode.E))
    {
        ringOpen = true;
        
        var mouseManager = FindObjectOfType<PauseAndMouseManager>();
        if (mouseManager != null)
            mouseManager.AutoUnlockCursor();
            
        UIManager.Instance.ShowInventoryRadial(BuildUnlockArray(), currentIndex);
    }
    else if (Input.GetKeyUp(KeyCode.E))
    {
        ringOpen = false;
        
        var mouseManager = FindObjectOfType<PauseAndMouseManager>();
        if (mouseManager != null)
            mouseManager.AutoLockCursor();
            
        UIManager.Instance.HideInventoryRadial();
        // ... rest of the code
    }

3. For any other UI system that needs mouse control:

Opening UI:
    var mouseManager = FindObjectOfType<PauseAndMouseManager>();
    if (mouseManager != null)
        mouseManager.AutoUnlockCursor();

Closing UI:
    var mouseManager = FindObjectOfType<PauseAndMouseManager>();
    if (mouseManager != null)
        mouseManager.AutoLockCursor();

*/