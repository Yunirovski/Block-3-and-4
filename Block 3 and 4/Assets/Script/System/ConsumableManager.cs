// Assets/Scripts/Systems/ConsumableManager.cs
using UnityEngine;
using System;

/// <summary>
/// 全局消耗品管理：胶卷 1 种 + 食物 3 种（Meat / Leaves / Fruit）。<br/>
/// 兼容旧版本字段与 API，并新增分类计数与补满函数。  
/// </summary>
public class ConsumableManager : MonoBehaviour
{
    /* ---------- 单例 ---------- */
    public static ConsumableManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /* ---------- Inspector 参数 ---------- */
    [Header("Film")]
    [Tooltip("胶卷最大容量")]
    public int filmCap = 20;
    [Tooltip("游戏开始时的胶卷数量")]
    [SerializeField] private int filmStart = 20;

    [Header("Food (per type)")]
    [Tooltip("单一食物类型的最大数量")]
    public int foodCap = 10;                       // 等同“每种上限”
    [Tooltip("游戏开始时每种食物数量")]
    [SerializeField] private int foodStart = 10;

    /* ---------- 内部状态 ---------- */
    private int film;                              // 当前胶卷
    private int[] food;                            // 食物[0=Meat,1=Leaves,2=Fruit]

    /* ---------- 只读属性（兼容旧代码） ---------- */
    public int Film => film;

    /// <summary>旧版只关心“总食物数”，这里返回三种之和。</summary>
    public int Food
    {
        get
        {
            int total = 0;
            foreach (int n in food) total += n;
            return total;
        }
    }

    /* ---------- 事件 ---------- */
    /// <summary>旧版事件：任何消耗品发生变化都会触发。</summary>
    public event Action OnConsumableChanged;

    /// <summary>新版事件：胶卷变化 (current, max)。</summary>
    public Action<int, int> OnFilmChanged;
    /// <summary>新版事件：单种食物变化 (type, current, max)。</summary>
    public Action<FoodType, int, int> OnFoodChanged;

    /* ===================================================================== */
    /*                               公共 API                               */
    /* ===================================================================== */

    /* -- Film -- */
    public bool UseFilm(int amount = 1)
    {
        if (amount <= 0) { Debug.LogWarning($"UseFilm 非法参数: {amount}"); return false; }
        if (film < amount) return false;

        film -= amount;
        BroadcastFilm();
        return true;
    }

    /// <summary>便捷版：消费 1 张胶卷。</summary>
    public bool UseFilm() => UseFilm(1);

    public void AddFilm(int amount)
    {
        if (amount <= 0) { Debug.LogWarning($"AddFilm 非法参数: {amount}"); return; }
        film = Mathf.Clamp(film + amount, 0, filmCap);
        BroadcastFilm();
    }

    public void RefillFilm()
    {
        film = filmCap;
        BroadcastFilm();
    }

    /* -- Food -- */
    /// <summary>新版推荐：按类型消费。</summary>
    public bool UseFood(FoodType type, int amount = 1)
    {
        int idx = (int)type;
        if (amount <= 0) { Debug.LogWarning($"UseFood 非法参数: {amount}"); return false; }
        if (food[idx] < amount) return false;

        food[idx] -= amount;
        BroadcastFood(type);
        return true;
    }

    /// <summary>
    /// 旧接口保留：默认把请求算到 Fruit 里，保证编译通过。  
    /// 请尽快改为 <c>UseFood(FoodType,type)</c> 新版。
    /// </summary>
    [Obsolete("请改用 UseFood(FoodType type, int amount = 1)")]
    public bool UseFood(int amount = 1) => UseFood(FoodType.Fruit, amount);

    public void AddFood(FoodType type, int amount)
    {
        if (amount <= 0) { Debug.LogWarning($"AddFood 非法参数: {amount}"); return; }
        int idx = (int)type;
        food[idx] = Mathf.Clamp(food[idx] + amount, 0, foodCap);
        BroadcastFood(type);
    }

    /// <summary>一次性补满三种食物到上限。</summary>
    public void RefillAllFood()
    {
        for (int i = 0; i < food.Length; i++)
            food[i] = foodCap;

        BroadcastFood(FoodType.Meat);
        BroadcastFood(FoodType.Leaves);
        BroadcastFood(FoodType.Fruit);
    }

    /* ===================================================================== */
    /*                               初始化                                 */
    /* ===================================================================== */
    private void Start()
    {
        film = Mathf.Clamp(filmStart, 0, filmCap);

        int typeCount = System.Enum.GetValues(typeof(FoodType)).Length;
        food = new int[typeCount];
        for (int i = 0; i < typeCount; i++)
            food[i] = Mathf.Clamp(foodStart, 0, foodCap);

        BroadcastFilm();
        BroadcastFood(FoodType.Meat);
        BroadcastFood(FoodType.Leaves);
        BroadcastFood(FoodType.Fruit);
    }

    /* ---------- 内部广播 ---------- */
    private void BroadcastFilm()
    {
        OnFilmChanged?.Invoke(film, filmCap);
        OnConsumableChanged?.Invoke();
    }

    private void BroadcastFood(FoodType type)
    {
        OnFoodChanged?.Invoke(type, food[(int)type], foodCap);
        OnConsumableChanged?.Invoke();
    }
}
