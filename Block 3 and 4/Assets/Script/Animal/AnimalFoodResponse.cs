using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// �����ʳ��ļ���Ӧ������ϲ��(FoodType)���Ը�(Temperament)ʵ��
/// Idle �� Approach �� Eat �� Grace ��״̬����
/// </summary>
public class AnimalFoodResponse : MonoBehaviour
{
    public List<FoodType> preferences;   // ϲ����ʳ�������б�
    public Temperament temperament;      // �Ը�Neutral/ Fearful/ Hostile
    public float detectionRadius = 20f;  // ����ʳ��뾶
    public float safeDistance = 15f;     // �� Fearful/Hostile �İ�ȫ������ֵ
    public float eatDuration = 5f;       // ��ʳʱ��
    public float graceDuration = 10f;    // ��ʳ����˳ʱ��

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
        // �����ƶ������� NavMesh��ֱ����Ŀ���ƶ�
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetFood.position,
            3f * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetFood.position) < 1f)
        {
            // �����ʼ��ʳ
            state = State.Eating;
            timer = eatDuration;
            Destroy(targetFood.gameObject);
            targetFood = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        // �ڱ༭���п��ӻ����뾶
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

/// <summary>
/// �Ը�ö�٣����� / ���� / �ж�
/// </summary>
public enum Temperament
{
    Neutral,
    Fearful,
    Hostile
}
