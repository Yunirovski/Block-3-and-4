using UnityEngine;
using TMPro;
using System.Reflection;

/// <summary>
/// �� Start() ʱ���Ѿɽű����κν� scoreText / debugText / resultText / detectText
/// �� TMP_Text �ֶ��Զ�ָ�� GameUIHub ��Ķ�Ӧ���á�
/// </summary>
public class UITextAutoLink : MonoBehaviour
{
    void Start()
    {
        var hub = GameUIHub.Instance;
        if (hub == null) return;

        // �ҵ����������� MonoBehaviour������������
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;

            TryAssign(mb, "scoreText", hub.scoreText);
            TryAssign(mb, "debugText", hub.debugText);
            TryAssign(mb, "resultText", hub.detectText);   // CameraItem �õ��� resultText
            TryAssign(mb, "detectText", hub.detectText);
        }
    }

    static void TryAssign(object target, string fieldName, TMP_Text value)
    {
        if (value == null) return;

        FieldInfo fi = target.GetType()
                             .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (fi != null && fi.FieldType == typeof(TMP_Text))
        {
            fi.SetValue(target, value);
        }
    }
}
