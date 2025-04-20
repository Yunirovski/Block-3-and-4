using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public List<Image> slotImages;      // 4个槽位的背景Image组件列表
    public Color normalColor;           // 未选中时的颜色
    public Color highlightColor;        // 选中时的高亮颜色
    public GameObject readyIcon;        // 小图标，表示准备使用状态

    // 高亮第index个槽位
    public void HighlightSlot(int index)
    {
        for (int i = 0; i < slotImages.Count; i++)
            slotImages[i].color = (i == index) ? highlightColor : normalColor;
    }

    // 设置准备状态图标显示/隐藏
    public void SetReadyState(bool ready)
    {
        if (readyIcon != null)
            readyIcon.SetActive(ready);
    }
}