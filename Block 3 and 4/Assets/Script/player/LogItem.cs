using UnityEngine;

[CreateAssetMenu(menuName = "Items/LogItem")]
public class LogItem : BaseItem
{
    public GameObject logPanel;   // 指向日志面板对象

    public override void OnUse()
    {
        if (logPanel != null)
            logPanel.SetActive(true);
    }

    public override void OnUnready()
    {
        if (logPanel != null)
            logPanel.SetActive(false);
    }
}