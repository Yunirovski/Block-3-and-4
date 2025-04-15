using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TMP_Text scoreText;  // ������ʾ�ܻ��ֵ� UI �ı�
    private int totalScore = 0;                     // ��ǰ�ܻ���

    private void Awake()
    {
        // ����ģʽ��ȷ��ֻ��һ�� ScoreManager ʵ��
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
    /// ���ӻ��֣������� UI ��ʾ
    /// </summary>
    /// <param name="points">������ӵĻ�����</param>
    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreText();
        Debug.Log("Added score: " + points + " Current total: " + totalScore);
    }

    /// <summary>
    /// ���� UI �ı���ʾ
    /// </summary>
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + totalScore;
        }
    }
}
