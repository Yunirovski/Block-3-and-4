// Assets/Scripts/ScoreManager.cs
using UnityEngine;
using TMPro;

/// <summary>
/// 只负责在 UI 上实时显示「总星星数」。  
/// 真正的星星统计逻辑由 ProgressionManager 维护；
/// ProgressionManager 每次更新都会调用 SetStars()。
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    [Tooltip("TextMeshPro 组件：显示 Stars: xx")]
    [SerializeField] private TMP_Text scoreText;

    private int totalStars;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>ProgressionManager 调用：设定最新总星星数。</summary>
    public void SetStars(int stars)
    {
        totalStars = Mathf.Max(0, stars);
        UpdateText();
    }

    /// <summary>可选：某些地方想做 +1 弹窗时直接加。</summary>
    public void AddStars(int delta)
    {
        totalStars = Mathf.Max(0, totalStars + delta);
        UpdateText();
    }

    private void UpdateText()
    {
        if (scoreText != null)
            scoreText.text = $"Stars: {totalStars}";
    }
}
