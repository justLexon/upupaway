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

    public float Altitude { get; private set; }
    public float MaxAltitude { get; private set; }
    public float VerticalSpeed { get; private set; }

    // Extra sink in m/s from milk jugs.
    public float Weight { get; private set; }

    // Set by WindGusts. x = world units/s sideways, y = m/s up/down.
    public Vector2 WindPush { get; set; }

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

    private void Update()
    {
        float targetSpeed = furnace.Heat01 * maxLiftSpeed - sinkSpeed - Weight;
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
