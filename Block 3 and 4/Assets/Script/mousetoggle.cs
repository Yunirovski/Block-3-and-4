using UnityEngine;

public class MouseToggle : MonoBehaviour
{
    private bool isCursorLocked = true;

    void Start()
    {
        LockCursor();  // Lock and hide the cursor at the start of the game
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isCursorLocked)
                UnlockCursor();  // Show and unlock the cursor
            else
                LockCursor();    // Hide and lock the cursor
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // Lock cursor to the center of the screen
        Cursor.visible = false;                   // Hide the cursor
        isCursorLocked = true;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;   // Unlock the cursor
        Cursor.visible = true;                    // Show the cursor
        isCursorLocked = false;
    }
}
