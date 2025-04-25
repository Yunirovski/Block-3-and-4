using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    #region Properties
    public int QuotaStar => quotaStar;
    public int TotalStar => totalStar;
    #endregion

    // ★ 当前可花费
    [SerializeField] int quotaStar = 0;
    // ★ 总收藏（只增不减）
    [SerializeField] int totalStar = 0;

    public event Action OnCurrencyChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    public void AddStars(int amount)
    {
        quotaStar += amount;
        totalStar += amount;
        OnCurrencyChanged?.Invoke();
    }

    public bool TrySpendStars(int amount)
    {
        if (quotaStar < amount) return false;
        quotaStar -= amount;
        OnCurrencyChanged?.Invoke();
        return true;
    }
}
