using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public List<Image> slotImages;      // 4����λ�ı���Image����б�
    public Color normalColor;           // δѡ��ʱ����ɫ
    public Color highlightColor;        // ѡ��ʱ�ĸ�����ɫ
    public GameObject readyIcon;        // Сͼ�꣬��ʾ׼��ʹ��״̬

    // ������index����λ
    public void HighlightSlot(int index)
    {
        for (int i = 0; i < slotImages.Count; i++)
            slotImages[i].color = (i == index) ? highlightColor : normalColor;
    }

    // ����׼��״̬ͼ����ʾ/����
    public void SetReadyState(bool ready)
    {
        if (readyIcon != null)
            readyIcon.SetActive(ready);
    }
}