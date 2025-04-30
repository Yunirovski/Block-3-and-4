using UnityEngine;

public class AnimalBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f; // How fast the animal walks
    public float wanderRadius = 40f; // How far the animal can walk around
    public float minWanderTime = 2f; // Smallest time before changing direction
    public float maxWanderTime = 5f; // Longest time before changing direction

    [Header("Escape Settings")]
    public float escapeSpeed = 6f; // How fast the animal runs away
    public float detectionRadius = 20f; // How close the player can get before scaring the animal
    public float safeDistance = 8f; // How far the animal wants to be from the player
    public float forcedEscapeDuration = 10f; // Time the animal keeps running, even if player is gone

    [Header("Model Settings")]
    public Vector3 modelRotationOffset; // Fix the way the animal faces

    private Vector3 wanderCenter; // Where the animal started walking from
    private Vector3 targetPosition; // Where the animal wants to walk to
    private float wanderTimer; // Countdown until next walk direction
    private float escapeCooldown = 0f; // Countdown while escaping
    private bool isEscaping; // Is the animal running away now?
    private bool isReturning; // Is the animal going back to its area?
    private Transform player; // The player character

    private void Start()
    {
        wanderCenter = transform.position; // Set start point
        player = GameObject.FindGameObjectWithTag("Player")?.transform; // Find the player in the game
        SetNewWanderTarget(); // Pick a place to walk to
    }

    private void Update()
    {
        if (escapeCooldown > 0f)
        {
            escapeCooldown -= Time.deltaTime; // Count down escape time
        }

        // If running or still in escape time, keep running away
        if (isEscaping || escapeCooldown > 0f)
        {
            EscapeFromPlayer();
            return;
        }

        // If too far away from home, go back
        if (!isEscaping && Vector3.Distance(transform.position, wanderCenter) > wanderRadius)
        {
            isReturning = true;
            targetPosition = wanderCenter + (wanderCenter - transform.position).normalized * (wanderRadius * 0.8f);
        }
        // If close to home, stop returning
        else if (Vector3.Distance(transform.position, wanderCenter) <= wanderRadius * 0.9f)
        {
            isReturning = false;
        }

        // Go back or wander normally
        if (isReturning)
        {
            ReturnToWanderArea();
        }
        else
        {
            Wander();
            CheckForPlayer();
        }
    }

    private void ReturnToWanderArea()
    {
        // Move to the home area
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Face the direction of movement
        Vector3 direction = targetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            targetRotation *= Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // If reached the target, stop returning and wander again
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isReturning = false;
            SetNewWanderTarget();
        }
    }

    private void Wander()
    {
        // Walk to the target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Turn to face where it’s walking
        Vector3 direction = targetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            targetRotation *= Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Count down the timer
        wanderTimer -= Time.deltaTime;

        // Pick a new place to go if timer runs out or target is reached
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f || wanderTimer <= 0)
        {
            SetNewWanderTarget();
        }
    }

    private void SetNewWanderTarget()
    {
        // Pick a random spot around the center
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPosition = wanderCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
        // Set how long to walk there
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    private void CheckForPlayer()
    {
        // If there is no player, do nothing
        if (player == null) return;

        // Check how far the player is
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < detectionRadius)
        {
            // Start escaping if the player is too close
            isEscaping = true;
            escapeCooldown = forcedEscapeDuration;
        }
    }

    private void EscapeFromPlayer()
    {
        // If no player, stop escaping
        if (player == null)
        {
            isEscaping = false;
            return;
        }

        // Run away from the player
        Vector3 escapeDirection = (transform.position - player.position).normalized;
        Vector3 escapeTarget = transform.position + escapeDirection * safeDistance;

        transform.position = Vector3.MoveTowards(transform.position, escapeTarget, escapeSpeed * Time.deltaTime);

        // Turn to face where it’s running
        if (escapeDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(escapeDirection, Vector3.up);
            targetRotation *= Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // If far enough and time is up, stop escaping and wander again
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer >= safeDistance && escapeCooldown <= 0f)
        {
            isEscaping = false;
            wanderCenter = transform.position; // Set new center
            SetNewWanderTarget();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Show where the animal can walk and detect players (in editor)
        Vector3 centerToDraw = Application.isPlaying ? wanderCenter : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(centerToDraw, wanderRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private void OnDrawGizmos()
    {
        // Show walking and detection area as see-through colors (in editor)
        Vector3 centerToDraw = Application.isPlaying ? wanderCenter : transform.position;

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawSphere(centerToDraw, wanderRadius);

        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}
