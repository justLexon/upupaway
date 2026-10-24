using System.Collections;
using UnityEngine;

// Hits the ship and sticks underneath, adding weight. Attached jugs ARE your milk:
// click one off to drop it (lighter, but it's gone), or carry it to a pitstop and sell it.
public class MilkJug : Obstacle, IClickable
{
    [SerializeField] private float weightPerJug = 1.5f; // extra sink in m/s while attached
    [SerializeField] private int tapsToRemove = 1;
    [SerializeField] private float fallSpeed = 8f;      // world units/s after being knocked off
    [SerializeField] private float fallSpin = 360f;     // degrees/s while falling
    [SerializeField] private float sellFlyTime = 0.5f;  // seconds to fly over to the pitstop when sold
    [SerializeField] private GameObject balloon;        // pops when the jug sticks to the ship

    private bool attached;
    private int taps;
    private ShipController owner;
    private Coroutine wobbleRoutine;

    public bool IsAttached => attached;

    // Jugs can still be dropped while docked.
    public bool WorksWhilePaused => true;

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
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.Add(ResourceType.Milk, 1);
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

    // Dropped jugs are lost: one less milk to sell.
    private void KnockOff()
    {
        Detach();
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.TrySpend(ResourceType.Milk, 1);
        StartCoroutine(Fall());
    }

    // Called by the ship when milk is sold at a pitstop (the coins were already paid).
    public void Sell(Vector3 target)
    {
        if (!attached)
            return;
        Detach();
        StartCoroutine(FlyTo(target));
    }

    // Detach before touching ResourceManager so the ship's jug count already matches.
    private void Detach()
    {
        attached = false;
        owner.RemoveWeight(weightPerJug);

        // Unparenting frees the slot immediately.
        transform.SetParent(owner.transform.parent, true);
        GetComponent<Collider2D>().enabled = false;
    }

    // Unscaled so it still plays while docked (paused).
    private IEnumerator Fall()
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
        {
            transform.position += Vector3.down * fallSpeed * Time.unscaledDeltaTime;
            transform.Rotate(0f, 0f, fallSpin * Time.unscaledDeltaTime);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator FlyTo(Vector3 target)
    {
        Vector3 start = transform.position;
        Vector3 startScale = transform.localScale;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / sellFlyTime)
        {
            Vector3 position = Vector3.Lerp(start, target, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 1.2f;
            transform.position = position;
            transform.localScale = startScale * (1f - 0.6f * t);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator Wobble()
    {
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 60f) * 15f);
            yield return null;
        }
        transform.localRotation = Quaternion.identity;
        wobbleRoutine = null;
    }
}
