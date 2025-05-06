// Assets/Scripts/UI/SimplePanels.cs
using UnityEngine;

/// <summary>
/// 极简开关面板：J 键切换 Log，M 键切换 Map。<br/>
/// 面板默认可在 Inspector 拖入；若为空则忽略对应键。
/// </summary>
public class SimplePanels : MonoBehaviour
{
    public GameObject logPanel;
    public GameObject mapPanel;

    /* ---------- 每帧监听 ---------- */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) && logPanel)
            Toggle(logPanel);

        if (Input.GetKeyDown(KeyCode.M) && mapPanel)
            Toggle(mapPanel);
    }

    static void Toggle(GameObject go) => go.SetActive(!go.activeSelf);
}
