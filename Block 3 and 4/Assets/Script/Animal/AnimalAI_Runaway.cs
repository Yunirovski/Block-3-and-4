using UnityEngine;

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

    [Header("Model Settings")]
    public Vector3 modelRotationOffset; // 模型朝向修正

    private Vector3 wanderCenter;
    private Vector3 targetPosition;
    private float wanderTimer;
    private float escapeCooldown = 0f;
    private bool isEscaping;
    private bool isReturning;
    private Transform player;

    private void Start()
    {
        wanderCenter = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        SetNewWanderTarget();
    }

    private void Update()
    {
        if (escapeCooldown > 0f)
        {
            escapeCooldown -= Time.deltaTime;
        }

        if (isEscaping || escapeCooldown > 0f)
        {
            EscapeFromPlayer();
            return;
        }

        if (!isEscaping && Vector3.Distance(transform.position, wanderCenter) > wanderRadius)
        {
            isReturning = true;
            targetPosition = wanderCenter + (wanderCenter - transform.position).normalized * (wanderRadius * 0.8f);
        }
        else if (Vector3.Distance(transform.position, wanderCenter) <= wanderRadius * 0.9f)
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

    private void ReturnToWanderArea()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        Vector3 direction = targetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            targetRotation *= Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isReturning = false;
            SetNewWanderTarget();
        }
    }

    private void Wander()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        Vector3 direction = targetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            targetRotation *= Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        wanderTimer -= Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f || wanderTimer <= 0)
        {
            SetNewWanderTarget();
        }
    }

    private void SetNewWanderTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPosition = wanderCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    private void CheckForPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < detectionRadius)
        {
            isEscaping = true;
            escapeCooldown = forcedEscapeDuration;
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

        transform.position = Vector3.MoveTowards(transform.position, escapeTarget, escapeSpeed * Time.deltaTime);

        if (escapeDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(escapeDirection, Vector3.up);
            targetRotation *= Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer >= safeDistance && escapeCooldown <= 0f)
        {
            isEscaping = false;
            wanderCenter = transform.position;
            SetNewWanderTarget();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centerToDraw = Application.isPlaying ? wanderCenter : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(centerToDraw, wanderRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private void OnDrawGizmos()
    {
        Vector3 centerToDraw = Application.isPlaying ? wanderCenter : transform.position;

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawSphere(centerToDraw, wanderRadius);

        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}
