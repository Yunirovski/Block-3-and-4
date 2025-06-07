// Assets/Scripts/Animal/PigeonBehavior.cs
using UnityEngine;

public class PigeonBehavior : MonoBehaviour
{
    [Header("Flight Settings")]
    public float flySpeed = 6f;
    public float flightHeight = 12f; // 固定飞行高度
    public float heightVariation = 3f; // 高度变化范围 ±3米

    [Header("Landing & Takeoff")]
    public float landingSpeed = 8f; // 降落速度
    public float takeoffSpeed = 6f; // 起飞速度

    [Header("Movement")]
    public float wanderRadius = 30f;
    public float changeDirectionTime = 5f; // 多久改变一次方向
    public float rotationSpeed = 2f;

    [Header("Food Behavior")]
    public float foodDetectionRadius = 8f; // 修改为8米
    public float eatDuration = 3f;
    public float restDuration = 4f;
    public Temperament temperament = Temperament.Neutral; // 修改为Neutral
    public float playerSafeDistance = 15f;

    [Header("Rest Position")]
    [Tooltip("鸟吃完食物后休息时的高度（从地面算起）")]
    public float restHeight = 0.1f; // 新增：可调节的休息高度，防止遁地

    [Header("Escape")]
    public float escapeSpeed = 10f;
    public float detectionRadius = 12f;
    public float escapeHeight = 18f; // 逃跑时的高度

    // 状态 - 新增了降落和起飞状态
    private enum State { Flying, Descending, Eating, Resting, Ascending }
    private State currentState = State.Flying;

    // 移动相关
    private Vector3 targetPosition;
    private Vector3 moveDirection;
    private float directionTimer;
    private float groundLevel;

    // 食物和降落相关
    private float stateTimer;
    private Transform targetFood;
    private Vector3 foodPosition; // 保存食物位置，防止食物被销毁后丢失位置
    private float startHeight; // 开始降落时的高度
    private float targetLandHeight; // 目标降落高度

    // 其他
    private Transform player;
    private Vector3 homePosition;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        homePosition = transform.position;

        // 找到地面高度
        FindGroundLevel();

        // 设置初始飞行位置
        Vector3 startPos = transform.position;
        startPos.y = groundLevel + flightHeight;
        transform.position = startPos;

        // 设置初始方向
        ChooseNewDirection();

