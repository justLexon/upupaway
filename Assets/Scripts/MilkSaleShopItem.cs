using System;
using UnityEngine;

[Serializable]
public sealed class MilkSaleShopItem : ShopItem
{
    [SerializeField] private int coinsPerJug = 3;

    public MilkSaleShopItem() : base("Sell milk", "Sell all attached milk jugs to remove their weight.")
    {
    }

    public override string ActionLabel => $"Sell all ({coinsPerJug} coins/jug)";

    public override bool CanExecute(ShopManager shop)
    {
        return coinsPerJug > 0 && shop.Ship.AttachedMilkCount > 0;
    }

    public override bool Execute(ShopManager shop, out string message)
    {
        if (coinsPerJug <= 0)
        {
            message = "The milk sale price is not configured correctly.";
            return false;
        }

        int sold = shop.Ship.SellAttachedMilk();
        if (sold == 0)
        {
            message = "There is no attached milk to sell.";
            return false;
        }

        int coins = sold * coinsPerJug;
        shop.Resources.AddCoins(coins);
        message = $"Sold {sold} milk jug{(sold == 1 ? string.Empty : "s")} for {coins} coins.";
        return true;
    }
}
