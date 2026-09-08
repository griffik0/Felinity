using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Static CatCoin wallet with a live UI counter.
/// </summary>
public static class CatCoins
{
    public static event Action<int> OnBalanceChanged;

    static int _balance;

    public static int CatCoinBalance
    {
        get => _balance;
        set
        {
            if (_balance == value)
                return;

            _balance = value;
            OnBalanceChanged?.Invoke(_balance);
        }
    }

    public static void Add(int amount)
    {
        CatCoinBalance += amount;
    }
}

/// <summary>
/// Top-left HUD text that mirrors <see cref="CatCoins.CatCoinBalance"/>.
/// </summary>
public class CatCoinsUI : MonoBehaviour
{
    public TMP_Text balanceText;

    void Awake()
    {
        if (balanceText == null)
            balanceText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        CatCoins.OnBalanceChanged += Refresh;
        Refresh(CatCoins.CatCoinBalance);
    }

    void OnDisable()
    {
        CatCoins.OnBalanceChanged -= Refresh;
    }

    void Refresh(int balance)
    {
        if (balanceText != null)
            balanceText.text = $"CatCoins: {balance}";
    }
}
