using UnityEngine;
using UnityEngine.Events;

public class AnimalEvent : MonoBehaviour
{
    [Tooltip("��������ƣ����ڱ�ʶ������")]
    public string animalName;

    [Tooltip("�ö��ﱻ�ĵ�ʱ��õĻ���")]
    public int scoreValue = 10;  // ���� Inspector ���޸ģ���ͬ�������ò�ͬ�Ļ���

    [System.Serializable]
    public class PhotoEvent : UnityEvent<string> { }

    [Tooltip("������ Inspector ������������Ӧ�¼������粥�Ŷ���������")]
    public PhotoEvent onDetected;

    /// <summary>
    /// ���ö��ﱻ��⵽ʱ���ø÷���
    /// </summary>
    /// <param name="photoPath">�����ͼƬ·��</param>
    public void TriggerEvent(string photoPath)
    {
        // ���� Inspector �����õ�������Ӧ�¼�
        if (onDetected != null)
        {
            onDetected.Invoke(photoPath);
        }

        // ����Ӧ������ӵ�����ϵͳ��
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        Debug.Log(animalName + " detected with photo: " + photoPath + ", earned score: " + scoreValue);
    }
}
