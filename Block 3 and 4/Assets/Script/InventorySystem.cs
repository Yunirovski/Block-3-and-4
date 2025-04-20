using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySystem : MonoBehaviour
{
    [Header("��Ʒ��������UI����")]
    public Transform itemHolder;              // ������ɵ���Ʒģ�͵ĸ�����
    public InventoryUI inventoryUI;           // Inventory UI ����ű�����

    [Header("�л���������")]
    public Animator itemAnimator;             // �����л������� Animator
    public string switchTrigger = "SwitchItem"; // Animator �д����л��Ĳ�����

    [Header("������Ʒ�б�")]
    public List<BaseItem> availableItems;     // ������Ʒ ScriptableObject ���б�

    private BaseItem currentItem;             // ��ǰѡ�е���Ʒ
    private GameObject currentModel;          // ��ǰչʾ��ģ��
    private bool isReady = false;             // �Ƿ���׼��ʹ��״̬
    private int pendingIndex = -1;            // ���л�������Ʒ�������ȴ�������ɺ��л���

    private void Start()
    {
        // ����ʱ������Ʒ�������һ����Ʒ���л�����
        if (availableItems.Count > 0)
        {
            pendingIndex = 0;
            inventoryUI.HighlightSlot(0);
            PlaySwitchAnimation();
        }
    }

    private void Update()
    {
        // ���ּ� 1-4 �л���Ʒ��
        for (int i = 0; i < availableItems.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (pendingIndex != i)
                {
                    pendingIndex = i;
                    inventoryUI.HighlightSlot(i);
                    isReady = false;
                    inventoryUI.SetReadyState(false);
                    PlaySwitchAnimation();  // �����л�����
                }
                return;
            }
        }

        // �Ҽ����л�׼��/ȡ��׼��״̬
        if (Input.GetMouseButtonDown(1) && currentItem != null)
        {
            isReady = !isReady;
            if (isReady) currentItem.OnReady(); else currentItem.OnUnready();
            inventoryUI.SetReadyState(isReady);
        }

        // ���������׼��״̬��ʹ����Ʒ
        if (isReady && Input.GetMouseButtonDown(0) && currentItem != null)
        {
            // ������ UI ʱ����
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                currentItem.OnUse();
            }
        }
    }

    /// <summary>
    /// �����л���������δ���� Animator����ֱ��ִ���л��߼�
    /// </summary>
    private void PlaySwitchAnimation()
    {
        if (itemAnimator != null)
        {
            itemAnimator.SetTrigger(switchTrigger);
        }
        else
        {
            OnSwitchAnimationComplete();
        }
    }

    /// <summary>
    /// �ڶ���ĩβͨ�� Animation Event ���ã�ִ��ģ���л�
    /// </summary>
    public void OnSwitchAnimationComplete()
    {
        // �л���ɺ���� UI ����
        inventoryUI.HighlightSlot(pendingIndex);

        // ���پ�ģ�Ͳ�������Ʒ��ȡ��ѡ��ص�
        // ���پ�ģ�Ͳ�������Ʒ��ȡ��ѡ��ص�
        if (currentItem != null) currentItem.OnDeselect();
        if (currentModel != null) Destroy(currentModel);

        // �л������л�����Ʒ
        currentItem = availableItems[pendingIndex];

        // ����ռλ����ģ�ͣ������滻Ϊ��ʽԤ�Ƽ���
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.name = currentItem.itemName + "_Model";

        // ������Ʒ��ѡ��ص�
        currentItem.OnSelect(currentModel);
    }
}