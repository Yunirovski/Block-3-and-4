// Assets/Scripts/Systems/ConsumableManager.cs
using UnityEngine;
using System;

/// <summary>
/// ȫ������Ʒ�������� 1 �� + ʳ�� 3 �֣�Meat / Leaves / Fruit����<br/>
/// ���ݾɰ汾�ֶ��� API����������������벹��������  
/// </summary>
public class ConsumableManager : MonoBehaviour
{
    /* ---------- ���� ---------- */
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

    /* ---------- Inspector ���� ---------- */
    [Header("Film")]
    [Tooltip("�����������")]
    public int filmCap = 20;
    [Tooltip("��Ϸ��ʼʱ�Ľ�������")]
    [SerializeField] private int filmStart = 20;

    [Header("Food (per type)")]
    [Tooltip("��һʳ�����͵��������")]
    public int foodCap = 10;                       // ��ͬ��ÿ�����ޡ�
    [Tooltip("��Ϸ��ʼʱÿ��ʳ������")]
    [SerializeField] private int foodStart = 10;

    /* ---------- �ڲ�״̬ ---------- */
    private int film;                              // ��ǰ����
    private int[] food;                            // ʳ��[0=Meat,1=Leaves,2=Fruit]

    /* ---------- ֻ�����ԣ����ݾɴ��룩 ---------- */
    public int Film => film;

    /// <summary>�ɰ�ֻ���ġ���ʳ�����������ﷵ������֮�͡�</summary>
    public int Food
    {
        get
        {
            int total = 0;
            foreach (int n in food) total += n;
            return total;
        }
    }

    /* ---------- �¼� ---------- */
    /// <summary>�ɰ��¼����κ�����Ʒ�����仯���ᴥ����</summary>
    public event Action OnConsumableChanged;

    /// <summary>�°��¼�������仯 (current, max)��</summary>
    public Action<int, int> OnFilmChanged;
    /// <summary>�°��¼�������ʳ��仯 (type, current, max)��</summary>
    public Action<FoodType, int, int> OnFoodChanged;

    /* ===================================================================== */
    /*                               ���� API                               */
    /* ===================================================================== */

    /* -- Film -- */
    public bool UseFilm(int amount = 1)
    {
        if (amount <= 0) { Debug.LogWarning($"UseFilm �Ƿ�����: {amount}"); return false; }
        if (film < amount) return false;

        film -= amount;
        BroadcastFilm();
        return true;
    }

    /// <summary>��ݰ棺���� 1 �Ž���</summary>
    public bool UseFilm() => UseFilm(1);

    public void AddFilm(int amount)
    {
        if (amount <= 0) { Debug.LogWarning($"AddFilm �Ƿ�����: {amount}"); return; }
        film = Mathf.Clamp(film + amount, 0, filmCap);
        BroadcastFilm();
    }

    public void RefillFilm()
    {
        film = filmCap;
        BroadcastFilm();
    }

    /* -- Food -- */
    /// <summary>�°��Ƽ������������ѡ�</summary>
    public bool UseFood(FoodType type, int amount = 1)
    {
        int idx = (int)type;
        if (amount <= 0) { Debug.LogWarning($"UseFood �Ƿ�����: {amount}"); return false; }
        if (food[idx] < amount) return false;

        food[idx] -= amount;
        BroadcastFood(type);
        return true;
    }

    /// <summary>
    /// �ɽӿڱ�����Ĭ�ϰ������㵽 Fruit ���֤����ͨ����  
    /// �뾡���Ϊ <c>UseFood(FoodType,type)</c> �°档
    /// </summary>
    [Obsolete("����� UseFood(FoodType type, int amount = 1)")]
    public bool UseFood(int amount = 1) => UseFood(FoodType.Fruit, amount);

    public void AddFood(FoodType type, int amount)
    {
        if (amount <= 0) { Debug.LogWarning($"AddFood �Ƿ�����: {amount}"); return; }
        int idx = (int)type;
        food[idx] = Mathf.Clamp(food[idx] + amount, 0, foodCap);
        BroadcastFood(type);
    }

    /// <summary>һ���Բ�������ʳ�ﵽ���ޡ�</summary>
    public void RefillAllFood()
    {
        for (int i = 0; i < food.Length; i++)
            food[i] = foodCap;

        BroadcastFood(FoodType.Meat);
        BroadcastFood(FoodType.Leaves);
        BroadcastFood(FoodType.Fruit);
    }

    /* ===================================================================== */
    /*                               ��ʼ��                                 */
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

    /* ---------- �ڲ��㲥 ---------- */
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
