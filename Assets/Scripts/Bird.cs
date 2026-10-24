using UnityEngine;

public class Bird : Obstacle
{
    [SerializeField] private float knockbackMeters = 15f;
    [SerializeField] private float wobbleAmplitude = 0.5f; // world units side to side
    [SerializeField] private float wobbleFrequency = 1.5f; // wobbles per second

    private float baseX;
    private float phase;

    private void Start()
    {
        baseX = transform.position.x;
        phase = Random.value * Mathf.PI * 2f;
    }

    protected override void Update()
    {
        base.Update();

        Vector3 position = transform.position;
        position.x = baseX + Mathf.Sin(Time.time * wobbleFrequency * Mathf.PI * 2f + phase) * wobbleAmplitude;
        transform.position = position;
    }

    protected override void OnHitShip(ShipController ship)
    {
        ship.Knockback(knockbackMeters);
        Destroy(gameObject);
    }
}
