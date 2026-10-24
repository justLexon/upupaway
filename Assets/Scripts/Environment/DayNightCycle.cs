using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Day -> dusk -> night -> dawn, on a timer (pauses with the game). Drives sky color, the global 2D light, stars and moon.
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [SerializeField] private float dayDuration = 60f;       // seconds, including dusk
    [SerializeField] private float nightDuration = 45f;     // seconds, including dawn
    [SerializeField] private float transitionDuration = 6f; // dusk/dawn length

    [Header("Sky")]
    [SerializeField] private Camera[] skyCameras; // main + PiP
    [SerializeField] private Color daySky = new Color(0.53f, 0.81f, 0.98f);
    [SerializeField] private Color duskSky = new Color(0.95f, 0.55f, 0.4f);
    [SerializeField] private Color nightSky = new Color(0.07f, 0.09f, 0.2f);
    [SerializeField] private Color stormSky = new Color(0.18f, 0.2f, 0.25f);

    [Header("Light")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private float dayIntensity = 1f;
    [SerializeField] private float nightIntensity = 0.45f;
    [SerializeField] private float stormIntensityMultiplier = 0.75f;
    [SerializeField] private Color nightLightColor = new Color(0.65f, 0.72f, 1f);
    [SerializeField] private float flashIntensity = 1.6f; // extra light at the peak of a lightning flash

    [Header("Stars & moon")]
    [SerializeField] private Sprite starSprite;     // circle
    [SerializeField] private Material unlitMaterial; // so they glow instead of being dimmed by the night light
    [SerializeField] private Transform starParent;  // OutsideZone
    [SerializeField] private int starCount = 45;
    [SerializeField] private Vector2 starArea = new Vector2(9f, 5f);
    [SerializeField] private Vector2 moonPosition = new Vector2(-5.5f, 3.4f);

    // 0 = full day, 1 = full night.
    public float Darkness { get; private set; }
    public bool IsNight => Darkness > 0.5f;

    // Fired when dusk begins. WeatherSystem rolls for a storm here.
    public event Action OnDusk;

    private float cycleTime;
    private bool wasDarkening;
    private float flash;
    private SpriteRenderer[] stars;
    private float[] starPhases;
    private SpriteRenderer moon;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CreateStars();
        Apply(0f);
    }

    private void Update()
    {
        cycleTime += Time.deltaTime;
        float length = dayDuration + nightDuration;
        float t = cycleTime % length;

        if (t < dayDuration)
            Darkness = Mathf.InverseLerp(dayDuration - transitionDuration, dayDuration, t);
        else
            Darkness = 1f - Mathf.InverseLerp(nightDuration - transitionDuration, nightDuration, t - dayDuration);

        bool darkening = t >= dayDuration - transitionDuration && t < dayDuration;
        if (darkening && !wasDarkening)
            OnDusk?.Invoke();
        wasDarkening = darkening;

        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 4f);

        float storm = WeatherSystem.Instance != null ? WeatherSystem.Instance.StormIntensity : 0f;
        Apply(storm);
    }

    // Lightning: briefly lights up everything.
    public void Flash(float amount)
    {
        flash = Mathf.Max(flash, amount);
    }

    private void Apply(float storm)
    {
        Color sky = Darkness < 0.5f
            ? Color.Lerp(daySky, duskSky, Darkness * 2f)
            : Color.Lerp(duskSky, nightSky, (Darkness - 0.5f) * 2f);
        sky = Color.Lerp(sky, Color.Lerp(stormSky, nightSky, Darkness * 0.5f), storm * 0.8f);
        sky = Color.Lerp(sky, Color.white, flash * 0.6f);

        foreach (Camera cam in skyCameras)
        {
            if (cam != null)
                cam.backgroundColor = sky;
        }

        if (globalLight != null)
        {
            float intensity = Mathf.Lerp(dayIntensity, nightIntensity, Darkness) * Mathf.Lerp(1f, stormIntensityMultiplier, storm);
            globalLight.intensity = intensity + flash * flashIntensity;
            globalLight.color = Color.Lerp(Color.white, nightLightColor, Darkness);
        }

        // Stars and moon come out at night, hidden by storm clouds.
        float visible = Mathf.Clamp01((Darkness - 0.5f) * 2f) * (1f - storm);
        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                float twinkle = 0.65f + 0.35f * Mathf.Sin(Time.time * 2f + starPhases[i]);
                stars[i].color = new Color(1f, 1f, 1f, visible * twinkle);
            }
        }
        if (moon != null)
            moon.color = new Color(0.98f, 0.95f, 0.82f, visible);
    }

    private void CreateStars()
    {
        if (starSprite == null || starParent == null)
            return;

        stars = new SpriteRenderer[starCount];
        starPhases = new float[starCount];
        for (int i = 0; i < starCount; i++)
        {
            var position = new Vector2(UnityEngine.Random.Range(-starArea.x, starArea.x), UnityEngine.Random.Range(-starArea.y * 0.3f, starArea.y));
            float size = UnityEngine.Random.Range(0.04f, 0.1f);
            stars[i] = CreateSkyDot("Star" + i, position, size, -20);
            starPhases[i] = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        moon = CreateSkyDot("Moon", moonPosition, 0.9f, -19);
    }

    private SpriteRenderer CreateSkyDot(string name, Vector2 position, float size, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(starParent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(size, size, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = starSprite;
        renderer.sortingOrder = sortingOrder;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        if (unlitMaterial != null)
            renderer.sharedMaterial = unlitMaterial;
        return renderer;
    }
}
