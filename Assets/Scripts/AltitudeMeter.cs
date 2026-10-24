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
}
