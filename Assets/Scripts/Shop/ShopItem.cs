using UnityEngine;

// One thing a pitstop trades. To add a new item: Create > Up Up Away > Shop Item, fill it in,
// and add it to the PitstopStation's Items list. No code needed.
[CreateAssetMenu(menuName = "Up Up Away/Shop Item", fileName = "NewShopItem")]
public class ShopItem : ScriptableObject
{
    public string displayName = "Item";
    public ResourceType resource;
    [Min(1)] public int amountPerTrade = 1; // how many you get/give per click

    [Header("Buying")]
    public bool canBuy = true;
    [Min(0)] public int buyPrice = 5;

    [Header("Selling")]
    public bool canSell = true;
    [Min(0)] public int sellPrice = 3;

    [Tooltip("Art shown on the stall (world space, origin at the deck surface).")]
    public GameObject displayPrefab;

    public bool CanBuy(ResourceManager resources)
    {
        return canBuy && resources.Get(ResourceType.Coins) >= buyPrice;
    }

    public bool CanSell(ResourceManager resources)
    {
        return canSell && resources.Get(resource) >= amountPerTrade;
    }

    public bool TryBuy(ResourceManager resources)
    {
        if (!CanBuy(resources)) return false;
        resources.TrySpend(ResourceType.Coins, buyPrice);
        resources.Add(resource, amountPerTrade);
        return true;
    }

    public bool TrySell(ResourceManager resources)
    {
        if (!CanSell(resources)) return false;
        resources.TrySpend(resource, amountPerTrade);
        resources.Add(ResourceType.Coins, sellPrice);
        return true;
    }
}
