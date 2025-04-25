using UnityEngine;
using System;

public class ConsumableManager : MonoBehaviour
{
    public static ConsumableManager Instance { get; private set; }

    [Header("Limits")]
    public int filmCap = 20;
    public int foodCap = 20;

    [Header("Initial Counts")]
    [SerializeField] int film = 5;
    [SerializeField] int food = 3;

    public int Film => film;
    public int Food => food;

    public event Action OnConsumableChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    #region Public API
    public bool UseFilm(int amount = 1)
    {
        if (film < amount) return false;
        film -= amount;
        OnConsumableChanged?.Invoke();
        return true;
    }

    public bool UseFood(int amount = 1)
    {
        if (food < amount) return false;
        food -= amount;
        OnConsumableChanged?.Invoke();
        return true;
    }

    public void AddFilm(int amount)
    {
        film = Mathf.Clamp(film + amount, 0, filmCap);
        OnConsumableChanged?.Invoke();
    }

    public void AddFood(int amount)
    {
        food = Mathf.Clamp(food + amount, 0, foodCap);
        OnConsumableChanged?.Invoke();
    }
    #endregion 

}
