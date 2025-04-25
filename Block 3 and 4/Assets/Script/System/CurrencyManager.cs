using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    #region Properties
    public int QuotaStar => quotaStar;
    public int TotalStar => totalStar;
    #endregion

    // �� ��ǰ�ɻ���
    [SerializeField] int quotaStar = 0;
    // �� ���ղأ�ֻ��������
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
