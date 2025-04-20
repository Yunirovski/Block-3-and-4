using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("物品父容器和 UI 引用")]
    public Transform itemHolder;              // 生成的物品模型父物体
    public InventoryUI inventoryUI;           // 底部栏 UI 管理脚本

    [Header("场景 UI 文本 (调试 / 检测)")]
    public TMP_Text debugTextTMP;             // 拖入 DebugText TMP
    public TMP_Text detectTextTMP;            // 拖入 DetectText TMP

    [Header("切换动画设置")]
    public Animator itemAnimator;             // Animator 控制器
    public string switchTrigger = "SwitchItem"; // Trigger 名

    [Header("可用物品列表 (按索引对号入座)")]
    public List<BaseItem> availableItems;     // 0=Camera 1=Log 2=Map 3=Food

    private BaseItem currentItem;             // 当前物品
    private GameObject currentModel;          // 当前展示模型
    private bool isReady = false;             // 是否准备使用
    private int pendingIndex = -1;            // 等待切换索引

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
        // 数字键 1‑4 切换
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

        // 左键使用（需准备）
        if (isReady && Input.GetMouseButtonDown(0) && currentItem != null)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
                currentItem.OnUse();
        }
    }

    private void PlaySwitchAnimation()
    {
        if (itemAnimator != null)
            itemAnimator.SetTrigger(switchTrigger);
        else
            OnSwitchAnimationComplete();
    }

    // Animation Event 调用
    public void OnSwitchAnimationComplete()
    {
        inventoryUI.HighlightSlot(pendingIndex);

        if (currentItem != null) currentItem.OnDeselect();
        if (currentModel != null) Destroy(currentModel);

        currentItem = availableItems[pendingIndex];

        // 占位模型（可替换正式预制）
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.name = currentItem.itemName + "_Model";

        currentItem.OnSelect(currentModel);

        // 若是 CameraItem，注入相机与 UI
        if (currentItem is CameraItem camItem)
        {
            camItem.Init(Camera.main);
            camItem.InitUI(debugTextTMP, detectTextTMP);
        }
    }
}