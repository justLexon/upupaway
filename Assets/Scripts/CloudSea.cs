using UnityEngine;

public class CloudSea : MonoBehaviour
{
    [SerializeField] private ShipController ship;
    [SerializeField] private float startingAltitude = 0f;
    [SerializeField] private float riseSpeed = 2f;           // m/s at start
    [SerializeField] private float riseAcceleration = 0.02f; // rise speed gained per second
    [SerializeField] private float maxGap = 150f;            // clouds never fall further behind than this

    public float Altitude { get; private set; }
    public float MaxGap => maxGap;

    private float currentRiseSpeed;

    private void Awake()
    {
        Altitude = startingAltitude;
        currentRiseSpeed = riseSpeed;
    }

    private void Update()
    {
        currentRiseSpeed += riseAcceleration * Time.deltaTime;
        Altitude += currentRiseSpeed * Time.deltaTime;

        // Keep the pressure on: if the player races ahead, the clouds catch up to maxGap.
        Altitude = Mathf.Max(Altitude, ship.Altitude - maxGap);

        // The ship stays fixed on screen, so place the clouds relative to it.
        float gap = Altitude - ship.Altitude;
        Vector3 position = transform.position;
        position.y = ship.transform.position.y + gap * ShipController.UnitsPerMeter;
        transform.position = position;

        if (Altitude >= ship.Altitude)
            GameManager.Instance.GameOver();
    }
}
