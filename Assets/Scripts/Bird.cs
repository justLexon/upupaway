using UnityEngine;

// Flies back and forth across the whole screen while wandering up and down.
public class Bird : Obstacle
{
    [SerializeField] private float knockbackMeters = 15f;
    [SerializeField] private Vector2 speedRange = new Vector2(1.2f, 5.5f); // world units/s, picked per bird
    [SerializeField] private float xEdge = 8.5f;                       // turns around here (screen is ~±8.9 wide)
    [SerializeField] private float maxClimbSpeed = 8f;                 // m/s up or down while wandering
    [SerializeField] private float wanderFrequency = 0.5f;             // how often it changes vertical direction

    private float speed;
    private float direction;
    private float noiseSeed;

    private void Start()
    {
        speed = Random.Range(speedRange.x, speedRange.y);
        direction = Random.value < 0.5f ? -1f : 1f;
        noiseSeed = Random.value * 100f;
        Face();
    }

    protected override void Update()
    {
        // Smooth random climb/dive. Changing Altitude before base.Update keeps the on-screen position in sync.
        float wander = (Mathf.PerlinNoise(noiseSeed, Time.time * wanderFrequency) - 0.5f) * 2f;
        Altitude += wander * maxClimbSpeed * Time.deltaTime;

        base.Update();

        Vector3 position = transform.position;
        position.x += direction * speed * Time.deltaTime;
        transform.position = position;

        // Birds only fly by day: at night they stop turning around and leave the screen.
        bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
        if (night)
        {
            if (Mathf.Abs(position.x) > xEdge + 1.5f)
                Destroy(gameObject);
        }
        else if ((direction > 0f && position.x > xEdge) || (direction < 0f && position.x < -xEdge))
        {
            direction = -direction;
            Face();
        }
    }

    // The art faces right, so flip it to match the flight direction.
    private void Face()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    protected override void OnHitShip(ShipController ship)
    {
        ship.Knockback(knockbackMeters);
        Destroy(gameObject);
    }
}
