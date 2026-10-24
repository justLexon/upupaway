using UnityEngine;

public abstract class Obstacle : AltitudeObject
{
    protected bool used;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used)
            return;

        var ship = other.GetComponentInParent<ShipController>();
        if (ship == null)
            return;

        used = true;
        OnHitShip(ship);
    }

    protected abstract void OnHitShip(ShipController ship);
}
