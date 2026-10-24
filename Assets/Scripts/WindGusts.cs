using UnityEngine;

// Random gusts that shove the ship sideways (and a little up or down), with wind streaks so players can see them coming.
public class WindGusts : MonoBehaviour
{
    [SerializeField] private ShipController ship;
    [SerializeField] private Vector2 intervalRange = new Vector2(4f, 10f);  // seconds between gusts
    [SerializeField] private Vector2 durationRange = new Vector2(1.5f, 3f); // seconds per gust
    [SerializeField] private Vector2 strengthRange = new Vector2(1f, 3.5f); // world units/s sideways at peak (steering max is 4)
    [SerializeField] private float maxVerticalPush = 3f;                    // m/s up or down at peak

    [Header("Streaks")]
    [SerializeField] private Sprite streakSprite;
    [SerializeField] private Transform streakParent; // OutsideZone
    [SerializeField] private int streakCount = 16;
    [SerializeField] private Color streakColor = new Color(1f, 1f, 1f, 0.7f);
    [SerializeField] private float streakSpeedMultiplier = 4f; // streaks move faster than the ship gets pushed
    [SerializeField] private Vector2 streakArea = new Vector2(9.5f, 5f); // half-extents around the outside view
    [SerializeField] private int streakSortingOrder = 8;

    // x = world units/s sideways, y = m/s up/down. Signed.
    public Vector2 CurrentPush { get; private set; }

    // Set by WeatherSystem: storms bring stronger, more frequent gusts.
    public float StrengthMultiplier { get; set; } = 1f;
    public float FrequencyMultiplier { get; set; } = 1f;

    private float nextGustTime;
    private float gustStart;
    private float gustDuration;
    private float gustStrength; // signed: negative blows left
    private float gustVertical;
    private bool gustActive;

    private SpriteRenderer[] streaks;

    private void Start()
    {
        nextGustTime = Time.time + Random.Range(intervalRange.x, intervalRange.y);
        CreateStreaks();
    }

    private void Update()
    {
        // No wind while docked.
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            UpdateStreaks(0f);
            return;
        }

        if (!gustActive && Time.time >= nextGustTime)
            StartGust();

        float envelope = 0f;
        if (gustActive)
        {
            float t = (Time.time - gustStart) / gustDuration;
            if (t >= 1f)
            {
                gustActive = false;
                nextGustTime = Time.time + Random.Range(intervalRange.x, intervalRange.y) / Mathf.Max(0.1f, FrequencyMultiplier);
            }
            else
            {
                envelope = Mathf.Sin(t * Mathf.PI); // ramp up, then die down
            }
        }

        CurrentPush = new Vector2(gustStrength, gustVertical) * envelope;
        ship.WindPush = CurrentPush;

        UpdateStreaks(envelope);
    }

    private void StartGust()
    {
        gustActive = true;
        gustStart = Time.time;
        gustDuration = Random.Range(durationRange.x, durationRange.y);
        gustStrength = Random.Range(strengthRange.x, strengthRange.y) * StrengthMultiplier * (Random.value < 0.5f ? -1f : 1f);
        gustVertical = Random.Range(-maxVerticalPush, maxVerticalPush);
    }

    private void CreateStreaks()
    {
        if (streakSprite == null || streakParent == null)
        {
            streaks = new SpriteRenderer[0];
            return;
        }

        streaks = new SpriteRenderer[streakCount];
        for (int i = 0; i < streakCount; i++)
        {
            var go = new GameObject("WindStreak" + i);
            go.transform.SetParent(streakParent, false);
            go.transform.localPosition = new Vector3(
                Random.Range(-streakArea.x, streakArea.x),
                Random.Range(-streakArea.y, streakArea.y),
                0f);
            go.transform.localScale = new Vector3(Random.Range(0.8f, 2.2f), 0.04f, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = streakSprite;
            renderer.sortingOrder = streakSortingOrder;
            renderer.color = new Color(streakColor.r, streakColor.g, streakColor.b, 0f);
            streaks[i] = renderer;
        }
    }

    private void UpdateStreaks(float envelope)
    {
        float speed = gustStrength * streakSpeedMultiplier;
        Color color = streakColor;
        color.a *= envelope;

        foreach (SpriteRenderer streak in streaks)
        {
            streak.color = color;
            if (envelope <= 0f)
                continue;

            Vector3 position = streak.transform.localPosition;
            position.x += speed * Time.deltaTime;
            position.y += gustVertical * ShipController.UnitsPerMeter * streakSpeedMultiplier * Time.deltaTime;

            // Wrap around the view so the streaks keep flowing.
            if (position.x > streakArea.x) position.x -= streakArea.x * 2f;
            if (position.x < -streakArea.x) position.x += streakArea.x * 2f;
            if (position.y > streakArea.y) position.y -= streakArea.y * 2f;
            if (position.y < -streakArea.y) position.y += streakArea.y * 2f;
            streak.transform.localPosition = position;
        }
    }
}
