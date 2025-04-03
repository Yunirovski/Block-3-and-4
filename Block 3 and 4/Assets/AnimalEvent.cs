using UnityEngine;
using UnityEngine.Events;

public class AnimalEvent : MonoBehaviour
{
    [Tooltip("为当前动物指定一个唯一名称，用于照片分类")]
    public string animalName;

    [System.Serializable]
    public class PhotoEvent : UnityEvent<string> { }

    // 通过 Inspector 配置的额外事件（例如播放动画、声音等）
    public PhotoEvent onDetected;

    /// <summary>
    /// 当该动物被检测到时调用该方法
    /// </summary>
    /// <param name="photoPath">照片的保存路径</param>
    public void TriggerEvent(string photoPath)
    {
        // 调用额外事件
        if (onDetected != null)
        {
            onDetected.Invoke(photoPath);
        }

        // 将照片添加到照片集管理器中，归类到当前动物下
        if (PhotoCollectionManager.Instance != null)
        {
            PhotoCollectionManager.Instance.AddPhoto(animalName, photoPath);
        }

        Debug.Log($"{animalName} detected with photo: {photoPath}");
    }
}