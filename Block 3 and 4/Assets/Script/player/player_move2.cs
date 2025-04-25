using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class player_move2 : MonoBehaviour, IMoveController
{
    [Header("Movement Settings")]
    public float walkSpeed = 10f;      // Walk speed
    public float runSpeed = 20f;       // Run speed
    public float crouchSpeed = 2f;     // Crouch movement speed
    public float jumpHeight = 4f;      // Jump height
    public float gravity = -12f;       // Gravity force

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;    // Mouse look sensitivity
    public float verticalLookLimit = 80f;  // Up/down look limit
    public Transform cameraTransform;      // Reference to the camera

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;        // CharacterController height when crouching
    public float normalHeight = 2f;        // Height when standing
    public float crouchTransitionSpeed = 5f;

    // Internal state
    private CharacterController controller;
    private Vector3 velocity;              // Vertical velocity for gravity/jump
    private float verticalRotation = 0f;   // Camera pitch
    private bool isCrouching = false;

    // For IMoveController
    private float baseWalkSpeed;
    private float baseRunSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Store base speeds for ModifySpeed
        baseWalkSpeed = walkSpeed;
        baseRunSpeed = runSpeed;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleCrouchToggle();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate character horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");
        bool isRunning = Input.GetButton("Fire3"); // Shift
        float speed = isCrouching
            ? crouchSpeed
            : (isRunning ? runSpeed : walkSpeed);

        Vector3 move = (transform.right * inputX + transform.forward * inputZ) * speed;

        // Gravity
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        move.y = velocity.y;

        controller.Move(move * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (controller.isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void HandleCrouchToggle()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
            isCrouching = !isCrouching;

        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        controller.height = Mathf.MoveTowards(
            controller.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime);

        // Adjust center so feet stay on ground
        Vector3 center = controller.center;
        center.y = controller.height / 2f;
        controller.center = center;
    }

    // IMoveController implementation
    public void ModifySpeed(float multiplier)
    {
        walkSpeed = baseWalkSpeed * multiplier;
        runSpeed = baseRunSpeed * multiplier;
    }
}
