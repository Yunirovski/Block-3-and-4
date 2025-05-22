using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialInventoryUI : MonoBehaviour
{
    [Header("Refs")] public CanvasGroup canvasGroup;
    [Tooltip("顺时针拖 6 张 Image")] public List<Image> slotImages;

    [Header("Style")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color lockedColor = new(0.3f, 0.3f, 0.3f, 0.6f);
    public float normalScale = 1f;
    public float highlightScale = 1.25f;

    bool[] unlocked = new bool[0];
    public int CurrentIndex { get; private set; } = -1;

    /* ────────── 外部接口 ────────── */
    /// <param name="states">各槽解锁布尔</param>
    /// <param name="defaultIdx">希望初始高亮的槽 (-1 = 自动取第一个解锁槽)</param>
    public void SetUnlockedStates(bool[] states, int defaultIdx = -1)
    {
        unlocked = states;
        CurrentIndex = DecideStartIndex(defaultIdx);
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

    /* ────────── 内部 ────────── */
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

    void UpdateVisual()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            bool unlock = unlocked.Length == slotImages.Count && unlocked[i];
            bool sel = (i == CurrentIndex);

            slotImages[i].color = unlock
                                    ? (sel ? highlightColor : normalColor)
                                    : lockedColor;
            slotImages[i].transform.localScale =
                Vector3.one * (sel ? highlightScale : normalScale);
        }
    }
}
