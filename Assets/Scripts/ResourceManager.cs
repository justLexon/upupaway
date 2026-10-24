using System;
using System.Collections.Generic;
using UnityEngine;

// Add new resources here; shops, HUDs and pickups all work off this.
public enum ResourceType
{
    Coal,
    Milk,
    Coins
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private int startingCoal = 20;
    [SerializeField] private int startingCoins = 5;

    public event Action<ResourceType, int> OnChanged;

    public int Coal => Get(ResourceType.Coal);

    private readonly Dictionary<ResourceType, int> amounts = new Dictionary<ResourceType, int>();

    private void Awake()
    {
        Instance = this;
        Set(ResourceType.Coal, startingCoal);
        Set(ResourceType.Coins, startingCoins);
    }

    public int Get(ResourceType type)
    {
        return amounts.TryGetValue(type, out int amount) ? amount : 0;
    }

    public void Add(ResourceType type, int amount)
    {
        Set(type, Get(type) + amount);
    }

    public bool TrySpend(ResourceType type, int amount)
    {
        if (Get(type) < amount) return false;
        Set(type, Get(type) - amount);
        return true;
    }

    public bool TrySpendCoal(int amount = 1) => TrySpend(ResourceType.Coal, amount);

    public void AddCoal(int amount) => Add(ResourceType.Coal, amount);

    private void Set(ResourceType type, int amount)
    {
        amounts[type] = amount;
        OnChanged?.Invoke(type, amount);
    }
}
