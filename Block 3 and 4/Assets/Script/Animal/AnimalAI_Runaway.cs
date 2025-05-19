// Assets/Scripts/Animal/AnimalBehavior.cs
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
    public Vector3 modelRotationOffset;

    // —— 昏迷状态 —— 
    private bool isStunned = false;
    private float stunTimer = 0f;

    // —— 吸引状态 —— 
    private bool isAttracted = false;
    private float attractTimer = 0f;
    private Transform attractTarget = null;

    // —— 逃跑/漫游状态 —— 
    private Vector3 wanderCenter;
    private Vector3 targetPosition;
    private float wanderTimer;
    private float escapeCooldown = 0f;
    private bool isEscaping;
    private bool isReturning;

    private Transform player;

    void Start()
    {
        // 禁用 NavMeshAgent，全部改为手动移动
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        wanderCenter = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        SetNewWanderTarget();
    }

    void Update()
    {
        // 1) 昏迷中：只计时
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
            return;
        }

        // 2) 吸引中：水平走向 attractTarget
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
                // 水平移动到玩家（摄像机）所在 XZ 平面位置
                Vector3 targetPos = attractTarget.position;
                targetPos.y = transform.position.y;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime);

                // 面向玩家方向
                Vector3 lookDir = (targetPos - transform.position).normalized;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion rot = Quaternion.LookRotation(lookDir, Vector3.up)
                                     * Quaternion.Euler(modelRotationOffset);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, rot, 5f * Time.deltaTime);
                }
            }
            return;
        }

        // 3) 逃跑优先：更新逃跑计时
        if (escapeCooldown > 0f) escapeCooldown -= Time.deltaTime;
        if (isEscaping || escapeCooldown > 0f)
        {
            EscapeFromPlayer();
            return;
        }

        // 4) 漫游/返回
        float distToCenter = Vector3.Distance(transform.position, wanderCenter);
        if (distToCenter > wanderRadius)
        {
            isReturning = true;
            targetPosition = wanderCenter + (wanderCenter - transform.position).normalized * (wanderRadius * 0.8f);
        }
        else if (distToCenter <= wanderRadius * 0.9f)
        {
            isReturning = false;
        }

        if (isReturning)
            ReturnToWanderArea();
        else
        {
            Wander();
            CheckForPlayer();
        }
    }

    /// <summary>外部调用：令动物进入昏迷</summary>
    public void Stun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
        isEscaping = false;
        escapeCooldown = 0f;
        isReturning = false;
        isAttracted = false;
    }

    /// <summary>外部调用：令动物被法杖吸引</summary>
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
    }

    private void ReturnToWanderArea()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        Vector3 dir = (targetPosition - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up)
                             * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 5f * Time.deltaTime);
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

        Vector3 dir = (targetPosition - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up)
                             * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 5f * Time.deltaTime);
        }

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || Vector3.Distance(transform.position, targetPosition) < 0.1f)
            SetNewWanderTarget();
    }

    private void SetNewWanderTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        targetPosition = wanderCenter + new Vector3(rnd.x, 0, rnd.y);
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    private void CheckForPlayer()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < detectionRadius)
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

        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 fleeTarget = transform.position + dir * safeDistance;

        transform.position = Vector3.MoveTowards(transform.position, fleeTarget, escapeSpeed * Time.deltaTime);

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up)
                             * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10f * Time.deltaTime);
        }

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
    }

    private void OnDrawGizmos()
    {
        Vector3 c = Application.isPlaying ? wanderCenter : transform.position;
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawSphere(c, wanderRadius);
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}
