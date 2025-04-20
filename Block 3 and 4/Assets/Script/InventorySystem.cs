using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySystem : MonoBehaviour
{
    [Header("物品父容器和UI引用")]
    public Transform itemHolder;              // 存放生成的物品模型的父物体
    public InventoryUI inventoryUI;           // Inventory UI 管理脚本引用

    [Header("切换动画设置")]
    public Animator itemAnimator;             // 控制切换动画的 Animator
    public string switchTrigger = "SwitchItem"; // Animator 中触发切换的参数名

    [Header("可用物品列表")]
    public List<BaseItem> availableItems;     // 所有物品 ScriptableObject 的列表

    private BaseItem currentItem;             // 当前选中的物品
    private GameObject currentModel;          // 当前展示的模型
    private bool isReady = false;             // 是否处于准备使用状态
    private int pendingIndex = -1;            // 待切换到的物品索引（等待动画完成后切换）

    private void Start()
    {
        // 启动时若有物品，进入第一个物品的切换流程
        if (availableItems.Count > 0)
        {
            pendingIndex = 0;
            inventoryUI.HighlightSlot(0);
            PlaySwitchAnimation();
        }
    }

    private void Update()
    {
        // 数字键 1-4 切换物品槽
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
                    PlaySwitchAnimation();  // 播放切换动画
                }
                return;
            }
        }

        // 右键：切换准备/取消准备状态
        if (Input.GetMouseButtonDown(1) && currentItem != null)
        {
            isReady = !isReady;
            if (isReady) currentItem.OnReady(); else currentItem.OnUnready();
            inventoryUI.SetReadyState(isReady);
        }

        // 左键：仅在准备状态下使用物品
        if (isReady && Input.GetMouseButtonDown(0) && currentItem != null)
        {
            // 避免点击 UI 时触发
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                currentItem.OnUse();
            }
        }
    }

    /// <summary>
    /// 播放切换动画；若未配置 Animator，则直接执行切换逻辑
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
    /// 在动画末尾通过 Animation Event 调用，执行模型切换
    /// </summary>
    public void OnSwitchAnimationComplete()
    {
        // 切换完成后更新 UI 高亮
        inventoryUI.HighlightSlot(pendingIndex);

        // 销毁旧模型并调用物品的取消选择回调
        // 销毁旧模型并调用物品的取消选择回调
        if (currentItem != null) currentItem.OnDeselect();
        if (currentModel != null) Destroy(currentModel);

        // 切换到待切换的物品
        currentItem = availableItems[pendingIndex];

        // 生成占位方块模型（后续替换为正式预制件）
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.name = currentItem.itemName + "_Model";

        // 调用物品的选择回调
        currentItem.OnSelect(currentModel);
    }
}