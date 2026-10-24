using System.Collections;
using UnityEngine;

public class MilkJug : Obstacle, IClickable
{
    [SerializeField] private float weightPerJug = 1.5f; // extra sink in m/s while attached
    [SerializeField] private int tapsToRemove = 1;
    [SerializeField] private float fallSpeed = 8f;      // world units/s after being knocked off
    [SerializeField] private float fallSpin = 360f;     // degrees/s while falling
    [SerializeField] private GameObject balloon;        // pops when the jug sticks to the ship

    private bool attached;
    private int taps;
    private ShipController owner;
    private Coroutine wobbleRoutine;

    protected override void OnHitShip(ShipController ship)
    {
        if (!ship.TryGetFreeSlot(out Transform slot))
        {
            Destroy(gameObject); // ship is full, this one just bounces off
            return;
        }

        attached = true;
        Frozen = true;
        owner = ship;

        if (balloon != null)
            Destroy(balloon);

        // Keep the jug's on-screen size even though the ship is scaled non-uniformly.
        Vector3 worldScale = transform.lossyScale;
        transform.SetParent(slot, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        Vector3 parentScale = slot.lossyScale;
        transform.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, 1f);

        ship.AddWeight(weightPerJug);
    }

    public void OnPress()
    {
        if (!attached)
            return; // floating jugs are dodged, not clicked

        taps++;
        if (taps >= tapsToRemove)
        {
            KnockOff();
            return;
        }

        if (wobbleRoutine != null)
            StopCoroutine(wobbleRoutine);
        wobbleRoutine = StartCoroutine(Wobble());
    }

    public void OnRelease()
    {
    }

    private void KnockOff()
    {
        attached = false;
        owner.RemoveWeight(weightPerJug);

        // Unparenting frees the slot immediately.
        transform.SetParent(owner.transform.parent, true);
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(Fall());
    }

    private IEnumerator Fall()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            transform.Rotate(0f, 0f, fallSpin * Time.deltaTime);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator Wobble()
    {
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 60f) * 15f);
            yield return null;
        }
        transform.localRotation = Quaternion.identity;
        wobbleRoutine = null;
    }
}
