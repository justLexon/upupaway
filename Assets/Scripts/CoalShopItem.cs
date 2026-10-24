using System;
using UnityEngine;

[Serializable]
public sealed class CoalShopItem : ShopItem
{
    [SerializeField] private int coinCost = 3;
    [SerializeField] private int coalAmount = 5;

    public CoalShopItem() : base("Coal", "Buy a bundle of 5 coal.")
    {
    }

    public override string ActionLabel => $"Buy ({coinCost} coins)";

    public override bool CanExecute(ShopManager shop)
    {
        return coinCost > 0 && coalAmount > 0 && shop.Resources.Coins >= coinCost;
    }

    public override bool Execute(ShopManager shop, out string message)
    {
        if (coinCost <= 0 || coalAmount <= 0)
        {
            message = "The coal offer is not configured correctly.";
            return false;
        }

        if (!shop.Resources.TrySpendCoins(coinCost))
        {
            message = "Not enough coins.";
            return false;
        }

        shop.Resources.AddCoal(coalAmount);
        message = $"Bought {coalAmount} coal.";
        return true;
    }
}
