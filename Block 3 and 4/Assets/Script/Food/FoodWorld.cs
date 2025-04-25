using UnityEngine;

/// <summary>
/// �����е�ʳ��ʵ�壺����Ԥ�����ϣ��Զ����٣��������⡣
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FoodWorld : MonoBehaviour
{
    [Tooltip("��ʳ������ͣ��� FoodItem.prefab ������")]
    public FoodType foodType;

    [Tooltip("ʳ���ڳ����д��ڵ��ʱ�䣨�룩")]
    public float lifetime = 300f;

    private float timer;

    void Start()
    {
        // ȷ�� Collider �� Trigger�����ڼ�⶯��
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
