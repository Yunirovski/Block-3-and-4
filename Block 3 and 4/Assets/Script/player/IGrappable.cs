// Assets/Scripts/Player/IGrappable.cs
using UnityEngine;

/// <summary>
/// 可钩住物体接口
/// </summary>
public interface IGrappable
{
    /// <summary>
    /// 是否可以被钩住
    /// </summary>
    bool CanBeGrappled();

    /// <summary>
    /// 获取最佳钩住点
    /// </summary>
    Vector3 GetBestGrapplePoint(Vector3 hookPosition);

    /// <summary>
    /// 钩爪附着时调用
    /// </summary>
    void OnGrappleAttach(Vector3 attachPoint);

    /// <summary>
    /// 钩爪脱离时调用
    /// </summary>
    void OnGrappleDetach();

    /// <summary>
    /// 获取表面强度（影响钩爪是否会脱落）
    /// </summary>
    float GetSurfaceStrength();
}