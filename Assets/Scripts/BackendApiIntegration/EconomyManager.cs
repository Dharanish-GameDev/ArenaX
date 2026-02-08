using UnityEngine;
using System;
using Arena.API.Models;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    private int coins = 100;
    private int silver = 10;
    private int diamonds = 5;

    private event Action<int> coinsChanged;
    private event Action<int> silverChanged;
    private event Action<int> diamondsChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region GETTERS

    public int GetCoins() => coins;
    public int GetSilver() => silver;
    public int GetDiamonds() => diamonds;

    #endregion

    #region REGISTER / UNREGISTER

    public void RegisterCoins(Action<int> callback)
    {
        coinsChanged += callback;
        callback?.Invoke(coins);
    }

    public void UnregisterCoins(Action<int> callback)
    {
        coinsChanged -= callback;
    }

    public void RegisterSilver(Action<int> callback)
    {
        silverChanged += callback;
        callback?.Invoke(silver);
    }

    public void UnregisterSilver(Action<int> callback)
    {
        silverChanged -= callback;
    }

    public void RegisterDiamonds(Action<int> callback)
    {
        diamondsChanged += callback;
        callback?.Invoke(diamonds);
    }

    public void UnregisterDiamonds(Action<int> callback)
    {
        diamondsChanged -= callback;
    }

    #endregion

    #region SET / ADD / SPEND

    public void AddEconomy(string type, int value)
    {
        if (TryParseReward(type, out RewardType rewardType))
        {
            switch (rewardType)
            {
                case RewardType.coins:
                    AddCoins(value); 
                    break;
                case RewardType.silver:
                    AddSilver(value);
                    break;
                case RewardType.diamond:
                    AddDiamonds(value);
                    break;
            }
        }
    }

    public void SetCoins(int value)
    {
        value = Mathf.Max(0, value);
        if (coins == value) return;

        coins = value;
        coinsChanged?.Invoke(coins);
    }

    public void SetSilver(int value)
    {
        value = Mathf.Max(0, value);
        if (silver == value) return;

        silver = value;
        silverChanged?.Invoke(silver);
    }

    public void SetDiamonds(int value)
    {
        value = Mathf.Max(0, value);
        if (diamonds == value) return;

        diamonds = value;
        diamondsChanged?.Invoke(diamonds);
    }

    public void AddCoins(int amount) => SetCoins(coins + amount);
    public void AddSilver(int amount) => SetSilver(silver + amount);
    public void AddDiamonds(int amount) => SetDiamonds(diamonds + amount);

    public bool SpendCoins(int amount)
    {
        if (coins < amount) return false;
        SetCoins(coins - amount);
        return true;
    }

    public bool SpendSilver(int amount)
    {
        if (silver < amount) return false;
        SetSilver(silver - amount);
        return true;
    }

    public bool SpendDiamonds(int amount)
    {
        if (diamonds < amount) return false;
        SetDiamonds(diamonds - amount);
        return true;
    }

    #endregion
    
    public static bool TryParseReward(string value, out RewardType reward)
    {
        return Enum.TryParse(value, true, out reward);
    }

}
