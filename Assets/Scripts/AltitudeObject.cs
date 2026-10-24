using UnityEngine;

// Anything that lives in the sky. It has its own altitude and is placed on screen relative to the ship.
public class AltitudeObject : MonoBehaviour
{
    [SerializeField] protected float driftSpeed = 0f;    // its own m/s (balloons rise, birds can sink)
    [SerializeField] private float despawnBelow = 80f;   // meters below the ship
    [SerializeField] private float despawnAbove = 150f;  // meters above the ship

    public float Altitude { get; set; }
    protected ShipController Ship { get; private set; }

    // When true, this object stops following the sky (e.g. a milk jug stuck to the ship).
    protected bool Frozen { get; set; }

    public void Init(ShipController ship, float altitude)
    {
        Ship = ship;
        Altitude = altitude;
        UpdatePosition();
    }

    protected virtual void Update()
    {
        if (Ship == null || Frozen)
            return;

        Altitude += driftSpeed * Time.deltaTime;
        UpdatePosition();

        float gap = Altitude - Ship.Altitude;
        if (gap < -despawnBelow || gap > despawnAbove)
            Destroy(gameObject);
    }

    private void UpdatePosition()
    {
        float gap = Altitude - Ship.Altitude;
        Vector3 position = transform.position;
        position.y = Ship.transform.position.y + gap * ShipController.UnitsPerMeter;
        transform.position = position;
    }
}
