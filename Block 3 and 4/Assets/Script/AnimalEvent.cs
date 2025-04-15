using UnityEngine;
using UnityEngine.Events;

public class AnimalEvent : MonoBehaviour
{
    [Tooltip("动物的名称，用于标识及分类")]
    public string animalName;

    [Tooltip("该动物被拍到时获得的积分")]
    public int scoreValue = 10;  // 可在 Inspector 中修改，不同动物设置不同的积分

    [System.Serializable]
    public class PhotoEvent : UnityEvent<string> { }

    [Tooltip("可以在 Inspector 中增加其他响应事件，例如播放动画或声音")]
    public PhotoEvent onDetected;

    /// <summary>
    /// 当该动物被检测到时调用该方法
    /// </summary>
    /// <param name="photoPath">拍摄的图片路径</param>
    public void TriggerEvent(string photoPath)
    {
        // 触发 Inspector 中配置的其他响应事件
        if (onDetected != null)
        {
            onDetected.Invoke(photoPath);
        }

        // 将对应积分添加到积分系统中
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        Debug.Log(animalName + " detected with photo: " + photoPath + ", earned score: " + scoreValue);
    }
}
