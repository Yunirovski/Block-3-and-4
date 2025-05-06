// Assets/Scripts/UI/PopupController.cs
using UnityEngine;
using TMPro;

/// <summary>
/// ���򵯴�����ʵ��������� Show(msg)����ʾ�ı����� lifetime ����Ի١�<br/>
/// �����û�� UI ���󣬱����ű�����������룻Inspector �пɲ�ָ�� prefab��
/// </summary>
public class PopupController : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float lifetime = 3f;

    /// <summary>��ʾһ����Ϣ���ɶ�ε����ظ����ı���</summary>
    public void Show(string msg)
    {
        if (messageText != null)
            messageText.text = msg;

        // ��û�� Text �����Ҳ���ֳ�Ĭ
        CancelInvoke(nameof(AutoDestroy));
        Invoke(nameof(AutoDestroy), lifetime);
    }

    private void AutoDestroy() => Destroy(gameObject);
}
