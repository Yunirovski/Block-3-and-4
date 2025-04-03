using UnityEngine;
using UnityEngine.Events;

public class AnimalEvent : MonoBehaviour
{
    // 定义一个接受图片路径参数的 UnityEvent
    [System.Serializable]
    public class PhotoEvent : UnityEvent<string> { }

    // 在 Inspector 中可以给这个事件添加不同的响应
    public PhotoEvent onDetected;

    // 当该动物被检测到时调用
    public void TriggerEvent(string photoPath)
    {
        if (onDetected != null)
        {
            onDetected.Invoke(photoPath);
        }
    }
}