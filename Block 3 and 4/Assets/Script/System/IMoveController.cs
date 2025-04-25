/// <summary>
/// 提供给装备调用的移动控制接口：允许动态修改角色行走与奔跑速度。
/// </summary>
public interface IMoveController
{
    /// <summary>
    /// 按 multiplier 调整角色基础速度（走/跑）。
    /// multiplier = 1 恢复原速。
    /// </summary>
    void ModifySpeed(float multiplier);
}
