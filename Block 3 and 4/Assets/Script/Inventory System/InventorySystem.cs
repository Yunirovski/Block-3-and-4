using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("Item & UI References")]
    public Transform itemHolder;
    public InventoryUI inventoryUI;
    public TMP_Text debugTextTMP;    // make sure you drag your DebugText here
    public TMP_Text detectTextTMP;
    public Transform spawnPoint;     // make sure you drag your spawn point here

    [Header("Animation")]
    public Animator itemAnimator;
    public string switchTrigger = "SwitchItem";

    [Header("Slots (0=Camera,1=Log,2=Map,3=Food/Equip)")]
    public List<BaseItem> availableItems;

    BaseItem currentItem;
    GameObject currentModel;
    bool isReady;
    int pendingIndex = -1;

    void Start()
    {
        if (availableItems.Count > 3 && availableItems[3] != null)
            InventoryCycler.InitWith(availableItems[3]);

        if (availableItems.Count > 0)
        {
            pendingIndex = 0;
            inventoryUI.HighlightSlot(0);
            PlaySwitchAnimation();
        }
    }

    void Update()
    {
        HandleSlotSwitch();
        HandleReadyToggle();
        HandleUse();
    }

    void HandleSlotSwitch()
    {
        // 1) 数字键切槽
        for (int i = 0; i < availableItems.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (isReady)
                {
                    debugTextTMP.text = "请先收起当前物品！";
                    return;
                }
                if (pendingIndex == i) return;
                pendingIndex = i;
                inventoryUI.HighlightSlot(i);
                PlaySwitchAnimation();
                return;
            }
        }

        // 2) 槽3（索引3）特判：食物类型切换 + 道具循环
        if (pendingIndex == 3)
        {
            // —— 食物类型切换 ([ 和 ]) ——
            if (availableItems[3] is FoodItem foodItem)
            {
                if (Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    Debug.Log("InventorySystem: Detected [  key");
                    foodItem.CycleFoodType(false);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.RightBracket))
                {
                    Debug.Log("InventorySystem: Detected ]  key");
                    foodItem.CycleFoodType(true);
                    return;
                }
            }

            // —— Q/E 或 滚轮 切槽3内循环 —— 
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) CycleSlot3(true);
            else if (scroll < 0f) CycleSlot3(false);

            if (Input.GetKeyDown(KeyCode.Q)) CycleSlot3(false);
            if (Input.GetKeyDown(KeyCode.E)) CycleSlot3(true);
        }
    }

    void CycleSlot3(bool forward)
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;
        int cur = list.IndexOf(availableItems[3]);
        int next = (cur + (forward ? 1 : -1) + list.Count) % list.Count;
        availableItems[3] = list[next];
        debugTextTMP.text = $"切换至 {list[next].itemName}";
        PlaySwitchAnimation();
    }

    void HandleReadyToggle()
    {
        if (Input.GetMouseButtonDown(1) && currentItem != null)
        {
            isReady = !isReady;
            if (isReady) currentItem.OnReady();
            else currentItem.OnUnready();
            inventoryUI.SetReadyState(isReady);
        }
    }

    void HandleUse()
    {
        if (!isReady || currentItem == null) return;
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            currentItem.OnUse();
    }

    void PlaySwitchAnimation()
    {
        if (itemAnimator != null)
            itemAnimator.SetTrigger(switchTrigger);
        else
            OnSwitchAnimationComplete();
    }

    // 动画事件或无动画时直接调用
    public void OnSwitchAnimationComplete()
    {
        Debug.Log($"OnSwitchAnimationComplete: pendingIndex={pendingIndex}");
        inventoryUI.HighlightSlot(pendingIndex);

        if (currentItem != null) currentItem.OnDeselect();
        if (currentModel != null) Destroy(currentModel);

        currentItem = availableItems[pendingIndex];
        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.name = currentItem.itemName + "_Model";

        currentItem.OnSelect(currentModel);

        // 注入 FoodItem
        if (currentItem is FoodItem food)
        {
            
            food.debugText = debugTextTMP;
            Debug.Log("Injected spawnPoint & debugText into FoodItem");
            food.OnSelect(currentModel);
        }

        // 注入 CameraItem
        if (currentItem is CameraItem cam)
        {
            cam.Init(Camera.main);
            cam.InitUI(debugTextTMP, detectTextTMP);
        }
    }
}
