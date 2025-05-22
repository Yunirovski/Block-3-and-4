// Assets/Scripts/Systems/ConsumableManagerExtensions.cs
using UnityEngine;

/// <summary>
/// 为ConsumableManager类提供扩展方法
/// </summary>
public static class ConsumableManagerExtensions
{
    /// <summary>
    /// 获取指定食物类型的当前数量
    /// </summary>
    /// <param name="manager">ConsumableManager实例</param>
    /// <param name="foodType">食物类型</param>
    /// <returns>该类型食物的当前数量</returns>
    public static int GetFoodCount(this ConsumableManager manager, FoodType foodType)
    {
        // 获取食物数组的反射字段（如果没有公共API）
        var foodField = typeof(ConsumableManager).GetField("food", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
            
        if (foodField != null)
        {
            int[] foodArray = foodField.GetValue(manager) as int[];
            if (foodArray != null && foodArray.Length > (int)foodType)
            {
                return foodArray[(int)foodType];
            }
        }
        
        // 如果无法通过反射获取，则返回-1表示未知
        return -1;
    }
    
    /// <summary>
    /// 获取指定食物类型的最大容量
    /// </summary>
    /// <param name="manager">ConsumableManager实例</param>
    /// <param name="foodType">食物类型</param>
    /// <returns>该类型食物的最大容量</returns>
    public static int GetFoodCapacity(this ConsumableManager manager, FoodType foodType)
    {
        // 直接使用公共字段
        return manager.foodCap;
    }
}
