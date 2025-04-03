using UnityEngine;
using UnityEngine.Events;

public class AnimalEvent : MonoBehaviour
{
    [Tooltip("Ϊ��ǰ����ָ��һ��Ψһ���ƣ�������Ƭ����")]
    public string animalName;

    [System.Serializable]
    public class PhotoEvent : UnityEvent<string> { }

    // ͨ�� Inspector ���õĶ����¼������粥�Ŷ����������ȣ�
    public PhotoEvent onDetected;

    /// <summary>
    /// ���ö��ﱻ��⵽ʱ���ø÷���
    /// </summary>
    /// <param name="photoPath">��Ƭ�ı���·��</param>
    public void TriggerEvent(string photoPath)
    {
        // ���ö����¼�
        if (onDetected != null)
        {
            onDetected.Invoke(photoPath);
        }

        // ����Ƭ��ӵ���Ƭ���������У����ൽ��ǰ������
        if (PhotoCollectionManager.Instance != null)
        {
            PhotoCollectionManager.Instance.AddPhoto(animalName, photoPath);
        }

        Debug.Log($"{animalName} detected with photo: {photoPath}");
    }
}