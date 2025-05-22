// Assets/Scripts/Items/FoodItem.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ��һʳ�� ScriptableObject��ʹ��ʱ�����ǰ������Ԥ���岢�ۿ�档<br/>
/// ͬʱ�������ֶ� <c>foodTypes</c>/<c>foodPrefabs</c>/<c>currentIndex</c>
/// �Լ��� InventorySystem �ɴ��루��Զֻ�� 1 ��Ԫ�أ���
/// </summary>
[CreateAssetMenu(menuName = "Items/FoodItem")]
public class FoodItem : BaseItem
{
    /* ===================================================================== */
    /*                        ���� �������ã�����棩 ����                        */
    /* ===================================================================== */

    [Header("Prefab & Spawn")]
    [Tooltip("Ҫ���ɵ�ʳ��Ԥ����")]
    public GameObject foodPrefab;          // ����ֻ��Ҫ 1 ��Ԥ����

    [Tooltip("�����ǰ�����ɵľ��루�ף�")]
    public float spawnDistance = 2f;

    /* ===================================================================== */
    /*                       ���� ���ݾɴ���ġ��Žӡ� ����                         */
    /* ===================================================================== */

    // ���� ���ֶΣ����� Inspector ���۵����أ���������� ����
    [HideInInspector] public List<FoodType> foodTypes = new() { FoodType.Food };
    [HideInInspector] public List<GameObject> foodPrefabs = new();           // Awake ʱ�Զ����
    [HideInInspector] public int currentIndex = 0;                            // ��Զ = 0

    // ���� ScriptableObject �� Awake �������Ǳ����ã��ټ� OnEnable ������
    private void Awake() => EnsureCompatLists();
    private void OnEnable() => EnsureCompatLists();

    // ȷ�������б���������һ��Ԫ��
    private void EnsureCompatLists()
    {
        if (foodTypes.Count == 0) foodTypes.Add(FoodType.Food);

        if (foodPrefabs.Count == 0 && foodPrefab != null)
            foodPrefabs.Add(foodPrefab);
    }

    /* ===================================================================== */
    /*                          ���� BaseItem �ӿ� ����                           */
    /* ===================================================================== */

    public override void OnSelect(GameObject model)
    {
        // ���� HUD ����ʾΨһ����
        UIManager.Instance.UpdateFoodTypeText(FoodType.Food);
    }

    public override void OnUse()
    {
        Debug.Log("FoodItem.OnUse: ��ʼ����ʳ��");

        // ��� ConsumableManager
        if (ConsumableManager.Instance == null)
        {
            Debug.LogError("FoodItem.OnUse: ConsumableManager.Instance Ϊ��");
            UIManager.Instance?.UpdateCameraDebugText("����: ����Ʒ������δ�ҵ�");
            return;
        }

        // �ۿ��
        if (!ConsumableManager.Instance.UseFood())
        {
            Debug.Log("FoodItem.OnUse: û��ʳ����");
            UIManager.Instance?.UpdateCameraDebugText("û��ʳ��ʣ�࣡");
            return;
        }

        // ������
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("FoodItem.OnUse: Main Camera not found.");
            UIManager.Instance?.UpdateCameraDebugText("����: �Ҳ��������");
            return;
        }

        // ���ʳ��Ԥ����
        if (foodPrefab == null)
        {
            Debug.LogError("FoodItem.OnUse: foodPrefab Ϊ�գ�����Inspector������ʳ��Ԥ����");
            UIManager.Instance?.UpdateCameraDebugText("����: ʳ��Ԥ����δ����");
            return;
        }

        // ��������λ��
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * spawnDistance;

        // ȷ��ʳ�ﲻ�������ڵ�������
        spawnPos.y = Mathf.Max(spawnPos.y, 0.5f);

        try
        {
            // ����ʳ��
            GameObject foodInstance = Instantiate(foodPrefab, spawnPos, Quaternion.identity);

            if (foodInstance != null)
            {
                Debug.Log($"FoodItem.OnUse: �ɹ�����ʳ����λ�� {spawnPos}");
                UIManager.Instance?.UpdateCameraDebugText($"��ǰ�� {spawnDistance}m ������ʳ��");
            }
            else
            {
                Debug.LogError("FoodItem.OnUse: ʳ��ʵ����ʧ��");
                UIManager.Instance?.UpdateCameraDebugText("����: ʳ������ʧ��");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FoodItem.OnUse: ����ʳ��ʱ�����쳣: {e.Message}");
            UIManager.Instance?.UpdateCameraDebugText($"����: {e.Message}");
        }
    }

    // ��ǰ���� [ / ] �л������ڲ�����Ҫ
    public override void HandleUpdate() { }
}
