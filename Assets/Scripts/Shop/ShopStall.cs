using TMPro;
using UnityEngine;

// One stall on the pitstop deck: shows a ShopItem's art, name, how many you have, and Buy/Sell signs.
public class ShopStall : MonoBehaviour
{
    [SerializeField] private Transform displayAnchor;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private ShopButton buyButton;
    [SerializeField] private ShopButton sellButton;

    private ShopItem item;

    public void Setup(ShopItem shopItem)
    {
        item = shopItem;
        titleText.text = item.displayName.ToUpperInvariant();

        if (item.displayPrefab != null)
            Instantiate(item.displayPrefab, displayAnchor, false);

        buyButton.gameObject.SetActive(item.canBuy);
        sellButton.gameObject.SetActive(item.canSell);

        // A lone button sits in the middle.
        if (item.canBuy != item.canSell)
        {
            ShopButton only = item.canBuy ? buyButton : sellButton;
            Vector3 position = only.transform.localPosition;
            position.x = 0f;
            only.transform.localPosition = position;
        }

        string amount = item.amountPerTrade > 1 ? $" x{item.amountPerTrade}" : "";
        buyButton.SetLabel($"BUY{amount}\n${item.buyPrice}");
        sellButton.SetLabel($"SELL{amount}\n${item.sellPrice}");

        buyButton.Clicked += () => item.TryBuy(ResourceManager.Instance);
        sellButton.Clicked += () => item.TrySell(ResourceManager.Instance);

        ResourceManager.Instance.OnChanged += HandleResourcesChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnChanged -= HandleResourcesChanged;
    }

    private void HandleResourcesChanged(ResourceType type, int amount)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (item == null)
            return;

        ResourceManager resources = ResourceManager.Instance;
        infoText.text = $"You have {resources.Get(item.resource)}";
        buyButton.SetInteractable(item.CanBuy(resources));
        sellButton.SetInteractable(item.CanSell(resources));
    }
}
