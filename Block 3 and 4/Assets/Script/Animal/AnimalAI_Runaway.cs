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
    [Tooltip("NavMesh Agent停止距离")]
    public float stoppingDistance = 0.5f; // 减小默认停止距离
    [Tooltip("NavMesh采样距离")]
    public float sampleDistance = 5f;
    [Tooltip("是否自动旋转（建议关闭，使用自定义旋转）")]
    public bool useAgentRotation = false;
    [Tooltip("食物交互的检测距离")]
    public float foodInteractionDistance = 2.5f;
    [Tooltip("强制设置NavMeshAgent半径（0=自动）")]
    public float forceAgentRadius = 0f;

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
    private NavMeshAgent agent;
    private bool hasValidPath = false;

    void Start()
    {
        // 获取或添加NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            Debug.Log($"{gameObject.name}: 自动添加NavMeshAgent组件");
        }

        // 配置NavMeshAgent
        ConfigureNavMeshAgent();

        wanderCenter = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 确保起始位置在NavMesh上
        ValidateStartPosition();

        SetNewWanderTarget();
    }

    void ConfigureNavMeshAgent()
    {
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = useAgentRotation;
        agent.updateUpAxis = true; // 保持在NavMesh表面
        agent.autoBraking = true;

        // 设置合理的转向速度
        agent.angularSpeed = 180f;
        agent.acceleration = 8f;

        // 重要：设置合理的Agent尺寸以避免碰撞问题
        if (forceAgentRadius > 0f)
        {
            agent.radius = forceAgentRadius;
        }
        else if (agent.radius == 0.5f) // 如果是默认值，调整它
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // 根据实际碰撞体设置Agent半径
                agent.radius = Mathf.Max(col.bounds.size.x, col.bounds.size.z) * 0.4f; // 稍小于实际尺寸
                agent.height = col.bounds.size.y;
                Debug.Log($"{gameObject.name}: 设置NavMeshAgent - radius: {agent.radius:F2}, height: {agent.height:F2}");
            }
        }
    }

    void ValidateStartPosition()
    {
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, sampleDistance, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{gameObject.name}: 不在NavMesh上，尝试寻找最近的NavMesh位置");

            // 尝试在更大范围内找到NavMesh
            if (NavMesh.SamplePosition(transform.position, out hit, sampleDistance * 3, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Debug.Log($"{gameObject.name}: 移动到最近的NavMesh位置 {hit.position}");
            }
            else
            {
                Debug.LogError($"{gameObject.name}: 无法找到有效的NavMesh位置！请检查NavMesh烘焙");
            }
        }
    }

    void Update()
    {
        // 检查Agent是否有效
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning($"{gameObject.name}: NavMeshAgent无效或不在NavMesh上");
            return;
        }

        // 1) 昏迷中：只计时
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                agent.isStopped = false; // 恢复移动
            }
            else
            {
                agent.isStopped = true; // 停止移动
            }
            return;
        }

        // 2) 吸引中：移动向attractTarget
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

        // 3) 食物交互优先级高于逃跑和漫游
        HandleFoodInteraction();
        if (foodState != FoodState.Idle)
        {
            return;
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

    #region NavMesh移动系统

    /// <summary>
    /// 使用NavMesh移动到指定位置
    /// </summary>
    private bool MoveToPosition(Vector3 targetPos, float speed)
    {
        if (agent == null || !agent.isOnNavMesh) return false;

        // 寻找最近的有效NavMesh位置
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
            Debug.LogWarning($"{gameObject.name}: 无法在目标位置 {targetPos} 找到有效的NavMesh");
            return false;
        }
    }

    /// <summary>
    /// 处理动物旋转（如果不使用Agent自动旋转）
    /// </summary>
    private void HandleRotation(Vector3 targetPos)
    {
        if (useAgentRotation) return;

        Vector3 direction = (targetPos - transform.position);
        direction.y = 0; // 只在水平面旋转

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up)
                                       * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    /// <summary>
    /// 检查是否到达目标位置
    /// </summary>
    private bool HasReachedDestination()
    {
        if (agent == null || !agent.hasPath || agent.pathPending) return false;

        // 更宽松的到达判断
        return agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    /// <summary>
    /// 获取随机的NavMesh位置
    /// </summary>
    private bool GetRandomNavMeshPosition(Vector3 center, float radius, out Vector3 result)
    {
        result = Vector3.zero;

        for (int i = 0; i < 10; i++) // 最多尝试10次
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
                    wanderCenter = transform.position;
                    SetNewWanderTarget();
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

        Debug.Log($"{gameObject.name} 开始向食物移动");
    }

    private void MoveTowardsFood()
    {
        if (targetFood == null)
        {
            foodState = FoodState.Idle;
            return;
        }

        // 设置更小的停止距离以便接近食物
        float originalStopDistance = agent.stoppingDistance;
        agent.stoppingDistance = 0.1f; // 更小的停止距离

        MoveToPosition(targetFood.position, moveSpeed);
        HandleRotation(targetFood.position);

        // 检查是否到达食物 - 考虑碰撞体积
        float distanceToFood = Vector3.Distance(transform.position, targetFood.position);

        // 考虑动物和食物的碰撞体积
        float animalRadius = agent.radius;
        Collider foodCollider = targetFood.GetComponent<Collider>();
        float foodRadius = foodCollider != null ? foodCollider.bounds.size.magnitude * 0.5f : 0.5f;
        float totalRadius = animalRadius + foodRadius + 0.5f; // 添加额外缓冲

        bool nearFood = distanceToFood <= totalRadius;
        bool reachedDestination = HasReachedDestination();
        bool stoppedMoving = agent.velocity.magnitude < 0.1f && !agent.pathPending;
        bool stuckNearFood = distanceToFood < totalRadius * 1.5f && agent.velocity.magnitude < 0.1f;

        if (nearFood || reachedDestination || stoppedMoving || stuckNearFood)
        {
            foodState = FoodState.Eating;
            foodTimer = eatDuration;
            agent.isStopped = true; // 停止移动开始吃
            agent.stoppingDistance = originalStopDistance; // 恢复原始停止距离

            if (targetFood != null)
            {
                Destroy(targetFood.gameObject);
                targetFood = null;
                Debug.Log($"{gameObject.name} 开始吃食物 - 距离: {distanceToFood:F2}m, 需要距离: {totalRadius:F2}m, " +
                         $"动物半径: {animalRadius:F2}m, 食物半径: {foodRadius:F2}m, " +
                         $"到达目标: {reachedDestination}, 停止移动: {stoppedMoving}, 卡住: {stuckNearFood}");
            }
        }

        // 如果长时间无法到达食物，放弃
        if (!nearFood && !reachedDestination && Vector3.Distance(transform.position, targetFood.position) > foodDetectionRadius)
        {
            Debug.LogWarning($"{gameObject.name} 食物太远，放弃追踪");
            foodState = FoodState.Idle;
            agent.stoppingDistance = originalStopDistance;
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

        foodState = FoodState.Idle;
        targetFood = null;
        foodTimer = 0f;

        if (agent != null) agent.isStopped = true;
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
            // 如果找不到有效位置，延长当前的等待时间
            wanderTimer = Random.Range(minWanderTime, maxWanderTime);
            Debug.LogWarning($"{gameObject.name}: 无法找到有效的漫游目标");
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

        // 尝试找到逃跑目标的有效NavMesh位置
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

        // 显示NavMesh路径
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

/// <summary>动物性格枚举</summary>
public enum Temperament { Neutral, Fearful, Hostile }