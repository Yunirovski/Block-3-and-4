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

    [Header("Footstep Settings")]
    public AudioSource footstepAudioSource;

    [Header("Loop Footstep Clips")]
    public AudioClip walkLoopClip;
    public AudioClip runLoopClip;
    public AudioClip crouchLoopClip;

    [Header("Jump & Landing Sounds")]
    public AudioClip jumpClip;
    public AudioClip landClip;
    [Tooltip("Minimum time in air before a landing sound is played")]
    public float minAirTimeForLandSound = 0.2f;

    [Header("Extra Audio")]
    public AudioSource effectAudioSource;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float stepVolume = 0.7f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;

    private CharacterController controller;
    private Vector3 velocity;
    private float verticalRotation = 0f;
    private bool isCrouching = false;
    private bool wasGroundedLastFrame = true;
    private bool isInAir = false;
    private float airTimeCounter = 0f;

    private float baseWalkSpeed;
    private float baseRunSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (effectAudioSource == null)
        {
            effectAudioSource = gameObject.AddComponent<AudioSource>();
            effectAudioSource.playOnAwake = false;
        }

        footstepAudioSource.loop = true;

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
        HandleFootsteps();
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
        bool isRunning = Input.GetButton("Fire3");
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

    private void HandleFootsteps()
    {
        if (!controller.isGrounded)
        {
            StopFootstepLoop();
            return;
        }

        float horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        if (horizontalVelocity < 0.1f)
        {
            StopFootstepLoop();
            return;
        }

        AudioClip targetClip = isCrouching ? crouchLoopClip :
                               (Input.GetButton("Fire3") ? runLoopClip : walkLoopClip);

        if (footstepAudioSource.clip != targetClip)
        {
            footstepAudioSource.clip = targetClip;
            footstepAudioSource.loop = true;
            footstepAudioSource.volume = stepVolume;
            footstepAudioSource.pitch = Random.Range(pitchMin, pitchMax);
            footstepAudioSource.Play();
        }
        else if (!footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Play();
        }
    }

    private void StopFootstepLoop()
    {
        if (footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
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
            if (airTimeCounter >= minAirTimeForLandSound)
            {
                PlayEffectOneShot(landClip);
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
}
