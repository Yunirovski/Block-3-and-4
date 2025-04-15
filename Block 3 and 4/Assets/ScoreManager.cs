using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TMP_Text scoreText;  // 用于显示总积分的 UI 文本
    private int totalScore = 0;                     // 当前总积分

    private void Awake()
    {
        // 单例模式，确保只有一个 ScoreManager 实例
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 增加积分，并更新 UI 显示
    /// </summary>
    /// <param name="points">本次添加的积分数</param>
    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreText();
        Debug.Log("Added score: " + points + " Current total: " + totalScore);
    }

    /// <summary>
    /// 更新 UI 文本显示
    /// </summary>
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + totalScore;
        }
    }
}
