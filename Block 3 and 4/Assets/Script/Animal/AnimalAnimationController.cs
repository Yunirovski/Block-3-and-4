using UnityEngine;

/// <summary>
/// Animal Animation Controller: Controls animal running animations
/// Automatically plays appropriate animations based on animal movement state
/// </summary>
public class AnimalAnimationController : MonoBehaviour
{
    [Header("Animation Components")]
    [Tooltip("Animal's Animator component")]
    public Animator animator;

    [Header("Animation Parameters")]
    [Tooltip("Boolean parameter name in Animator for controlling running state")]
    public string isRunParameterName = "isRun";

    [Tooltip("Float parameter name in Animator for controlling movement speed (optional)")]
    public string moveSpeedParameterName = "moveSpeed";

    [Header("Movement Detection")]
    [Tooltip("Minimum speed threshold for detecting movement")]
    public float movementThreshold = 0.1f;

    [Tooltip("Smooth time for velocity detection")]
    public float smoothTime = 0.1f;

    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebugInfo = false;

    // Internal state
    private Vector3 lastPosition;
    private float currentSpeed;
    private float velocitySmooth;
    private bool isRunning = false;

    // Cached component references
    private AnimalBehavior animalBehavior;

    void Start()
    {
        // If no Animator is manually assigned, try to get it automatically
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (animator == null)
        {
            Debug.LogError($"AnimalAnimationController: No Animator component found on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Get animal behavior component
        animalBehavior = GetComponent<AnimalBehavior>();

        // Initialize position
        lastPosition = transform.position;

        // Validate animator parameters
        ValidateAnimatorParameters();

        if (showDebugInfo)
        {
            Debug.Log($"AnimalAnimationController: Initialization complete for {gameObject.name}");
        }
    }

    void Update()
    {
        UpdateMovementDetection();
        UpdateAnimatorParameters();
    }

    /// <summary>
    /// Detect animal movement state
    /// </summary>
    private void UpdateMovementDetection()
    {
        // Calculate current speed
        Vector3 currentPosition = transform.position;
        float instantSpeed = Vector3.Distance(currentPosition, lastPosition) / Time.deltaTime;

        // Smooth speed changes
        currentSpeed = Mathf.SmoothDamp(currentSpeed, instantSpeed, ref velocitySmooth, smoothTime);

        // Update running state
        bool wasRunning = isRunning;
        isRunning = currentSpeed > movementThreshold;

        // Debug output when state changes
        if (showDebugInfo && wasRunning != isRunning)
        {
            Debug.Log($"{gameObject.name}: Running state changed - {(isRunning ? "Started running" : "Stopped running")}, Speed: {currentSpeed:F2}");
        }

        // Update last frame position
        lastPosition = currentPosition;
    }

    /// <summary>
    /// Update Animator parameters
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        // Set running boolean parameter
        if (HasParameter(isRunParameterName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(isRunParameterName, isRunning);
        }

        // Set movement speed float parameter (if exists)
        if (HasParameter(moveSpeedParameterName, AnimatorControllerParameterType.Float))
        {
            // Normalize speed to 0-1 range, adjust as needed
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / 5f); // Assuming max speed is 5
            animator.SetFloat(moveSpeedParameterName, normalizedSpeed);
        }
    }

    /// <summary>
    /// Validate if Animator parameters exist
    /// </summary>
    private void ValidateAnimatorParameters()
    {
        if (animator == null) return;

        // Check isRun parameter
        if (!HasParameter(isRunParameterName, AnimatorControllerParameterType.Bool))
        {
            Debug.LogWarning($"AnimalAnimationController: Boolean parameter '{isRunParameterName}' not found in Animator");
        }

        // Check moveSpeed parameter (optional)
        if (!string.IsNullOrEmpty(moveSpeedParameterName) &&
            !HasParameter(moveSpeedParameterName, AnimatorControllerParameterType.Float))
        {
            Debug.LogWarning($"AnimalAnimationController: Float parameter '{moveSpeedParameterName}' not found in Animator");
        }
    }

    /// <summary>
    /// Check if Animator has specified parameter
    /// </summary>
    private bool HasParameter(string paramName, AnimatorControllerParameterType paramType)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == paramType)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Force set running state (for external calls)
    /// </summary>
    public void SetRunningState(bool running)
    {
        isRunning = running;
        if (animator != null && HasParameter(isRunParameterName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(isRunParameterName, running);
        }

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Force set running state to {running}");
        }
    }

    /// <summary>
    /// Get current movement speed
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// Get if currently running
    /// </summary>
    public bool IsRunning()
    {
        return isRunning;
    }

    /// <summary>
    /// Set movement threshold
    /// </summary>
    public void SetMovementThreshold(float threshold)
    {
        movementThreshold = threshold;
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // Draw movement speed information
        if (Application.isPlaying)
        {
            Gizmos.color = isRunning ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

            // Draw velocity vector
            if (isRunning)
            {
                Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
                velocity.y = 0; // Only show horizontal velocity
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, velocity);
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        // Display debug information
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        GUILayout.Label($"Animal: {gameObject.name}");
        GUILayout.Label($"Running: {(isRunning ? "Yes" : "No")}");
        GUILayout.Label($"Speed: {currentSpeed:F2}");
        GUILayout.Label($"Threshold: {movementThreshold:F2}");
        GUILayout.EndArea();
    }
}