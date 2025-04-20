using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class player_move2 : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 10f;      // Walk speed
    public float runSpeed = 20f;       // Run speed
    public float crouchSpeed = 2f;     // Crouch speed
    public float jumpHeight = 4f;      // Jump height
    public float gravity = -12f;       // Gravity force

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;    // Mouse look speed
    public float verticalLookLimit = 80f;  // Look up/down limit
    public Transform cameraTransform;      // Camera to rotate

    [Header("Crouch Settings (test)")]
    public float crouchHeight = 1f;        // Height when crouching
    public float normalHeight = 2f;        // Normal height
    public float crouchTransitionSpeed = 5f; // Crouch speed (smooth)

    private CharacterController controller;
    private Vector3 velocity;              // Y axis speed
    private float verticalRotation = 0f;   // Camera up/down rotation
    private bool isCrouching = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // Lock mouse
        Cursor.visible = false;
    }

    void Update()
    {
        LookAround();   // Mouse look
        MovePlayer();   // Move with WASD
        HandleJump();   // Jump with Space
    }

    void MovePlayer()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        bool isRunning = Input.GetButton("Fire3"); // Shift
        float currentSpeed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move *= currentSpeed;

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = move + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }

    void HandleJump()
    {
        if (controller.isGrounded && Input.GetButtonDown("Jump"))
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void LookAround()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
