using UnityEngine;

public class PauseAndMouseManager : MonoBehaviour
{
    [Tooltip("Pause menu UI panel (optional)")]
    public GameObject pauseMenuUI;

    private bool isPaused = false;
    private bool isCursorLocked = true;

    private player_move2 playerMove;   // Player movement script

    void Start()
    {
        LockCursor();  // Lock cursor at start
        playerMove = FindObjectOfType<player_move2>();  // Find player script
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
            playerMove.enabled = false; // Disable player movement
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        LockCursor();
        isPaused = false;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (playerMove != null)
            playerMove.enabled = true; // Enable player movement
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
