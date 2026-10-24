using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Left-side bar. Bottom = the cloud sea, marker height = how far above the clouds the ship is.
public class AltitudeMeter : MonoBehaviour
{
    [SerializeField] private ShipController ship;
    [SerializeField] private CloudSea cloudSea;
    [SerializeField] private RectTransform fill;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform marker;
    [SerializeField] private TMP_Text altitudeText;
    [SerializeField] private Color safeColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color warnColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color dangerColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private float dangerGap = 20f; // meters; marker pulses below this

    private RectTransform shopMarker;
    private RectTransform shopDistanceLabel;
    private TMP_Text shopDistanceText;

    private void Awake()
    {
        CreateShopIndicator();
    }

    private void Start()
    {
        if (ShopManager.Instance != null)
            return;

        GameObject managers = GameObject.Find("Managers");
        if (managers == null)
        {
            Debug.LogError("AltitudeMeter requires a Managers object to create the shop.", this);
            return;
        }

        managers.AddComponent<ShopManager>();
    }

    private void Update()
    {
        float gap = ship.Altitude - cloudSea.Altitude;
        float t = Mathf.Clamp01(gap / cloudSea.MaxGap);

        fill.anchorMax = new Vector2(1f, t);
        marker.anchorMin = marker.anchorMax = new Vector2(0.5f, t);

        fillImage.color = t < 0.5f
            ? Color.Lerp(dangerColor, warnColor, t * 2f)
            : Color.Lerp(warnColor, safeColor, (t - 0.5f) * 2f);

        float pulse = gap < dangerGap ? 1f + 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f)) : 1f;
        marker.localScale = new Vector3(pulse, pulse, 1f);

        altitudeText.text = $"{ship.Altitude:N0} m";
    }

    public void SetShopProgress(float progress, float distanceMeters, bool shopOpen)
    {
        if (shopMarker == null || shopDistanceText == null)
            return;

        float clampedProgress = Mathf.Clamp01(progress);
        shopMarker.anchorMin = shopMarker.anchorMax = new Vector2(1f, clampedProgress);
        shopMarker.anchoredPosition = new Vector2(8f, 0f);
        shopDistanceLabel.anchorMin = shopDistanceLabel.anchorMax = new Vector2(1f, clampedProgress);
        shopDistanceLabel.anchoredPosition = new Vector2(24f, 0f);
        shopDistanceText.text = shopOpen ? "SHOP OPEN" : $"SHOP {distanceMeters:N0} m";
    }

    private void CreateShopIndicator()
    {
        var markerObject = new GameObject("ShopProgressMarker", typeof(RectTransform), typeof(Image));
        markerObject.transform.SetParent(transform, false);
        shopMarker = markerObject.GetComponent<RectTransform>();
        shopMarker.anchorMin = shopMarker.anchorMax = new Vector2(1f, 0f);
        shopMarker.pivot = new Vector2(0.5f, 0.5f);
        shopMarker.sizeDelta = new Vector2(22f, 22f);
        shopMarker.anchoredPosition = new Vector2(8f, 0f);
        markerObject.GetComponent<Image>().color = new Color(1f, 0.75f, 0.2f);
        markerObject.GetComponent<Image>().raycastTarget = false;

        var markerTextObject = new GameObject("ShopMarkerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        markerTextObject.transform.SetParent(markerObject.transform, false);
        TextMeshProUGUI iconText = markerTextObject.GetComponent<TextMeshProUGUI>();
        iconText.rectTransform.anchorMin = Vector2.zero;
        iconText.rectTransform.anchorMax = Vector2.one;
        iconText.rectTransform.offsetMin = Vector2.zero;
        iconText.rectTransform.offsetMax = Vector2.zero;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.fontSize = 14f;
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = Color.black;
        iconText.text = "S";
        iconText.raycastTarget = false;

        var distanceObject = new GameObject("ShopDistance", typeof(RectTransform), typeof(TextMeshProUGUI));
        distanceObject.transform.SetParent(transform, false);
        shopDistanceLabel = distanceObject.GetComponent<RectTransform>();
        shopDistanceLabel.anchorMin = shopDistanceLabel.anchorMax = new Vector2(1f, 0f);
        shopDistanceLabel.pivot = new Vector2(0f, 0.5f);
        shopDistanceLabel.sizeDelta = new Vector2(150f, 32f);
        shopDistanceLabel.anchoredPosition = new Vector2(24f, 0f);
        shopDistanceText = distanceObject.GetComponent<TextMeshProUGUI>();
        shopDistanceText.alignment = TextAlignmentOptions.Left;
        shopDistanceText.fontSize = 18f;
        shopDistanceText.fontStyle = FontStyles.Bold;
        shopDistanceText.color = new Color(1f, 0.8f, 0.3f);
        shopDistanceText.raycastTarget = false;

        shopDistanceText.text = "SHOP";
    }
}
