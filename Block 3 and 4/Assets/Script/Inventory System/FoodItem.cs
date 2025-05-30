// Assets/Scripts/Items/FoodItem.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/FoodItem")]
public class FoodItem : BaseItem
{
    [Header("Food Settings")]
    [Tooltip("Food prefab for throwing")]
    public GameObject foodPrefab;

    [Tooltip("Throw distance")]
    public float spawnDistance = 2f;

    [Tooltip("Throw force")]
    public float throwForce = 10f;

    [Header("Food Types")]
    [Tooltip("Food types we can use")]
    public List<FoodType> foodTypes = new List<FoodType>();

    [Tooltip("Food prefabs for each type")]
    public List<GameObject> foodPrefabs = new List<GameObject>();

    [Tooltip("Which food type we picked")]
    public int currentIndex = 0;

    // 运行时状态
    private Camera playerCamera;
    private Transform playerTransform;

    public override void OnSelect(GameObject model)
    {
        // 获取玩家相机和位置
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerTransform = playerCamera.transform;
        }

        // 更新UI显示当前食物类型
        if (UIManager.Instance != null && foodTypes.Count > 0 && currentIndex < foodTypes.Count)
        {
            UIManager.Instance.UpdateFoodTypeText(foodTypes[currentIndex]);
        }

        Debug.Log($"FoodItem选中，当前食物类型: {(foodTypes.Count > 0 ? foodTypes[currentIndex].ToString() : "无")}");
    }

    public override void OnReady()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText("左键投掷食物，Q键切换食物类型");
        }
    }

    public override void OnUse()
    {
        ThrowFood();
    }

    public override void HandleUpdate()
    {
        // Q键切换食物类型
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchFoodType();
        }
    }

    /// <summary>
    /// Change food type
    /// </summary>
    private void SwitchFoodType()
    {
        if (foodTypes.Count <= 1) return;

        currentIndex = (currentIndex + 1) % foodTypes.Count;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateFoodTypeText(foodTypes[currentIndex]);
            UIManager.Instance.UpdateCameraDebugText($"Changed to: {foodTypes[currentIndex]}");
        }

        Debug.Log($"Food type changed to: {foodTypes[currentIndex]}");
    }

    /// <summary>
    /// Throw food
    /// </summary>
    private void ThrowFood()
    {
        if (playerCamera == null || playerTransform == null)
        {
            Debug.LogError("FoodItem: Can't find player camera or position");
            return;
        }

        // Check if we have enough food
        if (ConsumableManager.Instance == null || !ConsumableManager.Instance.UseFood())
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText("No food left!");
            }
            Debug.Log("FoodItem: Not enough food");
            return;
        }

        // 确定要使用的预制体
        GameObject prefabToUse = GetCurrentFoodPrefab();
        if (prefabToUse == null)
        {
            Debug.LogError("FoodItem: Can't find good food prefab");
            return;
        }

        // Make throw position (in front of player)
        Vector3 spawnPosition = playerTransform.position + playerTransform.forward * spawnDistance;

        // Make the food
        GameObject thrownFood = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);

        // Make sure food has what it needs
        SetupThrownFood(thrownFood);

        // Make it fly
        ApplyThrowForce(thrownFood);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText($"Threw {foodTypes[currentIndex]}");
        }

        Debug.Log($"FoodItem: Threw food {foodTypes[currentIndex]} to {spawnPosition}");
    }

    /// <summary>
    /// Get the food prefab we want to use now
    /// </summary>
    private GameObject GetCurrentFoodPrefab()
    {
        // 优先使用foodPrefabs列表
        if (foodPrefabs.Count > 0 && currentIndex < foodPrefabs.Count && foodPrefabs[currentIndex] != null)
        {
            return foodPrefabs[currentIndex];
        }

        // 回退到通用foodPrefab
        if (foodPrefab != null)
        {
            return foodPrefab;
        }

        Debug.LogError("FoodItem: No good food prefab found");
        return null;
    }

    /// <summary>
    /// Set up the food we threw
    /// </summary>
    private void SetupThrownFood(GameObject thrownFood)
    {
        // Make sure it has Rigidbody (for moving around)
        Rigidbody rb = thrownFood.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = thrownFood.AddComponent<Rigidbody>();
            Debug.Log("FoodItem: Added Rigidbody to food");
        }

        // Set Rigidbody settings
        rb.mass = 0.5f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.useGravity = true;
        rb.isKinematic = false;

        // Make sure it has Collider (for hitting things)
        Collider col = thrownFood.GetComponent<Collider>();
        if (col == null)
        {
            // If no collider, add a ball collider
            SphereCollider sphereCol = thrownFood.AddComponent<SphereCollider>();
            sphereCol.radius = 0.5f;
            sphereCol.isTrigger = false;
            Debug.Log("FoodItem: Added SphereCollider to food");
        }
        else
        {
            // Make sure the collider works and is not a trigger
            col.enabled = true;
            col.isTrigger = false;
            Debug.Log($"FoodItem: Collider found and ready: {col.GetType().Name}");
        }

        // Make sure it has FoodWorld script (for animals to find it)
        FoodWorld foodWorld = thrownFood.GetComponent<FoodWorld>();
        if (foodWorld == null)
        {
            foodWorld = thrownFood.AddComponent<FoodWorld>();
            Debug.Log("FoodItem: Added FoodWorld to food");
        }

        // Set the food type
        if (foodTypes.Count > 0 && currentIndex < foodTypes.Count)
        {
            foodWorld.foodType = foodTypes[currentIndex];
        }

        // Set how long the food will stay
        foodWorld.lifetime = 300f; // 5 minutes then goes away
    }

    /// <summary>
    /// Make the food fly when we throw it
    /// </summary>
    private void ApplyThrowForce(GameObject thrownFood)
    {
        Rigidbody rb = thrownFood.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Make throw direction (where player looks, a bit up)
        Vector3 throwDirection = playerTransform.forward;
        throwDirection.y += 0.3f; // add some up
        throwDirection = throwDirection.normalized;

        // Push it!
        rb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);

        // Make it spin a little
        Vector3 randomTorque = new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f)
        );
        rb.AddTorque(randomTorque, ForceMode.VelocityChange);

        Debug.Log($"FoodItem: Made food fly with force {throwDirection * throwForce}");
    }
}