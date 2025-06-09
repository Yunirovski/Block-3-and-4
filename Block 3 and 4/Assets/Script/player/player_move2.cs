using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class player_move2 : MonoBehaviour, IMoveController
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float crouchSpeed = 2f;
    public float jumpHeight = 1.5f;
    public float gravity = -12f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;
    public Transform cameraTransform;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float normalHeight = 2f;
    public float crouchTransitionSpeed = 5f;

    [Header("Jump & Landing Sounds")]
    public AudioClip jumpClip;
    public AudioClip landClip;
    [Tooltip("Minimum time in air before a landing sound is played")]
    public float minAirTimeForLandSound = 0.2f;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float stepVolume = 0.7f;

    // 组件引用
    private CharacterController controller;
    private AudioSource effectAudioSource;
    private TerrainFootstepSystem footstepSystem; // 新增：地形脚步声系统引用

    // 移动状态
    private Vector3 velocity;
    private float verticalRotation = 0f;
    private bool isCrouching = false;
    private bool wasGroundedLastFrame = true;
    private bool isInAir = false;
    private float airTimeCounter = 0f;

    // 移动状态缓存（供脚步声系统使用）
    public bool IsWalking { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCrouching => isCrouching;
    public bool IsGrounded => controller.isGrounded;
    public float HorizontalSpeed { get; private set; }

    // 速度修改
    private float baseWalkSpeed;
    private float baseRunSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 设置效果音频源
        effectAudioSource = GetComponent<AudioSource>();
        if (effectAudioSource == null)
        {
            effectAudioSource = gameObject.AddComponent<AudioSource>();
        }
        effectAudioSource.playOnAwake = false;

        // 获取地形脚步声系统
        footstepSystem = GetComponent<TerrainFootstepSystem>();
        if (footstepSystem == null)
        {
            Debug.LogWarning("TerrainFootstepSystem not found! Footstep sounds will not work properly.");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        baseWalkSpeed = walkSpeed;
        baseRunSpeed = runSpeed;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleCrouchToggle();
        UpdateMovementStates(); // 新增：更新移动状态供脚步声系统使用
        DetectLanding();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");
        bool isRunning = Input.GetButton("Fire3"); // Shift键
        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

        Vector3 move = (transform.right * inputX + transform.forward * inputZ) * speed;

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
            PlayEffectOneShot(jumpClip);
        }
    }

    private void HandleCrouchToggle()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
            isCrouching = !isCrouching;

        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        float previousHeight = controller.height;

        controller.height = Mathf.MoveTowards(
            controller.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        Vector3 center = controller.center;
        center.y += (controller.height - previousHeight) / 2f;
        controller.center = center;
    }

    /// <summary>
    /// 更新移动状态，供脚步声系统使用
    /// </summary>
    private void UpdateMovementStates()
    {
        // 计算水平速度
        HorizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;

        // 判断移动状态
        IsWalking = controller.isGrounded && HorizontalSpeed > 0.1f && !Input.GetButton("Fire3");
        IsRunning = controller.isGrounded && HorizontalSpeed > 0.1f && Input.GetButton("Fire3");

        // 通知脚步声系统更新
        if (footstepSystem != null)
        {
            footstepSystem.UpdateFootstepState(IsWalking, IsRunning, isCrouching, HorizontalSpeed);
        }
    }

    private void DetectLanding()
    {
        if (!controller.isGrounded)
        {
            isInAir = true;
            airTimeCounter += Time.deltaTime;
        }
        else if (isInAir)
        {
            // 着陆了
            if (airTimeCounter >= minAirTimeForLandSound)
            {
                // 通知脚步声系统播放着陆音效
                if (footstepSystem != null)
                {
                    footstepSystem.PlayLandingSound();
                }
                else
                {
                    // 备用：直接播放着陆音效
                    PlayEffectOneShot(landClip);
                }
            }
            isInAir = false;
            airTimeCounter = 0f;
        }

        wasGroundedLastFrame = controller.isGrounded;
    }

    private void PlayEffectOneShot(AudioClip clip)
    {
        if (clip != null && effectAudioSource != null)
        {
            effectAudioSource.PlayOneShot(clip, stepVolume);
        }
    }

    public void ModifySpeed(float multiplier)
    {
        walkSpeed = baseWalkSpeed * multiplier;
        runSpeed = baseRunSpeed * multiplier;
    }

    /// <summary>
    /// 获取当前移动状态信息（调试用）
    /// </summary>
    public string GetMovementStateInfo()
    {
        return $"Walking: {IsWalking}, Running: {IsRunning}, Crouching: {IsCrouching}, " +
               $"Grounded: {IsGrounded}, Speed: {HorizontalSpeed:F2}";
    }
}