// Assets/Scripts/Animal/AnimalBehavior.cs
using UnityEngine;
using UnityEngine.AI;

public class AnimalBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float wanderRadius = 40f;
    public float minWanderTime = 2f;
    public float maxWanderTime = 5f;

    [Header("Escape Settings")]
    public float escapeSpeed = 6f;
    public float detectionRadius = 20f;
    public float safeDistance = 8f;
    public float forcedEscapeDuration = 10f;

    [Header("NavMesh Settings")]
    [Tooltip("NavMesh Agent stopping distance")]
    public float stoppingDistance = 0.5f; // Reduced default stopping distance
    [Tooltip("NavMesh sampling distance")]
    public float sampleDistance = 5f;
    [Tooltip("Use agent auto-rotation (suggest disabling for custom rotation)")]
    public bool useAgentRotation = false;
    [Tooltip("Detection distance for food interaction")]
    public float foodInteractionDistance = 2.5f;
    [Tooltip("Force setting NavMeshAgent radius (0 = auto)")]
    public float forceAgentRadius = 0f;

    [Header("Food Interaction Settings")]
    [Tooltip("Animal temperament determines how it approaches/avoids the player")]
    public Temperament temperament = Temperament.Neutral;
    [Tooltip("Detection radius for finding food")]
    public float foodDetectionRadius = 20f;
    [Tooltip("Distance threshold for Fearful/Hostile temperament to judge player proximity")]
    public float playerSafeDistance = 15f;
    [Tooltip("Duration of eating food (seconds)")]
    public float eatDuration = 5f;
    [Tooltip("Calm period after eating (seconds)")]
    public float graceDuration = 10f;

    [Header("Model Settings")]
    public Vector3 modelRotationOffset;

    // — Stun state —
    private bool isStunned = false;
    private float stunTimer = 0f;

    // — Attracted state —
    private bool isAttracted = false;
    private float attractTimer = 0f;
    private Transform attractTarget = null;

    // — Food interaction state —
    private enum FoodState { Idle, Approaching, Eating, Grace }
    private FoodState foodState = FoodState.Idle;
    private Transform targetFood = null;
    private float foodTimer = 0f;

    // — Roaming/Escaping state —
    private Vector3 wanderCenter;
    private Vector3 targetPosition;
    private float wanderTimer;
    private float escapeCooldown = 0f;
    private bool isEscaping;
    private bool isReturning;

    private Transform player;
    private NavMeshAgent agent;
    private bool hasValidPath = false;

    void Start()
    {
        // Get or add NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            Debug.Log($"{gameObject.name}: Automatically added NavMeshAgent component");
        }

        // Configure NavMeshAgent
        ConfigureNavMeshAgent();

        wanderCenter = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Ensure the start position is on NavMesh
        ValidateStartPosition();

        SetNewWanderTarget();
    }

    void ConfigureNavMeshAgent()
    {
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = useAgentRotation;
        agent.updateUpAxis = true; // Keep on NavMesh surface
        agent.autoBraking = true;

        // Set reasonable turn speed
        agent.angularSpeed = 180f;
        agent.acceleration = 8f;

        // Important: Set reasonable agent size to avoid collision issues
        if (forceAgentRadius > 0f)
        {
            agent.radius = forceAgentRadius;
        }
        else if (agent.radius == 0.5f) // If default, adjust it
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // Set agent radius based on actual collider size
                agent.radius = Mathf.Max(col.bounds.size.x, col.bounds.size.z) * 0.4f; // Slightly smaller than actual
                agent.height = col.bounds.size.y;
                Debug.Log($"{gameObject.name}: Set NavMeshAgent - radius: {agent.radius:F2}, height: {agent.height:F2}");
            }
        }
    }

    void ValidateStartPosition()
    {
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, sampleDistance, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{gameObject.name}: Not on NavMesh, trying to find nearest NavMesh position");

            // Try to find NavMesh in larger radius
            if (NavMesh.SamplePosition(transform.position, out hit, sampleDistance * 3, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Debug.Log($"{gameObject.name}: Moved to nearest NavMesh position {hit.position}");
            }
            else
            {
                Debug.LogError($"{gameObject.name}: Could not find a valid NavMesh position! Check NavMesh baking");
            }
        }
    }

    void Update()
    {
        // Check if Agent is valid
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning($"{gameObject.name}: NavMeshAgent is invalid or not on NavMesh");
            return;
        }

        // 1) If stunned: only count down timer
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                agent.isStopped = false; // Resume movement
            }
            else
            {
                agent.isStopped = true; // Stop movement
            }
            return;
        }

        // 2) If attracted: move toward attractTarget
        if (isAttracted)
        {
            attractTimer -= Time.deltaTime;
            if (attractTimer <= 0f || attractTarget == null)
            {
                isAttracted = false;
                wanderCenter = transform.position;
                SetNewWanderTarget();
            }
            else
            {
                MoveToPosition(attractTarget.position, moveSpeed);
                HandleRotation(attractTarget.position);
            }
            return;
        }

        // 3) Food interaction has higher priority than escape or wander
        HandleFoodInteraction();
        if (foodState != FoodState.Idle)
        {
            return;
        }

        // 4) Escape logic: update cooldown
        if (escapeCooldown > 0f) escapeCooldown -= Time.deltaTime;
        if (isEscaping || escapeCooldown > 0f)
        {
            EscapeFromPlayer();
            return;
        }

        // 5) Roaming or returning
        float distToCenter = Vector3.Distance(transform.position, wanderCenter);
        if (distToCenter > wanderRadius)
        {
            isReturning = true;
            Vector3 returnPos = wanderCenter + (wanderCenter - transform.position).normalized * (wanderRadius * 0.8f);
            MoveToPosition(returnPos, moveSpeed);
        }
        else if (distToCenter <= wanderRadius * 0.9f)
        {
            isReturning = false;
        }

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

    #region NavMesh Movement System

    /// <summary>
    /// Move to target position using NavMesh
    /// </summary>
    private bool MoveToPosition(Vector3 targetPos, float speed)
    {
        if (agent == null || !agent.isOnNavMesh) return false;

        // Find nearest valid NavMesh position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, sampleDistance, NavMesh.AllAreas))
        {
            agent.speed = speed;
            agent.SetDestination(hit.position);
            hasValidPath = agent.hasPath;
            return true;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Could not find valid NavMesh near target {targetPos}");
            return false;
        }
    }

    /// <summary>
    /// Handle rotation if agent rotation is disabled
    /// </summary>
    private void HandleRotation(Vector3 targetPos)
    {
        if (useAgentRotation) return;

        Vector3 direction = (targetPos - transform.position);
        direction.y = 0; // Horizontal only

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up)
                                       * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Check if agent reached its destination
    /// </summary>
    private bool HasReachedDestination()
    {
        if (agent == null || !agent.hasPath || agent.pathPending) return false;

        // Looser check
        return agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    /// <summary>
    /// Get a random NavMesh position within a radius
    /// </summary>
    private bool GetRandomNavMeshPosition(Vector3 center, float radius, out Vector3 result)
    {
        result = Vector3.zero;

        for (int i = 0; i < 10; i++) // Try up to 10 times
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, sampleDistance, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        return false;
    }

    #endregion
    #region Food Interaction Logic

    private void HandleFoodInteraction()
    {
        switch (foodState)
        {
            case FoodState.Idle:
                DetectAndReactToFood();
                break;
            case FoodState.Approaching:
                MoveTowardsFood();
                break;
            case FoodState.Eating:
                foodTimer -= Time.deltaTime;
                if (foodTimer <= 0f)
                {
                    foodState = FoodState.Grace;
                    foodTimer = graceDuration;
                }
                break;
            case FoodState.Grace:
                foodTimer -= Time.deltaTime;
                if (foodTimer <= 0f)
                {
                    foodState = FoodState.Idle;
                    wanderCenter = transform.position;
                    SetNewWanderTarget();

                    // ✅ Fix: ensure completely back to normal
                    if (agent != null)
                    {
                        agent.isStopped = false;
                    }
                }
                break;
        }
    }

    private void DetectAndReactToFood()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, foodDetectionRadius);

        foreach (var col in colliders)
        {
            var food = col.GetComponent<FoodWorld>();
            if (food == null) continue;

            float distToPlayer = Vector3.Distance(transform.position,
                                                  player != null ? player.position : Camera.main.transform.position);

            bool shouldApproach = false;
            switch (temperament)
            {
                case Temperament.Neutral:
                    shouldApproach = true;
                    break;
                case Temperament.Fearful:
                    shouldApproach = distToPlayer > playerSafeDistance;
                    break;
                case Temperament.Hostile:
                    shouldApproach = distToPlayer <= playerSafeDistance;
                    break;
            }

            if (shouldApproach)
            {
                StartApproachFood(food.transform);
                return;
            }
        }
    }

    private void StartApproachFood(Transform food)
    {
        targetFood = food;
        foodState = FoodState.Approaching;

        isEscaping = false;
        escapeCooldown = 0f;
        isReturning = false;

        Debug.Log($"{gameObject.name} started moving toward food");
    }

    private void MoveTowardsFood()
    {
        if (targetFood == null)
        {
            foodState = FoodState.Idle;
            return;
        }

        // Use a smaller stopping distance to approach food more closely
        float originalStopDistance = agent.stoppingDistance;
        agent.stoppingDistance = 0.1f;

        MoveToPosition(targetFood.position, moveSpeed);
        HandleRotation(targetFood.position);

        float distanceToFood = Vector3.Distance(transform.position, targetFood.position);

        float animalRadius = agent.radius;
        Collider foodCollider = targetFood.GetComponent<Collider>();
        float foodRadius = foodCollider != null ? foodCollider.bounds.size.magnitude * 0.5f : 0.5f;
        float totalRadius = animalRadius + foodRadius + 0.5f; // Extra buffer

        bool nearFood = distanceToFood <= totalRadius;
        bool reachedDestination = HasReachedDestination();
        bool stoppedMoving = agent.velocity.magnitude < 0.1f && !agent.pathPending;
        bool stuckNearFood = distanceToFood < totalRadius * 1.5f && agent.velocity.magnitude < 0.1f;

        if (nearFood || reachedDestination || stoppedMoving || stuckNearFood)
        {
            foodState = FoodState.Eating;
            foodTimer = eatDuration;
            agent.isStopped = true;
            agent.stoppingDistance = originalStopDistance;

            if (targetFood != null)
            {
                Destroy(targetFood.gameObject);
                targetFood = null;
                Debug.Log($"{gameObject.name} started eating - Distance: {distanceToFood:F2}m, Required: {totalRadius:F2}m, " +
                          $"Animal Radius: {animalRadius:F2}m, Food Radius: {foodRadius:F2}m, " +
                          $"Reached: {reachedDestination}, Stopped: {stoppedMoving}, Stuck: {stuckNearFood}");
            }
        }

        // Give up if the food is too far
        if (!nearFood && !reachedDestination && Vector3.Distance(transform.position, targetFood.position) > foodDetectionRadius)
        {
            Debug.LogWarning($"{gameObject.name} food too far, giving up");
            foodState = FoodState.Idle;
            agent.stoppingDistance = originalStopDistance;
        }
    }

    #endregion

    /// <summary>External call: make the animal stunned</summary>
    public void Stun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
        isEscaping = false;
        escapeCooldown = 0f;
        isReturning = false;
        isAttracted = false;

        foodState = FoodState.Idle;
        targetFood = null;
        foodTimer = 0f;

        if (agent != null) agent.isStopped = true;
    }

    /// <summary>External call: attract the animal using a wand</summary>
    public void Attract(Transform target, float duration)
    {
        if (target == null || duration <= 0f) return;

        isAttracted = true;
        attractTarget = target;
        attractTimer = duration;
        isStunned = false;
        isEscaping = false;
        escapeCooldown = 0f;
        isReturning = false;

        foodState = FoodState.Idle;
        targetFood = null;
        foodTimer = 0f;

        if (agent != null) agent.isStopped = false;
    }

    private void ReturnToWanderArea()
    {
        if (!isReturning) return;

        HandleRotation(targetPosition);

        if (HasReachedDestination())
        {
            isReturning = false;
            SetNewWanderTarget();
        }
    }

    private void Wander()
    {
        HandleRotation(targetPosition);

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || HasReachedDestination())
        {
            SetNewWanderTarget();
        }
    }

    private void SetNewWanderTarget()
    {
        Vector3 newTarget;
        if (GetRandomNavMeshPosition(wanderCenter, wanderRadius, out newTarget))
        {
            targetPosition = newTarget;
            MoveToPosition(targetPosition, moveSpeed);
            wanderTimer = Random.Range(minWanderTime, maxWanderTime);
        }
        else
        {
            // If no valid position found, extend wait time
            wanderTimer = Random.Range(minWanderTime, maxWanderTime);
            Debug.LogWarning($"{gameObject.name}: Could not find valid wander target");
        }
    }

    private void CheckForPlayer()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < detectionRadius)
        {
            isEscaping = true;
            escapeCooldown = forcedEscapeDuration;

            foodState = FoodState.Idle;
            targetFood = null;
            foodTimer = 0f;
        }
    }

    private void EscapeFromPlayer()
    {
        if (player == null)
        {
            isEscaping = false;
            return;
        }

        Vector3 escapeDirection = (transform.position - player.position).normalized;
        Vector3 escapeTarget = transform.position + escapeDirection * safeDistance;

        // Try to find a valid NavMesh position for escape
        Vector3 validEscapeTarget;
        if (GetRandomNavMeshPosition(escapeTarget, sampleDistance, out validEscapeTarget))
        {
            MoveToPosition(validEscapeTarget, escapeSpeed);
        }

        HandleRotation(escapeTarget);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist >= safeDistance && escapeCooldown <= 0f)
        {
            isEscaping = false;
            wanderCenter = transform.position;
            SetNewWanderTarget();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying ? wanderCenter : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(c, wanderRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, foodDetectionRadius);

        // Show NavMesh path
        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            Vector3[] pathCorners = agent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 c = Application.isPlaying ? wanderCenter : transform.position;
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawSphere(c, wanderRadius);
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, foodDetectionRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}

/// <summary>Animal temperament enum</summary>
public enum Temperament { Neutral, Fearful, Hostile }
