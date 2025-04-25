using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 动物对食物的简单响应：基于喜好(FoodType)和性格(Temperament)实现
/// Idle → Approach → Eat → Grace 的状态机。
/// </summary>
public class AnimalFoodResponse : MonoBehaviour
{
    public List<FoodType> preferences;   // 喜欢的食物类型列表
    public Temperament temperament;      // 性格：Neutral/ Fearful/ Hostile
    public float detectionRadius = 20f;  // 发现食物半径
    public float safeDistance = 15f;     // 对 Fearful/Hostile 的安全距离阈值
    public float eatDuration = 5f;       // 进食时长
    public float graceDuration = 10f;    // 进食后温顺时长

    enum State { Idle, Approaching, Eating, Grace }
    State state = State.Idle;

    private Transform targetFood;
    private float timer;

    void Update()
    {
        switch (state)
        {
            case State.Idle:
                DetectAndReact();
                break;

            case State.Approaching:
                MoveTowardsFood();
                break;

            case State.Eating:
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    state = State.Grace;
                    timer = graceDuration;
                }
                break;

            case State.Grace:
                timer -= Time.deltaTime;
                if (timer <= 0) state = State.Idle;
                break;
        }
    }

    void DetectAndReact()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var col in hits)
        {
            var fw = col.GetComponent<FoodWorld>();
            if (fw == null) continue;
            if (!preferences.Contains(fw.foodType)) continue;

            float distToPlayer = Vector3.Distance(
                transform.position,
                Camera.main.transform.position);

            switch (temperament)
            {
                case Temperament.Neutral:
                    StartApproach(fw.transform);
                    return;

                case Temperament.Fearful:
                    if (distToPlayer > safeDistance)
                        StartApproach(fw.transform);
                    return;

                case Temperament.Hostile:
                    if (distToPlayer <= safeDistance)
                        StartApproach(fw.transform);
                    return;
            }
        }
    }

    void StartApproach(Transform food)
    {
        targetFood = food;
        state = State.Approaching;
    }

    void MoveTowardsFood()
    {
        if (targetFood == null)
        {
            state = State.Idle;
            return;
        }
        // 简易移动：不做 NavMesh，直接向目标移动
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetFood.position,
            3f * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetFood.position) < 1f)
        {
            // 到达，开始进食
            state = State.Eating;
            timer = eatDuration;
            Destroy(targetFood.gameObject);
            targetFood = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        // 在编辑器中可视化检测半径
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

/// <summary>
/// 性格枚举：中立 / 害怕 / 敌对
/// </summary>
public enum Temperament
{
    Neutral,
    Fearful,
    Hostile
}
