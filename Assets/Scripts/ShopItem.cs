using System;
using UnityEngine;

[Serializable]
public abstract class ShopItem
{
    [SerializeField] private string displayName;
    [SerializeField] private string description;

    protected ShopItem()
    {
    }

    protected ShopItem(string displayName, string description)
    {
        this.displayName = displayName;
        this.description = description;
    }

    public string DisplayName => displayName;
    public string Description => description;
    public abstract string ActionLabel { get; }
    public abstract bool CanExecute(ShopManager shop);
    public abstract bool Execute(ShopManager shop, out string message);
}
