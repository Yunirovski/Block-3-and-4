using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("物品父容器和 UI 引用")]
    public Transform itemHolder;              // 生成物品模型的父物体
    public InventoryUI inventoryUI;           // 底部栏 UI 管理脚本

    [Header("场景 UI 文本 (调试 / 检测)")]
    public TMP_Text debugTextTMP;             // 拖入调试文本
    public TMP_Text detectTextTMP;            // 拖入检测结果文本

    [Header("切换动画设置")]
    public Animator itemAnimator;             // Animator
    public string switchTrigger = "SwitchItem";

    [Header("可用物品列表 (0=Camera 1=Log 2=Map 3=Food)")]
    public List<BaseItem> availableItems;

    private BaseItem currentItem;
    private GameObject currentModel;
    private bool isReady;
    private int pendingIndex = -1;

    private void Start()
    {
        if (availableItems.Count > 0)
        {
            pendingIndex = 0;
            inventoryUI.HighlightSlot(0);
            PlaySwitchAnimation();
        }
    }

    private void Update()
    {
        // 数字键 1‑4 切换物品
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
                    PlaySwitchAnimation();
                }
                return;
            }
        }

        // 右键准备 / 取消
        if (Input.GetMouseButtonDown(1) && currentItem != null)
        {
            isReady = !isReady;
            if (isReady) currentItem.OnReady(); else currentItem.OnUnready();
            inventoryUI.SetReadyState(isReady);
        }

        // 左键使用（准备状态）
        if (isReady && Input.GetMouseButtonDown(0) && currentItem != null)
        {
            if (!EventSystem.current.IsPointerOverGameObject()) currentItem.OnUse();
        }
    }

    private void PlaySwitchAnimation()
    {
        if (itemAnimator) itemAnimator.SetTrigger(switchTrigger);
        else OnSwitchAnimationComplete();
    }

    // Animation Event 调用
    public void OnSwitchAnimationComplete()
    {
        inventoryUI.HighlightSlot(pendingIndex);

        if (currentItem != null) currentItem.OnDeselect();
        if (currentModel != null) Destroy(currentModel);

        currentItem = availableItems[pendingIndex];

        // 生成占位模型
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.name = currentItem.itemName + "_Model";

        currentItem.OnSelect(currentModel);

        // 切换到相机物品时注入引用
        if (currentItem is CameraItem cam)
        {
            cam.Init(Camera.main);
            cam.InitUI(debugTextTMP, detectTextTMP);
        }
    }
}