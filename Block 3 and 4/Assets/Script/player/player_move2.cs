using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
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

    [Header("Footstep Settings")]
    public AudioSource footstepAudioSource;  // Dedicated AudioSource for footsteps

    [Header("Footstep Clips - Default/Fallback")]
    public AudioClip[] defaultFootsteps;

    [Header("Terrain Layer Footstep Clips")]
    public TerrainLayerAudioPair[] terrainLayerFootsteps;  // Different sounds for different terrain layers

    [Header("Footstep Timing")]
    public float walkStepInterval = 0.5f;    // Time between steps when walking
    public float runStepInterval = 0.3f;     // Time between steps when running
    public float crouchStepInterval = 0.8f;  // Time between steps when crouching

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float stepVolume = 0.7f;
    public float pitchMin = 0.9f;           // Minimum random pitch
    public float pitchMax = 1.1f;           // Maximum random pitch

    // Internal state
    private CharacterController controller;
    private Vector3 velocity;              // Vertical velocity for gravity/jump
    private float verticalRotation = 0f;   // Camera pitch
    private bool isCrouching = false;

    // For IMoveController
    private float baseWalkSpeed;
    private float baseRunSpeed;

    // Footstep system
    private float stepTimer = 0f;
    private Dictionary<string, AudioClip[]> terrainLayerSounds;

    [System.Serializable]
    public struct TerrainLayerAudioPair
    {
        public string terrainLayerName;  // This should match the TerrainLayer asset name
        public AudioClip[] footstepClips;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Setup audio
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Initialize terrain layer sound dictionary
        InitializeTerrainLayerSounds();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Store base speeds for ModifySpeed
        baseWalkSpeed = walkSpeed;
        baseRunSpeed = runSpeed;
    }

    void InitializeTerrainLayerSounds()
    {
        terrainLayerSounds = new Dictionary<string, AudioClip[]>();

        // Add terrain layer-specific sounds to dictionary
        foreach (var pair in terrainLayerFootsteps)
        {
            if (!string.IsNullOrEmpty(pair.terrainLayerName) && pair.footstepClips != null && pair.footstepClips.Length > 0)
            {
                terrainLayerSounds[pair.terrainLayerName.ToLower()] = pair.footstepClips;
            }
        }
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleCrouchToggle();
        HandleFootsteps();
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

            // Play jump sound on takeoff
            PlayFootstepSound(true);
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

    private void HandleFootsteps()
    {
        // Only play footsteps if on ground and moving
        if (!controller.isGrounded) return;

        float horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        if (horizontalVelocity < 0.1f) return;  // Not moving enough to make footsteps

        // Update step timer
        stepTimer += Time.deltaTime;

        // Determine step interval based on movement type
        float currentStepInterval;
        if (isCrouching)
            currentStepInterval = crouchStepInterval;
        else if (Input.GetButton("Fire3"))  // Running
            currentStepInterval = runStepInterval;
        else
            currentStepInterval = walkStepInterval;

        // Play footstep when timer expires
        if (stepTimer >= currentStepInterval)
        {
            PlayFootstepSound();
            stepTimer = 0f;
        }
    }

    private void PlayFootstepSound(bool forcePlay = false)
    {
        // Detect terrain layer
        string terrainLayerName = GetCurrentTerrainLayer();

        // Get appropriate sound clip
        AudioClip[] clips = GetFootstepClipsForTerrainLayer(terrainLayerName);
        if (clips == null || clips.Length == 0)
        {
            return;  // No sound to play
        }

        // Choose random clip
        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];

        // Set audio properties
        footstepAudioSource.clip = clipToPlay;
        footstepAudioSource.volume = stepVolume;
        footstepAudioSource.pitch = Random.Range(pitchMin, pitchMax);

        // Play the sound
        footstepAudioSource.Play();
    }

    private string GetCurrentTerrainLayer()
    {
        // First try to detect if we're on terrain
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 2f))
        {
            // Check if we hit a terrain
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                // Get the dominant terrain layer at the hit position
                return GetDominantTerrainLayer(terrain, hit.point);
            }

            // If not on terrain, check for regular surface material
            if (hit.collider.GetComponent<Renderer>() != null)
            {
                Material mat = hit.collider.GetComponent<Renderer>().material;
                if (mat != null)
                {
                    // Try to match material name to terrain layer name
                    string materialName = mat.name.Replace(" (Instance)", "").ToLower();
                    if (terrainLayerSounds.ContainsKey(materialName))
                    {
                        return materialName;
                    }
                }
            }
        }

        return "default";  // Default if nothing else found
    }

    private string GetDominantTerrainLayer(Terrain terrain, Vector3 worldPosition)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = worldPosition - terrain.GetPosition();

        // Convert world position to terrain local coordinates
        Vector3 normalizedPos = new Vector3(
            terrainPosition.x / terrainData.size.x,
            terrainPosition.y / terrainData.size.y,
            terrainPosition.z / terrainData.size.z
        );

        // Get the alphamap coordinate
        int mapX = (int)(normalizedPos.x * terrainData.alphamapWidth);
        int mapZ = (int)(normalizedPos.z * terrainData.alphamapHeight);

        // Clamp coordinates
        mapX = Mathf.Clamp(mapX, 0, terrainData.alphamapWidth - 1);
        mapZ = Mathf.Clamp(mapZ, 0, terrainData.alphamapHeight - 1);

        // Get the splatmap data at this position
        float[,,] splatmaps = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        // Find the dominant layer
        float maxMix = 0;
        int maxIndex = 0;

        for (int i = 0; i < terrainData.terrainLayers.Length; i++)
        {
            if (splatmaps[0, 0, i] > maxMix)
            {
                maxMix = splatmaps[0, 0, i];
                maxIndex = i;
            }
        }

        // Return the name of the dominant terrain layer
        TerrainLayer dominantLayer = terrainData.terrainLayers[maxIndex];
        if (dominantLayer != null)
        {
            // Remove the file extension and make lowercase for matching
            string layerName = dominantLayer.name.Replace(" ", "").ToLower();
            return layerName;
        }

        return "default";
    }

    private AudioClip[] GetFootstepClipsForTerrainLayer(string terrainLayerName)
    {
        // First try to find terrain layer-specific sounds
        if (terrainLayerSounds.TryGetValue(terrainLayerName, out AudioClip[] clips))
        {
            return clips;
        }

        // Fall back to default footsteps
        return defaultFootsteps;
    }

    // IMoveController implementation
    public void ModifySpeed(float multiplier)
    {
        walkSpeed = baseWalkSpeed * multiplier;
        runSpeed = baseRunSpeed * multiplier;
    }
}