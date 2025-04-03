using UnityEngine;
using UnityEngine.Events;

public class AnimalEvent : MonoBehaviour
{
    // ����һ������ͼƬ·�������� UnityEvent
    [System.Serializable]
    public class PhotoEvent : UnityEvent<string> { }

    // �� Inspector �п��Ը�����¼���Ӳ�ͬ����Ӧ
    public PhotoEvent onDetected;

    // ���ö��ﱻ��⵽ʱ����
    public void TriggerEvent(string photoPath)
    {
        if (onDetected != null)
        {
            onDetected.Invoke(photoPath);
        }
    }
}