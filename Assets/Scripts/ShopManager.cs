using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private float shopIntervalMeters = 500f;
    [SerializeReference] private List<ShopItem> items = new List<ShopItem>
    {
        new MilkSaleShopItem(),
        new ExcessCoalSaleShopItem(),
        new CoalShopItem()
    };

    public static ShopManager Instance { get; private set; }

    private ShipController ship;
    private ResourceManager resources;
    private AltitudeMeter altitudeMeter;
    private GameObject shopOverlay;
    private TMP_Text coinText;
    private TMP_Text statusText;
    private readonly List<Button> purchaseButtons = new List<Button>();
    private float nextShopAltitude;
    private bool isOpen;

    public ShipController Ship => ship;
    public ResourceManager Resources => resources;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one ShopManager can be active in a scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ship = Object.FindAnyObjectByType<ShipController>();
        resources = ResourceManager.Instance;
        altitudeMeter = Object.FindAnyObjectByType<AltitudeMeter>();
        Canvas canvas = altitudeMeter != null ? altitudeMeter.GetComponentInParent<Canvas>() : null;

        if (ship == null || resources == null || altitudeMeter == null || canvas == null)
        {
            Debug.LogError("ShopManager requires a ShipController, ResourceManager, AltitudeMeter, and Canvas.", this);
            enabled = false;
            return;
        }

        if (shopIntervalMeters <= 0f)
        {
            Debug.LogError("Shop interval must be greater than zero meters.", this);
            enabled = false;
            return;
        }

        if (items == null)
            items = new List<ShopItem>();
        if (items.Count == 0)
        {
            items.Add(new MilkSaleShopItem());
            items.Add(new ExcessCoalSaleShopItem());
            items.Add(new CoalShopItem());
        }

        nextShopAltitude = ship.MaxAltitude + shopIntervalMeters;
        CreateShopUI(canvas.transform);
        UpdateShopDistance();
        OpenShop();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (!isOpen && ship.MaxAltitude >= nextShopAltitude)
        {
            nextShopAltitude += shopIntervalMeters;
            OpenShop();
        }

        UpdateShopDistance();
        if (isOpen)
            RefreshShopUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void CreateShopUI(Transform canvas)
    {
        shopOverlay = new GameObject("ShopOverlay", typeof(RectTransform), typeof(Image));
        shopOverlay.transform.SetParent(canvas, false);
        shopOverlay.transform.SetAsLastSibling();
        RectTransform overlayRect = shopOverlay.GetComponent<RectTransform>();
        Stretch(overlayRect);
        Image overlayImage = shopOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.72f);

        var panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(shopOverlay.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(640f, 260f + items.Count * 86f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

        CreateText("Airship Supply Shop", panel.transform, 42f, new Vector2(0f, 1f), new Vector2(0f, -26f),
            new Vector2(640f, 60f), TextAlignmentOptions.Center, Color.white);
        coinText = CreateText(string.Empty, panel.transform, 26f, new Vector2(1f, 1f), new Vector2(-24f, -28f),
            new Vector2(190f, 42f), TextAlignmentOptions.Right, new Color(1f, 0.82f, 0.28f));
        statusText = CreateText(string.Empty, panel.transform, 22f, new Vector2(0.5f, 0f), new Vector2(0f, 78f),
            new Vector2(560f, 38f), TextAlignmentOptions.Center, Color.white);

        for (int i = 0; i < items.Count; i++)
        {
            ShopItem item = items[i];
            if (item == null)
            {
                Debug.LogError($"Shop item at index {i} is missing.", this);
                continue;
            }

            float y = -105f - i * 86f;
            CreateText($"{item.DisplayName}  -  {item.Description}", panel.transform, 24f,
                new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(390f, 58f),
                TextAlignmentOptions.Left, Color.white);
            Button actionButton = CreateButton(item.ActionLabel, panel.transform,
                new Vector2(1f, 1f), new Vector2(-24f, y - 8f), new Vector2(180f, 54f));
            int itemIndex = i;
            actionButton.onClick.AddListener(() => ExecuteItem(itemIndex));
            purchaseButtons.Add(actionButton);
        }

        Button continueButton = CreateButton("Continue", panel.transform, new Vector2(0.5f, 0f),
            new Vector2(0f, 20f), new Vector2(220f, 54f));
        continueButton.onClick.AddListener(CloseShop);
        shopOverlay.SetActive(false);
    }

    private void ExecuteItem(int index)
    {
        if (index < 0 || index >= items.Count || items[index] == null)
            return;

        items[index].Execute(this, out string message);
        statusText.text = message;
        RefreshShopUI();
    }

    private void OpenShop()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        isOpen = true;
        Time.timeScale = 0f;
        shopOverlay.SetActive(true);
        shopOverlay.transform.SetAsLastSibling();
        statusText.text = "Welcome aboard. Spend your coins before you continue.";
        RefreshShopUI();
        UpdateShopDistance();
    }

    private void CloseShop()
    {
        isOpen = false;
        shopOverlay.SetActive(false);

        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
            Time.timeScale = 1f;

        UpdateShopDistance();
    }

    private void RefreshShopUI()
    {
        if (resources == null || coinText == null)
            return;

        coinText.text = $"Coins: {resources.Coins}";
        int buttonIndex = 0;
        foreach (ShopItem item in items)
        {
            if (item == null)
                continue;

            if (buttonIndex < purchaseButtons.Count)
            {
                Button button = purchaseButtons[buttonIndex];
                button.interactable = item.CanExecute(this);
                TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                    buttonText.text = item.ActionLabel;
            }
            buttonIndex++;
        }
    }

    private void UpdateShopDistance()
    {
        if (ship == null || altitudeMeter == null || shopIntervalMeters <= 0f)
            return;

        float distance = Mathf.Max(0f, nextShopAltitude - ship.MaxAltitude);
        float progress = 1f - distance / shopIntervalMeters;
        altitudeMeter.SetShopProgress(isOpen ? 1f : progress, distance, isOpen);
    }

    private static TMP_Text CreateText(string value, Transform parent, float fontSize, Vector2 anchor,
        Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment, Color color)
    {
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(anchor.x, anchor.y);
        rect.pivot = new Vector2(anchor.x, anchor.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string label, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.35f, 0.48f, 0.32f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        CreateText(label, buttonObject.transform, 22f, Vector2.zero, Vector2.zero, size,
            TextAlignmentOptions.Center, Color.white);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
