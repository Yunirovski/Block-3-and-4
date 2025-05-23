// 修改后的 PauseAndMouseManager.cs
using UnityEngine;

public class PauseAndMouseManager : MonoBehaviour
{
    [Tooltip("Pause menu UI panel (optional)")]
    public GameObject pauseMenuUI;

    private bool isPaused = false;
    private bool isCursorLocked = true;
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
            // 检查是否在相机模式 - 如果在相机模式，不允许暂停
            if (UIManager.Instance != null && UIManager.Instance.IsCameraMode())
            {
                Debug.Log("相机模式下不允许暂停，请先退出相机模式");
                // 可选：显示提示信息
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateCameraDebugText("相机模式下不能暂停，按Q或右键退出相机模式");
                }
                return; // 直接返回，不执行暂停逻辑
            }

            // 只有不在相机模式时才允许暂停/恢复
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private void HandleMouseToggleInput()
    {
        if (isPaused)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isCursorLocked)
                UnlockCursor();
            else
                LockCursor();
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        UnlockCursor();
        isPaused = true;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        if (playerMove != null)
            playerMove.enabled = false;

        Debug.Log("游戏已暂停");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        LockCursor();
        isPaused = false;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (playerMove != null)
            playerMove.enabled = true;

        Debug.Log("游戏已恢复");
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
}
