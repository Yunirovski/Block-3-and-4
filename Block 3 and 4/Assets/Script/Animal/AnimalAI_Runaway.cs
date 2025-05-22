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

    [Header("Food Interaction Settings")]
    [Tooltip("动物性格决定靠近/回避玩家的方式")]
    public Temperament temperament = Temperament.Neutral;
    [Tooltip("动物可以侦测食物的半径")]
    public float foodDetectionRadius = 20f;
    [Tooltip("Fearful/Hostile 判定玩家距离的阈值")]
    public float playerSafeDistance = 15f;
    [Tooltip("吃食物的持续时间（秒）")]
    public float eatDuration = 5f;
    [Tooltip("吃完后保持冷静的时间（秒）")]
    public float graceDuration = 10f;

    [Header("Model Settings")]
    public Vector3 modelRotationOffset;

    // —— 昏迷状态 —— 
    private bool isStunned = false;
    private float stunTimer = 0f;

    // —— 吸引状态 —— 
    private bool isAttracted = false;
    private float attractTimer = 0f;
    private Transform attractTarget = null;

    // —— 食物交互状态 —— 
    private enum FoodState { Idle, Approaching, Eating, Grace }
    private FoodState foodState = FoodState.Idle;
    private Transform targetFood = null;
    private float foodTimer = 0f;

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

        // 3) 食物交互优先级高于逃跑和漫游
        HandleFoodInteraction();
        if (foodState != FoodState.Idle)
        {
            return; // 如果在处理食物，不执行其他行为
        }

        // 4) 逃跑优先：更新逃跑计时
        if (escapeCooldown > 0f) escapeCooldown -= Time.deltaTime;
        if (isEscaping || escapeCooldown > 0f)
        {
            EscapeFromPlayer();
            return;
        }

        // 5) 漫游/返回
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

    #region 食物交互逻辑

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
                    // 重新设置漫游中心和目标
                    wanderCenter = transform.position;
                    SetNewWanderTarget();
                }
                break;
        }
    }

    private void DetectAndReactToFood()
    {
        // 检测范围内的食物
        Collider[] colliders = Physics.OverlapSphere(transform.position, foodDetectionRadius);

        foreach (var col in colliders)
        {
            var food = col.GetComponent<FoodWorld>();
            if (food == null) continue; // 只对 FoodWorld 做反应

            // 根据性格决定是否靠近食物
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
                return; // 找到一个即可
            }
        }
    }

    private void StartApproachFood(Transform food)
    {
        targetFood = food;
        foodState = FoodState.Approaching;

        // 取消其他状态
        isEscaping = false;
        escapeCooldown = 0f;
        isReturning = false;

        Debug.Log($"{gameObject.name} 开始向食物移动");
    }

    private void MoveTowardsFood()
    {
        if (targetFood == null)
        {
            foodState = FoodState.Idle;
            return;
        }

        // 移动向食物
        Vector3 targetPos = targetFood.position;
        targetPos.y = transform.position.y; // 保持同一水平面

        transform.position = Vector3.MoveTowards(transform.position,
                                                targetPos,
                                                moveSpeed * Time.deltaTime);

        // 面向食物
        Vector3 lookDir = (targetPos - transform.position).normalized;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(lookDir, Vector3.up)
                             * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 5f * Time.deltaTime);
        }

        // 检查是否到达食物
        if (Vector3.Distance(transform.position, targetPos) < 1.5f)
        {
            // 开始吃食物
            foodState = FoodState.Eating;
            foodTimer = eatDuration;

            // 销毁食物
            if (targetFood != null)
            {
                Destroy(targetFood.gameObject);
                targetFood = null;
                Debug.Log($"{gameObject.name} 开始吃食物");
            }
        }
    }

    #endregion

    /// <summary>外部调用：令动物进入昏迷</summary>
    public void Stun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
        isEscaping = false;
        escapeCooldown = 0f;
        isReturning = false;
        isAttracted = false;

        // 重置食物状态
        foodState = FoodState.Idle;
        targetFood = null;
        foodTimer = 0f;
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

        // 重置食物状态
        foodState = FoodState.Idle;
        targetFood = null;
        foodTimer = 0f;
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

            // 取消食物交互
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, foodDetectionRadius);
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

/// <summary>动物性格枚举</summary>
public enum Temperament { Neutral, Fearful, Hostile }