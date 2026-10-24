using System;
using UnityEngine;

[Serializable]
public sealed class ExcessCoalSaleShopItem : ShopItem
{
    [SerializeField] private int keepCoal = 10;
    [SerializeField] private int coalPerCoin = 5;

    public ExcessCoalSaleShopItem() : base("Sell excess coal", "Keep 10 coal in reserve; sell extra coal in bundles of 5.")
    {
    }

    public override string ActionLabel => $"Sell excess ({coalPerCoin} coal/coin)";

    public override bool CanExecute(ShopManager shop)
    {
        return keepCoal >= 0 && coalPerCoin > 0 && shop.Resources.Coal >= keepCoal + coalPerCoin;
    }

    public override bool Execute(ShopManager shop, out string message)
    {
        if (keepCoal < 0 || coalPerCoin <= 0)
        {
            message = "The excess coal offer is not configured correctly.";
            return false;
        }

        int excessCoal = shop.Resources.Coal - keepCoal;
        int coalToSell = coalPerCoin > 0 ? excessCoal / coalPerCoin * coalPerCoin : 0;
        if (coalToSell <= 0 || !shop.Resources.TrySpendCoal(coalToSell))
        {
            message = $"Keep at least {keepCoal} coal and have {coalPerCoin} excess to sell.";
            return false;
        }

        int coins = coalToSell / coalPerCoin;
        shop.Resources.AddCoins(coins);
        message = $"Sold {coalToSell} excess coal for {coins} coins.";
        return true;
    }
}
