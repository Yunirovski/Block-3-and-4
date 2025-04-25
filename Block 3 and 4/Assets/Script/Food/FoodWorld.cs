using UnityEngine;

/// <summary>
/// 场景中的食物实体：挂在预制体上，自动销毁，供动物检测。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FoodWorld : MonoBehaviour
{
    [Tooltip("此食物的类型，由 FoodItem.prefab 上设置")]
    public FoodType foodType;

    [Tooltip("食物在场景中存在的最长时间（秒）")]
    public float lifetime = 300f;

    private float timer;

    void Start()
    {
        // 确保 Collider 是 Trigger，用于检测动物
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
