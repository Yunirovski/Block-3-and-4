// Assets/Scripts/Animal/PigeonBehavior.cs
using UnityEngine;
using UnityEngine.AI;

public class PigeonBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // 鸽子飞行速度稍快
    public float wanderRadius = 40f;
    public float minWanderTime = 2f;
    public float maxWanderTime = 5f;

    [Header("Flight Settings")]
    [Tooltip("最低飞行高度")]
    public float minFlightHeight = 8f;
    [Tooltip("最高飞行高度")]
    public float maxFlightHeight = 15f;
    [Tooltip("飞行高度变化速度")]
    public float heightChangeSpeed = 2f;
    [Tooltip("地面检测射线长度")]
    public float groundCheckDistance = 50f;
    [Tooltip("飞行时是否避开障碍物")]
    public bool avoidObstacles = true;
    [Tooltip("障碍物检测距离")]
    public float obstacleDetectionDistance = 3f;

    [Header("Escape Settings")]
    public float escapeSpeed = 8f; // 鸽子逃跑时飞得更快
    public float detectionRadius = 15f; // 鸽子更敏感
    public float safeDistance = 20f; // 飞行时保持更远距离
    public float forcedEscapeDuration = 8f;

    [Header("Landing & Feeding Settings")]
    [Tooltip("动物性格决定靠近/回避玩家的方式")]
    public Temperament temperament = Temperament.Fearful; // 鸽子默认胆小
    [Tooltip("动物可以侦测食物的半径")]
    public float foodDetectionRadius = 25f; // 鸽子视野好，检测距离更远
    [Tooltip("Fearful/Hostile 判定玩家距离的阈值")]
    public float playerSafeDistance = 20f;
    [Tooltip("吃食物的持续时间（秒）")]
    public float eatDuration = 3f; // 鸽子吃得比较快
    [Tooltip("吃完后在地面停留的时间（秒）")]
    public float graceDuration = 5f; // 消化时间
    [Tooltip("起飞过程用时（秒）")]
    public float takeoffDuration = 2f;

    [Header("NavMesh Settings")]
    [Tooltip("NavMesh Agent停止距离")]
    public float stoppingDistance = 0.3f; // 鸽子停止距离更小
    [Tooltip("NavMesh采样距离")]
    public float sampleDistance = 8f;
    [Tooltip("强制设置NavMeshAgent半径")]
    public float forceAgentRadius = 0.3f; // 鸽子体型小

    [Header("Model Settings")]
    public Vector3 modelRotationOffset;

    // —— 飞行状态 —— 
    private enum FlightState { Flying, Landing, OnGround, TakingOff }
    private FlightState flightState = FlightState.Flying;
    private float currentFlightHeight = 10f;
    private float targetFlightHeight = 10f;
    private float takeoffTimer = 0f;

    // —— 昏迷状态 —— 
    private bool isStunned = false;
    private float stunTimer = 0f;

    // —— 吸引状态 —— 
    private bool isAttracted = false;
    private float attractTimer = 0f;
    private Transform attractTarget = null;

    // —— 食物交互状态 —— 
    private enum FoodState { Idle, Approaching, Landing, Eating, Grace }
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

    // —— 地面检测 ——
    private float groundHeight = 0f;
    private bool hasGroundReference = false;

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

        // 初始化飞行状态
        InitializeFlight();

        SetNewWanderTarget();
    }

    void ConfigureNavMeshAgent()
    {
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false; // 鸽子自己控制旋转
        agent.updateUpAxis = false; // 鸽子需要3D移动
        agent.autoBraking = true;

        // 设置合理的转向速度
        agent.angularSpeed = 360f; // 鸽子转向更灵活
        agent.acceleration = 12f; // 鸽子加速更快

        // 设置鸽子的Agent尺寸
        agent.radius = forceAgentRadius;
        agent.height = 0.5f; // 鸽子高度小

        Debug.Log($"{gameObject.name}: 设置NavMeshAgent - radius: {agent.radius:F2}, height: {agent.height:F2}");
    }

    void InitializeFlight()
    {
        // 检测地面高度
        UpdateGroundHeight();

        // 设置初始飞行高度
        currentFlightHeight = Random.Range(minFlightHeight, maxFlightHeight);
        targetFlightHeight = currentFlightHeight;

        // 设置鸽子到飞行高度
        Vector3 pos = transform.position;
        pos.y = groundHeight + currentFlightHeight;
        transform.position = pos;

        flightState = FlightState.Flying;

        Debug.Log($"{gameObject.name}: 初始化飞行，地面高度: {groundHeight:F2}, 飞行高度: {currentFlightHeight:F2}");
    }

    void UpdateGroundHeight()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance))
        {
            groundHeight = hit.point.y;
            hasGroundReference = true;
        }
        else
        {
            // 如果检测不到地面，使用NavMesh的Y坐标
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(transform.position, out navHit, sampleDistance, NavMesh.AllAreas))
            {
                groundHeight = navHit.position.y;
                hasGroundReference = true;
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

        // 更新飞行状态
        UpdateFlightState();

        // 1) 昏迷中：只计时
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                agent.isStopped = false; // 恢复移动

                // 昏迷结束后重新飞起来
                if (flightState == FlightState.OnGround)
                {
                    StartTakeoff();
                }
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

    #region 飞行状态管理

    void UpdateFlightState()
    {
        UpdateGroundHeight(); // 持续更新地面高度

        switch (flightState)
        {
            case FlightState.Flying:
                UpdateFlying();
                break;
            case FlightState.Landing:
                UpdateLanding();
                break;
            case FlightState.OnGround:
                // 在地面时不需要特别处理
                break;
            case FlightState.TakingOff:
                UpdateTakeoff();
                break;
        }
    }

    void UpdateFlying()
    {
        // 随机改变目标飞行高度
        if (Random.Range(0f, 1f) < 0.005f) // 0.5%的概率每帧
        {
            targetFlightHeight = Random.Range(minFlightHeight, maxFlightHeight);
        }

        // 平滑调整到目标高度
        currentFlightHeight = Mathf.MoveTowards(currentFlightHeight, targetFlightHeight, heightChangeSpeed * Time.deltaTime);

        // 设置实际位置
        Vector3 pos = transform.position;
        float targetY = groundHeight + currentFlightHeight;
        pos.y = Mathf.MoveTowards(pos.y, targetY, heightChangeSpeed * Time.deltaTime);
        transform.position = pos;

        // 障碍物检测
        if (avoidObstacles)
        {
            AvoidObstacles();
        }
    }

    void UpdateLanding()
    {
        // 降落到地面
        Vector3 pos = transform.position;
        float targetY = groundHeight + 0.2f; // 稍微离地面一点
        pos.y = Mathf.MoveTowards(pos.y, targetY, heightChangeSpeed * 2f * Time.deltaTime); // 降落快一些
        transform.position = pos;

        // 检查是否着陆
        if (Mathf.Abs(pos.y - targetY) < 0.1f)
        {
            flightState = FlightState.OnGround;
            Debug.Log($"{gameObject.name}: 已着陆");
        }
    }

    void UpdateTakeoff()
    {
        takeoffTimer -= Time.deltaTime;

        // 起飞过程
        Vector3 pos = transform.position;
        float targetY = groundHeight + currentFlightHeight;
        pos.y = Mathf.MoveTowards(pos.y, targetY, heightChangeSpeed * Time.deltaTime);
        transform.position = pos;

        // 检查是否完成起飞
        if (takeoffTimer <= 0f || Mathf.Abs(pos.y - targetY) < 0.1f)
        {
            flightState = FlightState.Flying;
            agent.isStopped = false; // 恢复移动能力
            Debug.Log($"{gameObject.name}: 起飞完成");
        }
    }

    void StartLanding()
    {
        flightState = FlightState.Landing;
        agent.isStopped = true; // 降落时停止水平移动
        Debug.Log($"{gameObject.name}: 开始降落");
    }

    void StartTakeoff()
    {
        flightState = FlightState.TakingOff;
        takeoffTimer = takeoffDuration;
        targetFlightHeight = Random.Range(minFlightHeight, maxFlightHeight);
        Debug.Log($"{gameObject.name}: 开始起飞");
    }

    void AvoidObstacles()
    {
        // 前方障碍物检测
        Vector3 forward = transform.forward;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, forward, out hit, obstacleDetectionDistance))
        {
            if (!hit.collider.CompareTag("Ground") && !hit.collider.CompareTag("Food"))
            {
                // 遇到障碍物，尝试向上飞
                targetFlightHeight = Mathf.Min(maxFlightHeight, targetFlightHeight + 2f);
            }
        }
    }

    #endregion

    #region NavMesh移动系统（修改为支持3D飞行）

    /// <summary>
    /// 使用NavMesh移动到指定位置（鸽子版本 - 支持飞行高度）
    /// </summary>
    private bool MoveToPosition(Vector3 targetPos, float speed)
    {
        if (agent == null || !agent.isOnNavMesh) return false;

        // 对于飞行状态，只使用XZ平面的NavMesh
        Vector3 navTarget = targetPos;
        navTarget.y = groundHeight; // 投影到地面进行导航

        // 寻找最近的有效NavMesh位置
        NavMeshHit hit;
        if (NavMesh.SamplePosition(navTarget, out hit, sampleDistance, NavMesh.AllAreas))
        {
            agent.speed = speed;
            agent.SetDestination(hit.position);
            hasValidPath = agent.hasPath;

            // 如果在飞行状态，手动设置Y坐标
            if (flightState == FlightState.Flying)
            {
                Vector3 pos = transform.position;
                pos.y = groundHeight + currentFlightHeight;
                transform.position = pos;
            }

            return true;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: 无法在目标位置 {targetPos} 找到有效的NavMesh");
            return false;
        }
    }

    /// <summary>
    /// 处理鸽子旋转
    /// </summary>
    private void HandleRotation(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position);
        direction.y = 0; // 鸽子主要看水平方向

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up)
                                       * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime); // 鸽子转向更快
        }
    }

    /// <summary>
    /// 检查是否到达目标位置（只考虑水平距离）
    /// </summary>
    private bool HasReachedDestination()
    {
        if (agent == null || !agent.hasPath || agent.pathPending) return false;

        // 只考虑水平距离
        return agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    /// <summary>
    /// 获取随机的NavMesh位置（飞行版本）
    /// </summary>
    private bool GetRandomNavMeshPosition(Vector3 center, float radius, out Vector3 result)
    {
        result = Vector3.zero;

        for (int i = 0; i < 10; i++) // 最多尝试10次
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            randomPoint.y = groundHeight; // 投影到地面

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

    #region 食物交互逻辑（鸽子版本）

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
            case FoodState.Landing:
                // 等待降落完成
                if (flightState == FlightState.OnGround)
                {
                    foodState = FoodState.Eating;
                    foodTimer = eatDuration;
                    Debug.Log($"{gameObject.name}: 着陆完成，开始吃食物");
                }
                break;
            case FoodState.Eating:
                foodTimer -= Time.deltaTime;
                if (foodTimer <= 0f)
                {
                    foodState = FoodState.Grace;
                    foodTimer = graceDuration;
                    Debug.Log($"{gameObject.name}: 吃完食物，开始消化");
                }
                break;
            case FoodState.Grace:
                foodTimer -= Time.deltaTime;
                if (foodTimer <= 0f)
                {
                    foodState = FoodState.Idle;
                    wanderCenter = transform.position;

                    // 鸽子消化完后起飞
                    StartTakeoff();

                    Debug.Log($"{gameObject.name}: 消化完成，准备起飞");
                }
                break;
        }
    }

    private void DetectAndReactToFood()
    {
        // 只有在飞行状态才检测食物
        if (flightState != FlightState.Flying) return;

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

        Debug.Log($"{gameObject.name} 发现食物，开始接近");
    }

    private void MoveTowardsFood()
    {
        if (targetFood == null)
        {
            foodState = FoodState.Idle;
            return;
        }

        // 移动到食物上方
        Vector3 foodPosition = targetFood.position;
        MoveToPosition(foodPosition, moveSpeed);
        HandleRotation(foodPosition);

        // 检查是否到达食物上方
        float horizontalDistance = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(foodPosition.x, 0, foodPosition.z)
        );

        if (horizontalDistance <= agent.radius + 1f) // 到达食物上方
        {
            foodState = FoodState.Landing;
            StartLanding();

            // 销毁食物
            if (targetFood != null)
            {
                Destroy(targetFood.gameObject);
                targetFood = null;
                Debug.Log($"{gameObject.name}: 到达食物位置，开始降落");
            }
        }

        // 如果食物太远，放弃
        if (Vector3.Distance(transform.position, foodPosition) > foodDetectionRadius * 1.5f)
        {
            Debug.LogWarning($"{gameObject.name}: 食物太远，放弃追踪");
            foodState = FoodState.Idle;
        }
    }

    #endregion

    /// <summary>外部调用：令鸽子进入昏迷</summary>
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

        // 鸽子昏迷时掉到地面
        if (flightState == FlightState.Flying)
        {
            StartLanding();
        }

        if (agent != null) agent.isStopped = true;
    }

    /// <summary>外部调用：令鸽子被法杖吸引</summary>
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

        // 确保鸽子在飞行状态
        if (flightState == FlightState.OnGround)
        {
            StartTakeoff();
        }

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

            // 鸽子受惊时飞到更高
            if (flightState == FlightState.Flying)
            {
                targetFlightHeight = maxFlightHeight;
            }
            else if (flightState == FlightState.OnGround)
            {
                StartTakeoff();
            }
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

        // 漫游范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(c, wanderRadius);

        // 检测范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // 食物检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, foodDetectionRadius);

        // 飞行高度范围
        if (hasGroundReference)
        {
            Gizmos.color = Color.cyan;
            Vector3 minPos = new Vector3(transform.position.x, groundHeight + minFlightHeight, transform.position.z);
            Vector3 maxPos = new Vector3(transform.position.x, groundHeight + maxFlightHeight, transform.position.z);
            Gizmos.DrawLine(minPos, maxPos);
            Gizmos.DrawWireSphere(minPos, 0.5f);
            Gizmos.DrawWireSphere(maxPos, 0.5f);
        }

        // 障碍物检测
        if (avoidObstacles)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, transform.forward * obstacleDetectionDistance);
        }

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

        // 半透明范围显示
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawSphere(c, wanderRadius);
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, foodDetectionRadius);

        // 方向指示
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}