using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Left-side meter. The track runs from the cloud sea (bottom) to the next pitstop (top),
// with the ship marker in between, so one glance shows both danger and progress.
public class AltitudeMeter : MonoBehaviour
{
    [SerializeField] private ShipController ship;
    [SerializeField] private CloudSea cloudSea;
    [SerializeField] private PitstopManager pitstops;

    [Header("UI")]
    [SerializeField] private TMP_Text altitudeText; // optional
    [SerializeField] private RectTransform fill;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform shipMarker;
    [SerializeField] private TMP_Text gapText;
    [SerializeField] private TMP_Text pitstopText;

    [Header("Danger")]
    [SerializeField] private float safeGap = 100f;  // meters above clouds that count as fully safe
    [SerializeField] private float dangerGap = 25f; // meters; the marker pulses below this
    [SerializeField] private Color safeColor = new Color(0.35f, 0.85f, 0.45f);
    [SerializeField] private Color warnColor = new Color(1f, 0.82f, 0.25f);
    [SerializeField] private Color dangerColor = new Color(0.95f, 0.3f, 0.25f);

    private float displayT;

    private void Update()
    {
        float gap = ship.Altitude - cloudSea.Altitude;

        float bottom = cloudSea.Altitude;
        float top = pitstops != null ? pitstops.NextPitstopAltitude : cloudSea.Altitude + cloudSea.MaxGap;
        top = Mathf.Max(top, ship.Altitude + 1f);
        float t = Mathf.InverseLerp(bottom, top, ship.Altitude);

        // Ease the marker so it doesn't jitter.
        displayT = Mathf.Lerp(displayT, t, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        fill.anchorMax = new Vector2(1f, displayT);
        shipMarker.anchorMin = shipMarker.anchorMax = new Vector2(0.5f, displayT);

        float safety = Mathf.Clamp01(gap / safeGap);
        Color color = safety < 0.5f
            ? Color.Lerp(dangerColor, warnColor, safety * 2f)
            : Color.Lerp(warnColor, safeColor, (safety - 0.5f) * 2f);
        fillImage.color = color;
        gapText.color = color;
        gapText.text = $"+{Mathf.Max(0f, gap):0} m";

        float pulse = gap < dangerGap ? 1f + 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f)) : 1f;
        shipMarker.localScale = new Vector3(pulse, pulse, 1f);

        if (altitudeText != null)
            altitudeText.text = $"{ship.Altitude:N0} m";

        if (pitstopText != null && pitstops != null)
            pitstopText.text = pitstops.IsDocked ? "PITSTOP\nDocked" : $"PITSTOP\n{pitstops.DistanceToNext:N0} m";
    }
}
