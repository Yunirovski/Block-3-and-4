using UnityEngine;
using TMPro;
using System.Reflection;

/// <summary>
/// 在 Start() 时，把旧脚本里任何叫 scoreText / debugText / resultText / detectText
/// 的 TMP_Text 字段自动指向 GameUIHub 里的对应引用。
/// </summary>
public class UITextAutoLink : MonoBehaviour
{
    void Start()
    {
        var hub = GameUIHub.Instance;
        if (hub == null) return;

        // 找到此物体所有 MonoBehaviour，挨个补引用
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;

            TryAssign(mb, "scoreText", hub.scoreText);
            TryAssign(mb, "debugText", hub.debugText);
            TryAssign(mb, "resultText", hub.detectText);   // CameraItem 用的是 resultText
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
