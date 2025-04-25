using UnityEngine;
using TMPro;

/// <summary>
/// 单点收集场景里所有“文字状态”引用，让其他旧脚本在运行时按需来这里取。  
/// 挂在场景里唯一一个 GameObject（建议命名 UIRoot）。
/// </summary>
public class GameUIHub : MonoBehaviour
{
    public static GameUIHub Instance { get; private set; }

    [Header("Common Text References")]
    public TMP_Text scoreText;     // 计分板
    public TMP_Text debugText;     // 调试/提示
    public TMP_Text detectText;    // 动物检测结果
    public TMP_Text quotaText;     // 右上配额★（如需）
    public TMP_Text totalText;     // 右上总收藏★

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }
}
