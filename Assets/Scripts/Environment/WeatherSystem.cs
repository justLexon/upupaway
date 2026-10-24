using System.Collections;
using UnityEngine;

// Night storms: each dusk there's a chance of heavy rain for the night. Rain slows the climb, gusts get stronger,
// and lightning strikes now and then, with a small chance of hitting the ship.
public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance { get; private set; }

    [SerializeField] private ShipController ship;
    [SerializeField] private WindGusts wind;

    [Header("Storm")]
    [SerializeField, Range(0f, 1f)] private float stormChance = 0.5f; // per night
    [SerializeField] private float stormSink = 3f;          // extra sink in m/s at full storm
    [SerializeField] private float stormWindStrength = 1.8f; // gust strength multiplier at full storm
    [SerializeField] private float stormWindFrequency = 2f;  // gusts come this many times as often
    [SerializeField] private float rampSpeed = 0.3f;         // storm intensity change per second

    [Header("Lightning")]
    [SerializeField] private Vector2 lightningInterval = new Vector2(4f, 9f); // seconds between strikes
    [SerializeField, Range(0f, 1f)] private float lightningHitChance = 0.2f;
    [SerializeField] private float lightningKnockback = 30f; // meters

    [Header("Visuals")]
    [SerializeField] private Sprite rainSprite;      // square
    [SerializeField] private Material unlitMaterial; // so rain and bolts stay bright at night
    [SerializeField] private Transform effectsParent; // OutsideZone
    [SerializeField] private int rainCount = 90;
    [SerializeField] private Vector2 rainArea = new Vector2(9.5f, 5.5f);
    [SerializeField] private float rainSpeed = 14f;
    [SerializeField] private Color rainColor = new Color(0.75f, 0.82f, 1f, 0.55f);
    [SerializeField] private int rainSortingOrder = 9;
    [SerializeField] private Color boltColor = new Color(1f, 1f, 0.85f);
    [SerializeField] private int boltSortingOrder = 12;

    // 0 = clear, 1 = full storm.
    public float StormIntensity { get; private set; }
    public bool IsStorming => StormIntensity > 0.5f;

    // Fired on every lightning strike; true if it hit the ship.
    public event System.Action<bool> OnLightning;

    private bool stormTonight;
    private float nextLightning;
    private SpriteRenderer[] rain;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnDusk += RollStorm;
        CreateRain();
        ScheduleLightning();
    }

    private void OnDestroy()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnDusk -= RollStorm;
    }

    private void RollStorm()
    {
        stormTonight = Random.value < stormChance;
    }

    private void Update()
    {
        float darkness = DayNightCycle.Instance != null ? DayNightCycle.Instance.Darkness : 0f;
        float target = stormTonight ? darkness : 0f; // storms build at dusk and clear at dawn
        StormIntensity = Mathf.MoveTowards(StormIntensity, target, rampSpeed * Time.deltaTime);

        ship.WeatherSink = StormIntensity * stormSink;
        if (wind != null)
        {
            wind.StrengthMultiplier = Mathf.Lerp(1f, stormWindStrength, StormIntensity);
            wind.FrequencyMultiplier = Mathf.Lerp(1f, stormWindFrequency, StormIntensity);
        }

        UpdateRain();

        bool canStrike = IsStorming && !GameManager.Instance.IsPaused && !GameManager.Instance.IsGameOver;
        if (!canStrike)
            ScheduleLightning(); // no instant strike the moment a storm starts
        else if (Time.time >= nextLightning)
        {
            Strike();
            ScheduleLightning();
        }
    }

    private void ScheduleLightning()
    {
        nextLightning = Time.time + Random.Range(lightningInterval.x, lightningInterval.y);
    }

    private void Strike()
    {
        bool hit = Random.value < lightningHitChance;
        Vector3 shipPosition = ship.transform.position;

        float x;
        Vector3 end;
        if (hit)
        {
            x = shipPosition.x;
            end = shipPosition + Vector3.up * 1.2f; // the balloon
        }
        else
        {
            // Miss: strike somewhere clearly away from the ship.
            do x = Random.Range(-8f, 8f); while (Mathf.Abs(x - shipPosition.x) < 2.5f);
            end = new Vector3(x, Random.Range(-4f, 0f), 0f);
        }

        var start = new Vector3(x + Random.Range(-1f, 1f), 5.5f, 0f);
        StartCoroutine(Bolt(start, end));

        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.Flash(1f);

        if (hit)
            ship.Knockback(lightningKnockback);
        OnLightning?.Invoke(hit);
    }

    // A zigzag of stretched squares (plus a wider glow) that fades out.
    private IEnumerator Bolt(Vector3 start, Vector3 end)
    {
        const int segments = 8;
        var points = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            points[i] = Vector3.Lerp(start, end, i / (float)segments);
            if (i > 0 && i < segments)
                points[i].x += Random.Range(-0.5f, 0.5f);
        }

        var root = new GameObject("LightningBolt").transform;
        root.SetParent(effectsParent, true);
        var pieces = new SpriteRenderer[segments * 2];
        for (int i = 0; i < segments; i++)
        {
            pieces[i * 2] = Segment(root, points[i], points[i + 1], 0.35f, new Color(boltColor.r, boltColor.g, boltColor.b, 0.35f), boltSortingOrder);
            pieces[i * 2 + 1] = Segment(root, points[i], points[i + 1], 0.1f, boltColor, boltSortingOrder + 1);
        }

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
        {
            foreach (SpriteRenderer piece in pieces)
            {
                Color color = piece.color;
                color.a = Mathf.Min(color.a, 1f - t);
                piece.color = color;
            }
            yield return null;
        }
        Destroy(root.gameObject);
    }

    private SpriteRenderer Segment(Transform parent, Vector3 from, Vector3 to, float width, Color color, int order)
    {
        var go = new GameObject("Segment");
        go.transform.SetParent(parent, true);
        go.transform.position = (from + to) / 2f;
        Vector3 delta = to - from;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
        go.transform.localScale = new Vector3(width, delta.magnitude + width * 0.5f, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = rainSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (unlitMaterial != null)
            renderer.sharedMaterial = unlitMaterial;
        return renderer;
    }

    private void CreateRain()
    {
        if (rainSprite == null || effectsParent == null)
        {
            rain = new SpriteRenderer[0];
            return;
        }

        rain = new SpriteRenderer[rainCount];
        for (int i = 0; i < rainCount; i++)
        {
            var go = new GameObject("RainDrop" + i);
            go.transform.SetParent(effectsParent, false);
            go.transform.localPosition = new Vector3(Random.Range(-rainArea.x, rainArea.x), Random.Range(-rainArea.y, rainArea.y), 0f);
            go.transform.localScale = new Vector3(0.03f, Random.Range(0.3f, 0.55f), 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = rainSprite;
            renderer.sortingOrder = rainSortingOrder;
            renderer.color = new Color(rainColor.r, rainColor.g, rainColor.b, 0f);
            if (unlitMaterial != null)
                renderer.sharedMaterial = unlitMaterial;
            rain[i] = renderer;
        }
    }

    private void UpdateRain()
    {
        Color color = rainColor;
        color.a *= StormIntensity;

        // Rain slants with the wind.
        float windX = wind != null ? wind.CurrentPush.x : 0f;
        var velocity = new Vector3(windX * 2f, -rainSpeed, 0f);
        Quaternion slant = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + 90f);

        foreach (SpriteRenderer drop in rain)
        {
            drop.color = color;
            if (StormIntensity <= 0f)
                continue;

            Transform t = drop.transform;
            Vector3 position = t.localPosition + velocity * Time.deltaTime;
            if (position.y < -rainArea.y) position.y += rainArea.y * 2f;
            if (position.x > rainArea.x) position.x -= rainArea.x * 2f;
            if (position.x < -rainArea.x) position.x += rainArea.x * 2f;
            t.localPosition = position;
            t.localRotation = slant;
        }
    }
}
