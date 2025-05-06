// Assets/Scripts/Inventory/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    [Header("Item & UI")]
    public Transform itemHolder;
    public RadialInventoryUI radialUI;
    public TMP_Text debugTextTMP;
    public TMP_Text detectTextTMP;
    public Canvas mainHUDCanvas;
    public Canvas cameraHUDCanvas;

    [Header("Animator (可选)")]
    public Animator itemAnimator;
    public string switchTrigger = "SwitchItem";

    [Header("Item List (Cam→Food→Hook→Board→Gun→Wand)")]
    public List<BaseItem> availableItems; // 必须长度=6

    BaseItem currentItem;
    GameObject currentModel;
    int currentIndex;
    int pendingIndex;
    bool ringOpen;

    void Start() => EquipSlot(0);

    void Update()
    {
        HandleRing();
        if (!ringOpen)
        {
            HandleNumberKeys();
            HandleUse();
        }
        // Q 交给 CameraItem 自己处理，OnUse/HandleUse 负责左键
        if (currentItem is CameraItem cam) cam.HandleInput();
    }

    void HandleRing()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ringOpen = true;
            radialUI.SetUnlockedStates(BuildUnlockArray(), currentIndex);
            radialUI.Show();
        }
        else if (Input.GetKeyUp(KeyCode.I))
        {
            int sel = radialUI.CurrentIndex;
            ringOpen = false;
            radialUI.Hide();

            if (sel == 1) RefreshSlot1List();
            if (sel >= 0 && sel != currentIndex) BeginSwitch(sel);
        }

        if (ringOpen)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) radialUI.Step(+1);
            else if (scroll < -0.01f) radialUI.Step(-1);
        }
    }

    void HandleNumberKeys()
    {
        var unlocked = BuildUnlockArray();
        for (int i = 0; i < 6; i++)
        {
            if (!unlocked[i]) continue;
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i != currentIndex)
            {
                BeginSwitch(i);
                return;
            }
        }
    }

    void HandleUse()
    {
        if (currentItem == null) return;

        bool onUI = EventSystem.current.IsPointerOverGameObject();
        bool allow = !(currentItem is CameraItem) ? !onUI : true;
        if (Input.GetMouseButtonDown(0) && allow)
            currentItem.OnUse();
    }

    void BeginSwitch(int idx)
    {
        pendingIndex = idx;
        if (itemAnimator) itemAnimator.SetTrigger(switchTrigger);
        OnSwitchAnimationComplete();
    }
    public void OnSwitchAnimationComplete() => EquipSlot(pendingIndex);

    void EquipSlot(int idx)
    {
        currentItem?.OnUnready();
        currentItem?.OnDeselect();
        if (currentModel) Destroy(currentModel);

        currentIndex = idx;
        currentItem = availableItems[idx];

        currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentModel.transform.SetParent(itemHolder, false);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.name = currentItem.itemName + "_Model";

        currentItem.OnSelect(currentModel);
        currentItem.OnReady();

        if (currentItem is CameraItem cam)
        {
            cam.Init(Camera.main, mainHUDCanvas, cameraHUDCanvas, debugTextTMP, detectTextTMP);
        }
        else if (currentItem is FoodItem food)
        {
            food.debugText = debugTextTMP;
        }

        debugTextTMP?.SetText($"切换到 {currentItem.itemName}");
    }

    void RefreshSlot1List()
    {
        var list = InventoryCycler.GetSlot3List();
        if (list.Count == 0) return;
        if (!list.Contains(availableItems[1]))
            availableItems[1] = list[0];
    }

    bool[] BuildUnlockArray()
    {
        var pm = ProgressionManager.Instance;
        return new[]
        {
            true,
            true,
            pm && pm.HasGrapple,
            pm && pm.HasSkateboard,
            pm && pm.HasDartGun,
            pm && pm.HasMagicWand
        };
    }
}