        Debug.Log($"{name}: 开始飞行，高度 {flightHeight}m");
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Flying:
                HandleFlying();
                break;
            case State.Descending:
                HandleDescending();
                break;
            case State.Eating:
                HandleEating();
                break;
            case State.Resting:
                HandleResting();
                break;
            case State.Ascending:
                HandleAscending();
                break;
        }
    }

    void HandleFlying()
    {
        // 检查是否需要逃跑
        if (ShouldEscape())
        {
            HandleEscape();
            return;
        }

        // 检查食物
        if (CheckForFood())
        {
            return; // 如果开始降落吃食物，就不继续飞行逻辑
        }

        // 正常飞行
        FlyTowardsTarget();

        // 定期改变方向
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
        {
            ChooseNewDirection();
        }
    }

    void HandleDescending()
    {
        // 平滑降落到食物位置
        Vector3 currentPos = transform.position;

        // 水平移动到食物位置
        Vector3 horizontalTarget = new Vector3(foodPosition.x, currentPos.y, foodPosition.z);
        Vector3 horizontalDirection = (horizontalTarget - currentPos);
        horizontalDirection.y = 0;

        if (horizontalDirection.magnitude > 0.5f)
        {
            // 还没到食物上方，继续水平移动
            currentPos += horizontalDirection.normalized * flySpeed * Time.deltaTime;

            // 旋转朝向食物
            if (horizontalDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // 垂直降落
        if (currentPos.y > targetLandHeight + 0.2f)
        {
            currentPos.y -= landingSpeed * Time.deltaTime;
            currentPos.y = Mathf.Max(currentPos.y, targetLandHeight);
        }
        else
        {
            // 降落完成
            currentPos.y = targetLandHeight;
            currentState = State.Eating;
            stateTimer = eatDuration;

            // 现在才销毁食物
            if (targetFood != null)
            {
                Destroy(targetFood.gameObject);
                targetFood = null;
            }

            Debug.Log($"{name}: 降落完成，开始吃食物");
        }

        transform.position = currentPos;
    }

    void HandleEating()
    {
        // 保持在地面附近，防止遁地
        Vector3 currentPos = transform.position;
        currentPos.y = Mathf.Max(currentPos.y, groundLevel + 0.05f); // 确保不会低于地面
        transform.position = currentPos;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            currentState = State.Resting;
            stateTimer = restDuration;

            // 吃完后移动到休息高度
            Vector3 restPos = transform.position;
            restPos.y = groundLevel + restHeight;
            transform.position = restPos;

            Debug.Log($"{name}: 吃完了，在高度 {restHeight}m 处休息");
        }
    }

    void HandleResting()
    {
        // 关键修复：持续保持在休息高度，防止遁地
        Vector3 currentPos = transform.position;
        float targetRestHeight = groundLevel + restHeight;
        currentPos.y = targetRestHeight; // 强制保持在休息高度
        transform.position = currentPos;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            // 休息完毕，开始起飞
            StartTakeOff();
        }
    }

    void HandleAscending()
    {
        // 平滑起飞
        Vector3 currentPos = transform.position;
        float targetHeight = groundLevel + flightHeight;

        if (currentPos.y < targetHeight - 0.2f)
        {
            currentPos.y += takeoffSpeed * Time.deltaTime;
            currentPos.y = Mathf.Min(currentPos.y, targetHeight);
        }
        else
        {
            // 起飞完成
            currentPos.y = targetHeight;
            currentState = State.Flying;

            // 选择新的飞行方向
            ChooseNewDirection();

            Debug.Log($"{name}: 起飞完成，继续飞行");
        }

        transform.position = currentPos;
    }

    void FlyTowardsTarget()
    {
        // 计算移动
        Vector3 currentPos = transform.position;
        Vector3 toTarget = targetPosition - currentPos;

        // 只考虑水平距离
        Vector3 horizontalDirection = new Vector3(toTarget.x, 0, toTarget.z);

        // 如果接近目标，选择新方向
        if (horizontalDirection.magnitude < 3f)
        {
            ChooseNewDirection();
            return;
        }

        // 移动
        moveDirection = horizontalDirection.normalized;
        Vector3 newPos = currentPos + moveDirection * flySpeed * Time.deltaTime;

        // 保持飞行高度（添加一些随机变化）
        float randomHeight = flightHeight + Mathf.Sin(Time.time * 0.5f) * heightVariation * 0.3f;
        newPos.y = groundLevel + randomHeight;

        transform.position = newPos;

        // 旋转朝向移动方向
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void ChooseNewDirection()
    {
        // 选择一个随机方向，但倾向于围绕家的位置飞行
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0; // 只在水平面选择
        randomDirection.Normalize();

        // 如果距离家太远，倾向于飞回家
        Vector3 toHome = homePosition - transform.position;
        toHome.y = 0;

        if (toHome.magnitude > wanderRadius)
        {
            randomDirection = Vector3.Lerp(randomDirection, toHome.normalized, 0.7f).normalized;
        }

        targetPosition = transform.position + randomDirection * wanderRadius * 0.5f;
        directionTimer = changeDirectionTime + Random.Range(-1f, 1f);
    }

    bool CheckForFood()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, foodDetectionRadius);

        foreach (var col in colliders)
        {
            FoodWorld food = col.GetComponent<FoodWorld>();
            if (food == null) continue;

            // 检查玩家距离
            bool canApproach = true;
            if (player != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, player.position);

                switch (temperament)
                {
                    case Temperament.Fearful:
                        canApproach = distToPlayer > playerSafeDistance;
                        break;
                    case Temperament.Hostile:
                        canApproach = distToPlayer <= playerSafeDistance;
                        break;
                        // Neutral 总是可以接近
                }
            }

            if (canApproach)
            {
                StartDescending(food);
                return true;
            }
        }

        return false;
    }

    void StartDescending(FoodWorld food)
    {
        currentState = State.Descending;
        targetFood = food.transform;
        foodPosition = food.transform.position; // 保存食物位置

        // 记录当前高度和目标高度
        startHeight = transform.position.y;
        targetLandHeight = groundLevel + 0.1f; // 稍微离地面一点

        Debug.Log($"{name}: 发现食物，开始降落到 {foodPosition}");
    }

    void StartTakeOff()
    {
        currentState = State.Ascending;
        Debug.Log($"{name}: 开始起飞");
    }

    bool ShouldEscape()
    {
        if (player == null) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        return distance < detectionRadius;
    }

    void HandleEscape()
    {
        // 逃跑：飞离玩家
        Vector3 escapeDirection = (transform.position - player.position).normalized;
        escapeDirection.y = 0;

        Vector3 currentPos = transform.position;
        Vector3 newPos = currentPos + escapeDirection * escapeSpeed * Time.deltaTime;

        // 逃跑时平滑飞到更高处
        float targetEscapeHeight = groundLevel + escapeHeight;
        if (currentPos.y < targetEscapeHeight)
        {
            newPos.y += takeoffSpeed * 0.5f * Time.deltaTime; // 逃跑时起飞稍慢一些
            newPos.y = Mathf.Min(newPos.y, targetEscapeHeight);
        }
        else
        {
            newPos.y = targetEscapeHeight;
        }

        transform.position = newPos;

        // 面朝逃跑方向
        if (escapeDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(escapeDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
        }
    }

    void FindGroundLevel()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f))
        {
            groundLevel = hit.point.y;
        }
        else
        {
            groundLevel = transform.position.y - 10f; // 默认值
        }
    }

    // 外部接口
    public void Stun(float duration)
    {
        // 鸽子被击晕时平滑掉到地面
        if (currentState == State.Flying)
        {
            currentState = State.Descending;
            foodPosition = transform.position; // 在原地降落
            startHeight = transform.position.y;
            targetLandHeight = groundLevel + 0.1f;

            // 降落后进入休息状态
            stateTimer = duration;
        }
    }

    public void Attract(Transform target, float duration)
    {
        if (currentState == State.Flying && target != null)
        {
            targetPosition = target.position;
            directionTimer = duration;
        }
    }

    // 调试显示
    void OnDrawGizmosSelected()
    {
        // 漫游范围
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);

        // 检测范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // 食物检测
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, foodDetectionRadius);

        // 飞行高度
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            float ground = Application.isPlaying ? groundLevel : transform.position.y - 10f;
            Vector3 flightPos = new Vector3(transform.position.x, ground + flightHeight, transform.position.z);
            Gizmos.DrawWireSphere(flightPos, 1f);

            // 休息高度显示
            Gizmos.color = new Color(1f, 0.5f, 0f); // 橙色 (替代Color.orange)
            Vector3 restPos = new Vector3(transform.position.x, ground + restHeight, transform.position.z);
            Gizmos.DrawWireSphere(restPos, 0.5f);

            // 目标位置
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(targetPosition, 0.5f);

            // 如果在降落，显示食物位置
            if (currentState == State.Descending)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(foodPosition, 0.8f);
                Gizmos.DrawLine(transform.position, foodPosition);
            }
        }
    }
}