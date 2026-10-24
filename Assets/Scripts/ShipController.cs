using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private Furnace furnace;
    [SerializeField] private float startingAltitude = 100f;
    [SerializeField] private float maxLiftSpeed = 15f; // m/s up at full heat
    [SerializeField] private float sinkSpeed = 5f;     // m/s down, always applied
    [SerializeField] private float acceleration = 4f;  // how quickly speed approaches its target

    public float Altitude { get; private set; }
    public float MaxAltitude { get; private set; }
    public float VerticalSpeed { get; private set; }

    // Extra sink in m/s. Phase 4: milk jugs add to this.
    public float Weight { get; set; }

    private void Awake()
    {
        Altitude = startingAltitude;
        MaxAltitude = startingAltitude;
    }

    private void Update()
    {
        float targetSpeed = furnace.Heat01 * maxLiftSpeed - sinkSpeed - Weight;
        VerticalSpeed = Mathf.MoveTowards(VerticalSpeed, targetSpeed, acceleration * Time.deltaTime);

        Altitude += VerticalSpeed * Time.deltaTime;
        MaxAltitude = Mathf.Max(MaxAltitude, Altitude);
    }
}
