using UnityEngine;

[CreateAssetMenu(menuName = "Items/MapItem")]
public class MapItem : BaseItem
{
    public GameObject mapPanel;   // ָ���ͼ������

    public override void OnUse()
    {
        if (mapPanel != null)
            mapPanel.SetActive(true);
    }

    public override void OnUnready()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);
    }
}