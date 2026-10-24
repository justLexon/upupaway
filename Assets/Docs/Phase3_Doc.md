# Phase 3: Steering Wheel, World Scrolling, First Obstacle

**Goal:** Steer the ship left/right from the cabin by holding the wheel and moving the mouse, make the world scroll past as you climb, and add one obstacle to dodge.

**Builds on:** Phase 2 (cabin, camera switching, PiP, `IClickable` / `ClickInput`).

**Not in this phase:** milk jugs, resource balloons, the altitude meter UI. The spawner and obstacle framework built here is what Phase 4 plugs into.

**Done when:**
- [ ] Holding the wheel and moving the mouse left/right turns the wheel and moves the ship
- [ ] Letting go of the wheel slowly re-centers it; the ship can't leave the screen
- [ ] Obstacles spawn above as you climb, scroll down past the ship, and despawn off screen
- [ ] Hitting a **bird** (first obstacle) knocks the ship down and gives clear feedback
- [ ] You can dodge birds using only the cabin view + PiP

---

## Design

### Steering wheel

- `SteeringWheel` implements `IClickable`.
- **While held:** `angle -= mouseDeltaX * sensitivity`. Clamp it to ±`maxAngle` (90°). Rotate the wheel sprite on Z.
  - Mouse right = wheel turns clockwise = negative Z = **ship goes right**.
  - Use `Mouse.current.delta.ReadValue().x`.
- **When released:** the angle drifts back toward 0 at `returnSpeed` (°/s). This is forgiving, but you can't just let go and fly straight forever.
- It outputs `SteerInput = -angle / maxAngle` (−1 left … +1 right).
- **Gotcha:** while holding the wheel the mouse can leave the wheel sprite. That's fine, because `ClickInput` only calls `OnRelease` on mouse up, wherever the mouse is.
- Optional: lock or confine the cursor while held (`Cursor.lockState = CursorLockMode.Confined`) so it can't leave the window.

### Ship horizontal movement

Add this to `ShipController`:
- `[SerializeField] SteeringWheel wheel`, `maxSideSpeed` (units/s, about 4), `xLimit` (about 7).
- `x += wheel.SteerInput * maxSideSpeed * dt`, then clamp to ±`xLimit`.
- Optional: tilt the ship sprite a few degrees in the steer direction, which is great feedback in the PiP.
- The camera **doesn't** follow X. The outside view is a fixed window and the ship slides across it.

### World scrolling: `AltitudeObject`

Same trick as the cloud sea. Anything in the sky has its own altitude in meters, and its screen Y is placed relative to the ship:

```csharp
// AltitudeObject.cs: base for anything that lives in the sky
public float Altitude;                  // meters
[SerializeField] float driftSpeed = 0f; // its own m/s (balloons rise, birds can sink)

void Update()
{
    Altitude += driftSpeed * Time.deltaTime;
    float gap = Altitude - ship.Altitude;
    transform.position = new Vector3(transform.position.x, ship.transform.position.y + gap * unitsPerMeter, 0f);
    if (gap < -despawnBelow || gap > despawnAbove) Destroy(gameObject);
}
```

- The spawner passes in `ship` and `unitsPerMeter`. Keep **one** shared `unitsPerMeter` (put it on `ShipController` as a public constant/field and have `CloudSea` use it too).
- **Optional sky decor:** spawn small, collider-less cloud puffs as `AltitudeObject`s so climbing *feels* like movement, even with no obstacles on screen. This is cheap and helps a lot.

### Spawner

`ObstacleSpawner` spawns by **distance climbed**, not time, so hovering doesn't flood the screen:
- It tracks `nextSpawnAltitude`. When `ship.MaxAltitude + spawnAhead >= nextSpawnAltitude`, it spawns at that altitude and adds `Random.Range(minSpacing, maxSpacing)`.
- `spawnAhead ≈ 60 m`, which is just above the top of the screen (the ship is at y=1, the screen top at y=5, which is 40 m away).
- Random X within ±`xLimit`.
- It holds a **weighted list** of prefabs: `[{prefab, weight}]`. Phase 3 has only the bird; Phase 4 adds the milk jug and balloon.
- Optional: scale the spacing down as altitude rises (difficulty ramp).

### Obstacles

Base class:

```csharp
public abstract class Obstacle : AltitudeObject
{
    protected bool used;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        var ship = other.GetComponentInParent<ShipController>();
        if (ship == null) return;
        used = true;
        OnHitShip(ship);
    }

    protected abstract void OnHitShip(ShipController ship);
}
```

