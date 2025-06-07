// Assets/Scripts/Projectiles/DartProjectile.cs
using UnityEngine;

/// <summary>
/// Handles dart projectile physics, collision detection, and lifetime
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DartProjectile : MonoBehaviour
{
    [Header("Dart Physics")]
    [Tooltip("Gravity multiplier")]
    public float gravityMultiplier = 1f;
    [Tooltip("Air drag coefficient")]
    public float dragCoefficient = 0.1f;
    [Tooltip("Minimum speed before dart drops")]
    public float minimumSpeed = 1f;

    [Header("Collision")]
    [Tooltip("Layer mask for valid targets")]
    public LayerMask targetLayers = -1;
    [Tooltip("Minimum impact velocity for sticking")]
    public float stickThreshold = 5f;

    [Header("Lifetime")]
    [Tooltip("Maximum flight time before auto-destruction")]
    public float maxFlightTime = 10f;
    [Tooltip("Time to stay stuck in surface")]
    public float stuckDuration = 30f;

    [Header("Visual")]
    [Tooltip("Trail renderer for dart trail")]
    public TrailRenderer dartTrail;

    // State tracking
    private Vector3 velocity;
    private Vector3 startPosition;
    private float maxRange;
    private float stunDuration;
    private DartGunController controller;
    private Rigidbody rb;
    private Collider col;
    private bool hasHit = false;
    private bool isInitialized = false;
    private float flightTimer = 0f;

    // Physics cache
    private Vector3 lastPosition;
    private float lastSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Configure rigidbody
        rb.useGravity = false; // We'll handle gravity manually
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Configure collider
        if (col.isTrigger)
        {
            col.isTrigger = false; // Ensure it's not a trigger for proper collision
        }

        // Setup trail if available
        if (dartTrail == null)
        {
            dartTrail = GetComponent<TrailRenderer>();
        }

        lastPosition = transform.position;
    }

    public void Initialize(Vector3 initialVelocity, float range, float stun, DartGunController gunController)
    {
        velocity = initialVelocity;
        startPosition = transform.position;
        maxRange = range;
        stunDuration = stun;
        controller = gunController;
        isInitialized = true;

        // Set initial physics
        rb.linearVelocity = velocity;
        lastSpeed = velocity.magnitude;

        Debug.Log($"Dart initialized with velocity: {velocity}, range: {range}");
    }

    void FixedUpdate()
    {
        if (!isInitialized || hasHit) return;

        flightTimer += Time.fixedDeltaTime;

        // Check max flight time
        if (flightTimer > maxFlightTime)
        {
            DestroyDart();
            return;
        }

        // Check max range
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled > maxRange)
        {
            DestroyDart();
            return;
        }

        // Update physics
        UpdateDartPhysics();

        // Update rotation to face velocity direction
        UpdateRotation();

        // Cache position for next frame
        lastPosition = transform.position;
    }

    private void UpdateDartPhysics()
    {
        Vector3 currentVelocity = rb.linearVelocity;

        // Apply gravity
        Vector3 gravity = Physics.gravity * gravityMultiplier;
        currentVelocity += gravity * Time.fixedDeltaTime;

        // Apply drag
        if (currentVelocity.magnitude > 0.1f)
        {
            Vector3 dragForce = -currentVelocity.normalized * (currentVelocity.sqrMagnitude * dragCoefficient);
            currentVelocity += dragForce * Time.fixedDeltaTime;
        }

        // Check minimum speed
        if (currentVelocity.magnitude < minimumSpeed)
        {
            DestroyDart();
            return;
        }

        rb.linearVelocity = currentVelocity;
        lastSpeed = currentVelocity.magnitude;
    }

    private void UpdateRotation()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        if (currentVelocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(currentVelocity.normalized);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit || !isInitialized) return;

        hasHit = true;
        Vector3 hitPoint = collision.contacts[0].point;
        Collider hitCollider = collision.collider;

        Debug.Log($"Dart hit: {hitCollider.name} at {hitPoint}");

        // Stop physics
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Disable trail
        if (dartTrail != null)
        {
            dartTrail.enabled = false;
        }

        // Check if we should stick to the surface
        bool shouldStick = ShouldStickToSurface(collision, hitCollider);

        if (shouldStick)
        {
            StickToSurface(collision);
        }

        // Notify controller
        if (controller != null)
        {
            controller.OnDartHit(hitPoint, hitCollider, gameObject);
        }

        // Schedule destruction
        if (shouldStick)
        {
            Invoke(nameof(DestroyDart), stuckDuration);
        }
        else
        {
            Invoke(nameof(DestroyDart), 1f); // Quick cleanup for non-stick surfaces
        }
    }

    private bool ShouldStickToSurface(Collision collision, Collider hitCollider)
    {
        // Check impact velocity
        if (lastSpeed < stickThreshold)
        {
            return false;
        }

        // Check if surface is valid for sticking
        if (hitCollider.CompareTag("Player"))
        {
            return false; // Don't stick to player
        }

        // Check if it's an animal (should stick for tranquilizer effect)
        AnimalBehavior animal = hitCollider.GetComponent<AnimalBehavior>();
        if (animal != null)
        {
            return true;
        }

        // Check if surface is static or has sufficient mass
        Rigidbody hitRb = hitCollider.GetComponent<Rigidbody>();
        if (hitRb == null || hitRb.isKinematic || hitRb.mass > 10f)
        {
            return true;
        }

        return false;
    }

    private void StickToSurface(Collision collision)
    {
        // Position dart slightly embedded in surface
        Vector3 normal = collision.contacts[0].normal;
        Vector3 surfacePoint = collision.contacts[0].point;

        // Embed dart slightly into surface
        transform.position = surfacePoint - normal * 0.1f;

        // Align dart with surface normal
        transform.rotation = Quaternion.LookRotation(-normal);

        // Parent to hit object if it moves
        Transform hitTransform = collision.collider.transform;
        if (hitTransform.GetComponent<Rigidbody>() != null)
        {
            transform.SetParent(hitTransform);
        }

        Debug.Log($"Dart stuck to: {collision.collider.name}");
    }

    private void DestroyDart()
    {
        if (controller != null)
        {
            // This will remove the dart from the active darts list
            controller.OnDartHit(transform.position, null, gameObject);
        }

        Destroy(gameObject);
    }

    // Visualization for debugging
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw velocity vector
        Gizmos.color = Color.red;
        if (rb != null && !hasHit)
        {
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        }

        // Draw range sphere from start position
        if (isInitialized)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPosition, maxRange);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw detailed flight path prediction
        if (!Application.isPlaying || hasHit) return;

        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position;
        Vector3 vel = rb != null ? rb.linearVelocity : velocity;

        // Predict next few seconds of flight
        for (int i = 0; i < 20; i++)
        {
            float dt = 0.2f;
            Vector3 nextPos = pos + vel * dt;

            // Apply gravity prediction
            vel += Physics.gravity * gravityMultiplier * dt;

            Gizmos.DrawLine(pos, nextPos);
            pos = nextPos;

            if (pos.y < startPosition.y - 50f) break; // Stop predicting if too far down
        }
    }
}