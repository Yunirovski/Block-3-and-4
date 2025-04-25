using UnityEngine;
using TMPro;

/// <summary>
/// �����ռ����������С�����״̬�����ã��������ɽű�������ʱ����������ȡ��  
/// ���ڳ�����Ψһһ�� GameObject���������� UIRoot����
/// </summary>
public class GameUIHub : MonoBehaviour
{
    public static GameUIHub Instance { get; private set; }

    [Header("Common Text References")]
    public TMP_Text scoreText;     // �Ʒְ�
    public TMP_Text debugText;     // ����/��ʾ
    public TMP_Text detectText;    // ��������
    public TMP_Text quotaText;     // ����������裩
    public TMP_Text totalText;     // �������ղء�

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }
}