**Physics setup:**
- Ship: `BoxCollider2D` + `Rigidbody2D` (**Dynamic**, Gravity Scale 0, Constraints **Freeze All**). In 2D, Kinematic bodies don't get trigger contacts with other Kinematic or static colliders by default, so the ship uses a frozen Dynamic body and is still moved by transform.
- Obstacles: `Collider2D` with **Is Trigger** on, plus a **Kinematic** `Rigidbody2D` (since they move every frame).

**Bird (first obstacle):**
- On hit: `ship.Knockback(knockbackMeters)` (about 15 m), plus a short burst of extra speed loss, then destroy (or tumble away).
- Add `public void Knockback(float meters)` to `ShipController`: `Altitude -= meters; VerticalSpeed = Mathf.Min(VerticalSpeed, -5f);`
- Optional: a `driftSpeed` of -1 so birds swoop down a bit, plus a slight horizontal sine wobble.
- Feedback: flash the ship red and do a small camera shake. A shake in the PiP reads well too.

---

## Steps

### 1. Scene

```
CabinZone
├── CabinBackground
├── Furnace
└── SteeringWheel     (circle/square sprite 2.5 × 2.5, CircleCollider2D, + SteeringWheel)
OutsideZone
├── Ship              (+ BoxCollider2D, Rigidbody2D Dynamic / no gravity / Freeze All)
└── CloudSea
Managers              (+ ObstacleSpawner)
```

**Prefabs** (`Assets/Prefabs/`):
- `Bird`: small dark square/triangle sprite, `CircleCollider2D` (trigger), `Bird` script.
- `SkyPuff` (optional): faint white sprite, `AltitudeObject` script, no collider.

Place the wheel in the cabin so the furnace and wheel are both reachable. The "run between stations" tension comes from switching views, not from distance inside the cabin.

### 2. Scripts

| Script | New/Changed | Responsibility |
|---|---|---|
| `SteeringWheel.cs` | New | Hold + mouse delta → angle → `SteerInput` |
| `AltitudeObject.cs` | New | Altitude-relative positioning, drift, despawn |
| `Obstacle.cs` | New | Abstract base: trigger detection → `OnHitShip` |
| `Bird.cs` | New | Knockback on hit |
| `ObstacleSpawner.cs` | New | Distance-based weighted spawning |
| `ShipController.cs` | Changed | Horizontal steering, `Knockback()`, shared `unitsPerMeter` |
| `CloudSea.cs` | Changed | Use the shared `unitsPerMeter` |

### 3. Scene builder

Extend the builder to add the wheel, ship physics, spawner, and generate the `Bird` prefab.

---

## Test

1. In the cabin, hold the wheel and drag right: the wheel turns, and in the PiP the ship slides right. Let go: the wheel eases back to center.
2. The ship stops at the screen edges.
3. Climb: birds appear at the top and scroll down. Hover or sink: they stop or drift back up (correct, since they're fixed in the sky).
4. Hit a bird: the ship drops, flashes, and the bird is consumed (it only triggers once).
5. Dodge birds only from the cabin using the PiP. If this feels impossible, the PiP is too small or the steering too slow (see Tuning).
6. Watch the Hierarchy for a while: obstacles get destroyed and don't pile up.

### Gotchas
- **Triggers never fire:** the ship needs a `Rigidbody2D` (Dynamic, gravity 0, Freeze All) and the obstacle collider needs Is Trigger.
- **The wheel spins backwards:** flip the sign in `angle -= delta`.
- **Steering is wildly different between machines:** mouse delta is per frame in pixels, so **don't** multiply it by `deltaTime`. Just tune `sensitivity`.

---

## Tuning Knobs

| Feel | Tweak |
|---|---|
| Steering is too twitchy | ↓ `sensitivity`, ↓ `maxSideSpeed` |
| Can't dodge in time | ↑ `maxSideSpeed`, ↑ `spawnAhead`, ↑ PiP size |
| Too many/few obstacles | `minSpacing` / `maxSpacing` |
| Wheel lets go too fast/slow | `returnSpeed` |
| Birds not punishing enough | ↑ `knockbackMeters` |

---

## Commit

`Phase 3: steering wheel, world scrolling, birds`

**Next up: Phase 4** adds milk jugs (weight), resource balloons (coal), and the real HUD, which completes the base game.
