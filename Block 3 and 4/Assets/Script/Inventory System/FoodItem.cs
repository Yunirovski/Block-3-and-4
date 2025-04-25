// Assets/Scripts/Items/FoodItem.cs

using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ������ʳ����ߣ�
///   - ʹ�� [ / ] �л���ǰʳ������  
///   - ʹ��ʱ����������ǰ�� spawnDistance �״����ɶ�Ӧ prefab  
///   - �۳� ConsumableManager �е�ʳ������  
/// </summary>
[CreateAssetMenu(menuName = "Items/FoodItem")]
public class FoodItem : BaseItem
{
    [Header("Types & Prefabs (parallel lists)")]
    [Tooltip("ʳ�������б�")]
    public List<FoodType> foodTypes;
    [Tooltip("��Ӧ Prefab �б�˳��Ҫһһ��Ӧ foodTypes")]
    public List<GameObject> foodPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("�������ǰ������������ʳ��")]
    public float spawnDistance = 2f;

    [HideInInspector] public TMP_Text debugText;  // �� InventorySystem ע��

    private int currentIndex = 0;

    /// <summary>
    /// �л���ǰʳ������
    /// </summary>
    /// <param name="forward">true �е���һ����false �е���һ��</param>
    public void CycleFoodType(bool forward)
    {
        if (foodTypes == null || foodPrefabs == null ||
            foodTypes.Count == 0 || foodPrefabs.Count == 0 ||
            foodTypes.Count != foodPrefabs.Count)
        {
            Debug.LogError("FoodItem �л�ʧ�ܣ��б�δ��ȷ����");
            return;
        }

        currentIndex = (currentIndex + (forward ? 1 : -1) + foodTypes.Count) % foodTypes.Count;
        debugText?.SetText($"�л�ʳ��: {foodTypes[currentIndex]}");
        Debug.Log($"FoodItem: �л��� {foodTypes[currentIndex]} (index={currentIndex})");
    }

    public override void OnSelect(GameObject model)
    {
        // ѡ��ʱ��ʾ��ǰ����
        debugText?.SetText($"��ǰʳ��: {foodTypes[currentIndex]}");
    }

    public override void OnUse()
    {
        // �۳����
        if (!ConsumableManager.Instance.UseFood())
        {
            debugText?.SetText("û��ʳ���Ͷ�ţ�");
            return;
        }

        // ���������ǰ��λ��
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("FoodItem: �Ҳ��� Main Camera���޷�Ͷ��");
            return;
        }

        Vector3 spawnPos = cam.transform.position + cam.transform.forward * spawnDistance;

        // ʵ������Ӧ prefab
        GameObject prefab = foodPrefabs[currentIndex];
        if (prefab == null)
        {
            Debug.LogError($"FoodItem: foodPrefabs[{currentIndex}] δ��ֵ");
            return;
        }

        Instantiate(prefab, spawnPos, Quaternion.identity);
        debugText?.SetText($"Ͷ�� {foodTypes[currentIndex]} ��ǰ��{spawnDistance}�״�");
        Debug.Log($"FoodItem: �� {spawnPos} λ�������� {foodTypes[currentIndex]}");
    }
}
