using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 场景中挂在动物根节点上的脚本：
/// • 保存物种唯一键 (animalName) & 是否彩蛋  
/// • 在被拍照时由 CameraItem 调用 <see cref="TriggerEvent"/>  
///   – 自动把星星汇报给 ProgressionManager  
///   – 把照片路径交给 PhotoCollectionManager  
///   – 触发自定义 UnityEvent（做 UI 动画等）
/// </summary>
public class AnimalEvent : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("唯一物种键（建议英文 ID），用于统计 & 解锁")]
    public string animalName;

    [Tooltip("若为 TRUE：额外 +1 星，上限 5★")]
    public bool isEasterEgg;

    // —— 供外部 UI 订阅 —— //
    [System.Serializable] public class PhotoEvent : UnityEvent<string /*path*/, int /*stars*/> { }
    public PhotoEvent onDetected;

    /// <summary>
    /// CameraItem 在完成评分后调用。  
    /// 会把 rawStars 调整为最终星级（彩蛋 +1，限幅），然后：<br/>
    /// 1. ProgressionManager.RegisterStars()<br/>
    /// 2. PhotoCollectionManager.AddPhoto()<br/>
    /// 3. 触发 onDetected 事件
    /// </summary>
    public void TriggerEvent(string photoPath, int rawStars)
    {
        int stars = isEasterEgg ? rawStars + 1 : rawStars;
        stars = Mathf.Clamp(stars, 1, isEasterEgg ? 5 : 4);

        // ① 汇报进度（星星 & 解锁）
        ProgressionManager.Instance?.RegisterStars(animalName, stars, isEasterEgg);

        // ② 不再直接添加到旧的PhotoCollectionManager
        // (由CameraItem通过新的PhotoCollectionManager处理)

        // ③ 触发自定义回调
        onDetected?.Invoke(photoPath, stars);

        Debug.Log($"{animalName} detected → {stars}★ ({photoPath})");
    }
}