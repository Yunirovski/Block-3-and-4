using UnityEngine;

[CreateAssetMenu(menuName = "Items/MapItem")]
public class MapItem : BaseItem
{
    public GameObject mapPanel;   // 指向地图面板对象

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