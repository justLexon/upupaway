using System.Collections;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    public const float UnitsPerMeter = 0.1f; // 1 world unit = 10 m, shared by everything in the sky

    [SerializeField] private Furnace furnace;
    [SerializeField] private SteeringWheel wheel;
    [SerializeField] private float startingAltitude = 100f;
    [SerializeField] private float maxLiftSpeed = 15f; // m/s up at full heat
    [SerializeField] private float sinkSpeed = 5f;     // m/s down, always applied
    [SerializeField] private float acceleration = 4f;  // how quickly speed approaches its target
    [SerializeField] private float maxSideSpeed = 4f;  // world units/s at full steer
    [SerializeField] private float xLimit = 7f;
    [SerializeField] private float maxTilt = 8f;       // degrees
    [SerializeField] private float knockbackSpeed = 5f; // m/s down right after a hit
    [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Transform[] jugSlots;      // where milk jugs stick under the ship
    [SerializeField] private Vector3 soldJugOffset = new Vector3(8.6f, -2.3f, 0f); // sold jugs fly here (the pitstop deck)

    public float Altitude { get; private set; }
    public float MaxAltitude { get; private set; }
    public float VerticalSpeed { get; private set; }

    // Extra sink in m/s from milk jugs.
    public float Weight { get; private set; }

    // Set by WindGusts. x = world units/s sideways, y = m/s up/down.
    public Vector2 WindPush { get; set; }

    // Extra sink in m/s from heavy rain, set by WeatherSystem.
    public float WeatherSink { get; set; }

    // Grabbed in Awake, so milk jugs that attach later don't flash.
    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Coroutine flashRoutine;

    private void Awake()
    {
        Altitude = startingAltitude;
        MaxAltitude = startingAltitude;

        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i].color;
    }

    private void Start()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnChanged += HandleResourceChanged;
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnChanged -= HandleResourceChanged;
    }

    // Milk is the jugs on board. If the milk count drops below that (sold at a pitstop), unload the extras.
    private void HandleResourceChanged(ResourceType type, int amount)
    {
        if (type != ResourceType.Milk || jugSlots == null)
            return;

        int attached = 0;
        foreach (Transform slot in jugSlots)
        {
            MilkJug jug = slot != null ? slot.GetComponentInChildren<MilkJug>() : null;
            if (jug != null && jug.IsAttached)
                attached++;
        }

        for (int i = jugSlots.Length - 1; i >= 0 && attached > amount; i--)
        {
            MilkJug jug = jugSlots[i] != null ? jugSlots[i].GetComponentInChildren<MilkJug>() : null;
            if (jug == null || !jug.IsAttached)
                continue;
            jug.Sell(transform.position + soldJugOffset);
            attached--;
        }
    }

    private void Update()
    {
        // Docked at a pitstop: PitstopManager moves the ship.
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;

        float targetSpeed = furnace.Heat01 * maxLiftSpeed - sinkSpeed - Weight - WeatherSink;
        VerticalSpeed = Mathf.MoveTowards(VerticalSpeed, targetSpeed, acceleration * Time.deltaTime);

        Altitude += (VerticalSpeed + WindPush.y) * Time.deltaTime;
        MaxAltitude = Mathf.Max(MaxAltitude, Altitude);

        float steer = wheel != null ? wheel.SteerInput : 0f;
        float sideSpeed = steer * maxSideSpeed + WindPush.x;
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x + sideSpeed * Time.deltaTime, -xLimit, xLimit);
        transform.position = position;

        // Lean into the movement, wind included.
        float lean = Mathf.Clamp(sideSpeed / maxSideSpeed, -1.5f, 1.5f);
        transform.rotation = Quaternion.Euler(0f, 0f, -lean * maxTilt);
    }

    public void Knockback(float meters)
    {
        Altitude -= meters;
        VerticalSpeed = Mathf.Min(VerticalSpeed, -knockbackSpeed);

        if (renderers.Length == 0)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());
    }

    public void AddWeight(float amount)
    {
        Weight += amount;
    }

    public void RemoveWeight(float amount)
    {
        Weight = Mathf.Max(0f, Weight - amount);
    }

    public bool TryGetFreeSlot(out Transform slot)
    {
        if (jugSlots != null)
        {
            foreach (Transform candidate in jugSlots)
            {
                if (candidate != null && candidate.childCount == 0)
                {
                    slot = candidate;
                    return true;
                }
            }
        }
        slot = null;
        return false;
    }

    private IEnumerator Flash()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = baseColors[i] * hitColor;
        yield return new WaitForSeconds(0.15f);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = baseColors[i];
        flashRoutine = null;
    }
}
