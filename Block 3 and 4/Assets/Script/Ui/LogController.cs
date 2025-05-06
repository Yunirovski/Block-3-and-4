using UnityEngine;

public class LogController : MonoBehaviour
{
    public Canvas logCanvas;         // ÍÏ LogCanvas
    public GameObject tutorialTab;   // ÍÏ 4 ¸öÒ³Ç©¸ù
    public GameObject polarTab;
    public GameObject savannaTab;
    public GameObject jungleTab;

    int currentTab = 0;              // 0-3

    void Start() => logCanvas.enabled = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            bool open = !logCanvas.enabled;
            logCanvas.enabled = open;
            if (open) ShowTab(currentTab);
        }
    }

    public void ShowTab(int idx)
    {
        currentTab = idx;
        tutorialTab.SetActive(idx == 0);
        polarTab.SetActive(idx == 1);
        savannaTab.SetActive(idx == 2);
        jungleTab.SetActive(idx == 3);
    }
}
