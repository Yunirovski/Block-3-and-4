using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RadialInventoryUI : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup canvasGroup;
    [Tooltip("顺时针拖 6 张 Image")]
    public List<Image> slotImages;

    [Header("Style")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color lockedColor = new(0.3f, 0.3f, 0.3f, 0.6f);
    public float normalScale = 1f;
    public float highlightScale = 1.25f;

    [Header("Hover Effect")]
    public Color hoverColor = Color.cyan;
    public float hoverScale = 1.1f;

    bool[] unlocked = new bool[0];
    public int CurrentIndex { get; private set; } = -1;

    // 点击相关
    private List<Button> slotButtons = new List<Button>();
    private int hoveredIndex = -1;

    void Awake()
    {
        SetupButtons();
    }

    /* ────────── 设置按钮 ────────── */
    void SetupButtons()
    {
        // 清理现有按钮
        slotButtons.Clear();

        for (int i = 0; i < slotImages.Count; i++)
        {
            Image slotImage = slotImages[i];

            // 获取或添加 Button 组件
            Button button = slotImage.GetComponent<Button>();
            if (button == null)
            {
                button = slotImage.gameObject.AddComponent<Button>();
            }

            // 设置按钮属性
            button.targetGraphic = slotImage;
            button.transition = Selectable.Transition.None; // 我们自己处理视觉效果

            // 添加点击事件
            int slotIndex = i; // 捕获循环变量
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSlotClicked(slotIndex));

            // 添加鼠标悬停事件
            EventTrigger trigger = slotImage.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = slotImage.gameObject.AddComponent<EventTrigger>();
            }

            // 清除现有事件
            trigger.triggers.Clear();

            // 鼠标进入事件
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => OnSlotHoverEnter(slotIndex));
            trigger.triggers.Add(pointerEnter);

            // 鼠标离开事件
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => OnSlotHoverExit(slotIndex));
            trigger.triggers.Add(pointerExit);

            slotButtons.Add(button);
        }

        Debug.Log($"RadialInventoryUI: 设置了 {slotButtons.Count} 个可点击槽位");
    }

    /* ────────── 点击和悬停事件 ────────── */
    void OnSlotClicked(int slotIndex)
    {
        // 检查槽位是否解锁
        if (slotIndex >= 0 && slotIndex < unlocked.Length && unlocked[slotIndex])
        {
            CurrentIndex = slotIndex;
            UpdateVisual();

            Debug.Log($"RadialInventoryUI: 点击槽位 {slotIndex}");

            // 可以在这里触发选择完成事件，让 InventorySystem 知道
            // 或者让 InventorySystem 在下一帧检查 CurrentIndex 的变化
        }
        else
        {
            Debug.Log($"RadialInventoryUI: 槽位 {slotIndex} 未解锁，无法选择");
        }
    }

    void OnSlotHoverEnter(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < unlocked.Length && unlocked[slotIndex])
        {
            hoveredIndex = slotIndex;
            UpdateVisual();
        }
    }

    void OnSlotHoverExit(int slotIndex)
    {
        if (hoveredIndex == slotIndex)
        {
            hoveredIndex = -1;
            UpdateVisual();
        }
    }

    /* ────────── 外部接口 ────────── */
    /// <param name="states">各槽解锁布尔</param>
    /// <param name="defaultIdx">希望初始高亮的槽 (-1 = 自动取第一个解锁槽)</param>
    public void SetUnlockedStates(bool[] states, int defaultIdx = -1)
    {
        unlocked = states;
        CurrentIndex = DecideStartIndex(defaultIdx);
        hoveredIndex = -1; // 重置悬停状态
        UpdateButtonStates();
        UpdateVisual();
    }

    public void Show()
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        UpdateVisual();
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        hoveredIndex = -1; // 重置悬停状态
        // 不清空 CurrentIndex，让 InventorySystem 在 Hide() 前先读取
    }

    /// <summary>dir = +1 / -1 ：顺时针 / 逆时针</summary>
    public void Step(int dir)
    {
        if (unlocked.Length == 0) return;
        int idx = CurrentIndex;
        for (int i = 0; i < unlocked.Length; i++)
        {
            idx = (idx + dir + unlocked.Length) % unlocked.Length;
            if (unlocked[idx]) { CurrentIndex = idx; break; }
        }
        UpdateVisual();
    }

    /* ────────── 内部方法 ────────── */
    int DecideStartIndex(int wanted)
    {
        if (unlocked.Length != slotImages.Count) return -1;

        if (wanted >= 0 && wanted < unlocked.Length && unlocked[wanted])
            return wanted;                     // 当前槽已解锁 → 直接高亮

        // 否则回退到列表中第一个解锁槽
        for (int i = 0; i < unlocked.Length; i++)
            if (unlocked[i]) return i;
        return -1;
    }

    void UpdateButtonStates()
    {
        // 更新按钮的可交互状态
        for (int i = 0; i < slotButtons.Count && i < unlocked.Length; i++)
        {
            bool isUnlocked = unlocked[i];
            slotButtons[i].interactable = isUnlocked;
        }
    }

    void UpdateVisual()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            bool unlock = unlocked.Length == slotImages.Count && unlocked[i];
            bool sel = (i == CurrentIndex);
            bool hover = (i == hoveredIndex);

            Color targetColor;
            float targetScale;

            if (!unlock)
            {
                // 未解锁：灰色
                targetColor = lockedColor;
                targetScale = normalScale;
            }
            else if (sel)
            {
                // 选中：高亮色
                targetColor = highlightColor;
                targetScale = highlightScale;
            }
            else if (hover)
            {
                // 悬停：悬停色
                targetColor = hoverColor;
                targetScale = hoverScale;
            }
            else
            {
                // 正常：普通色
                targetColor = normalColor;
                targetScale = normalScale;
            }

            slotImages[i].color = targetColor;
            slotImages[i].transform.localScale = Vector3.one * targetScale;
        }
    }

    /* ────────── 公共方法供外部调用 ────────── */
    /// <summary>
    /// 检查是否有槽位被点击（供 InventorySystem 轮询使用）
    /// </summary>
    public bool HasSlotSelection()
    {
        return CurrentIndex >= 0;
    }

    /// <summary>
    /// 强制设置当前选中的槽位（用于外部同步）
    /// </summary>
    public void SetCurrentIndex(int index)
    {
        if (index >= 0 && index < unlocked.Length && unlocked[index])
        {
            CurrentIndex = index;
            UpdateVisual();
        }
    }

    /// <summary>
    /// 获取当前悬停的槽位索引
    /// </summary>
    public int GetHoveredIndex()
    {
        return hoveredIndex;
    }
}