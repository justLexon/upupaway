# Phase 4: Milk Jugs, Resource Balloons, HUD (Base Game Complete)

**Goal:** Add the two signature mechanics, **milk jugs** that weigh you down until you click them off and **resource balloons** that refill coal, plus the real altitude meter and HUD. After this phase the base game is complete and submittable.

**Builds on:** Phase 3 (`AltitudeObject`, `Obstacle`, weighted `ObstacleSpawner`, ship collider, `ShipController.Weight`) and Phase 2 (`IClickable`).

**Not in this phase:** harpoon, docks, art/sound polish. See Post-MVP in the planning doc.

**Done when:**
- [ ] Hitting a milk jug sticks it to the bottom of the ship and makes the ship heavier
- [ ] Clicking a stuck jug (outside view) knocks it off and removes the weight
- [ ] Hitting a resource balloon pops it and gives coal
- [ ] The spawner mixes birds, jugs, and balloons, and coal runs short but rarely runs out unfairly
- [ ] The left-side altitude meter shows altitude and how close the clouds are
- [ ] The HUD shows coal and heat in both views; the debug text is hidden by default
- [ ] Game over shows the final altitude and the best altitude ever

---

## Design

### The core tradeoff, now complete

```
         ┌────────── Cabin ──────────┐        ┌──────── Outside ────────┐
         │ Furnace: feed coal        │        │ Click jugs off the ship │
         │ Wheel: dodge / catch      │ ◄────► │ Watch altitude meter    │
         └───────────────────────────┘        └─────────────────────────┘
Coal ── comes from balloons (steer INTO them)
Weight ── comes from jugs (steer AWAY, or go outside and click them off)
```

The player is constantly choosing between staying in the cabin (fuel, steering) and going outside (removing weight). That's the game.

### Milk jugs

- `MilkJug : Obstacle, IClickable`
- **On hit:**
  - Ask the ship for a free **attach slot** (an empty child Transform under the ship's base; about 6 slots spread along the bottom).
  - If there's no free slot, the jug just bounces off (or destroy it, so you never get more than the max).
  - Otherwise parent the jug to the slot, snap it to `localPosition = 0`, and **disable `AltitudeObject` movement** (set a `attached` flag so `Update` skips positioning and despawning).
  - `ship.AddWeight(weightPerJug)`. Store weight as a count × amount so it's easy to undo.
- **Click (`OnPress`) while attached:**
  - Count taps; at `tapsToRemove` (start with 1, try 2–3 if it's too easy), detach.
  - `ship.RemoveWeight(weightPerJug)`, free the slot, then "fall off" (unparent, give a quick downward tween/rotation, destroy after 1s).
  - Ignore clicks while it's still floating in the sky (`attached == false`). You dodge those, you don't click them.
- **Feel:** wobble the jug slightly each tap. Each attached jug should be clearly visible in the PiP from the cabin so the player knows to go outside.

Changes to `ShipController`:
- `Transform[] jugSlots` and `TryGetFreeSlot(out Transform slot)`.
- Replace the raw `Weight` setter with `AddWeight(float)` / `RemoveWeight(float)` (clamped ≥ 0).
- `weightPerJug ≈ 1.5 m/s` of extra sink. With 3 jugs, the player needs noticeably more heat to hold altitude.

### Resource balloons

- `ResourceBalloon : Obstacle`
- `driftSpeed ≈ +1.5 m/s` (rises gently, so it's harder to catch when you're sinking).
- **On hit:** `ResourceManager.Instance.AddCoal(coalAmount)` (about 5), pop effect, destroy.
- Visual: a colored circle (balloon) with a small square (crate) hanging below. Use one collider covering both.
- Later, the harpoon will also call the same `Collect()` method, so put the reward logic there.

### Spawner mix

Starting weights (tune by playing):

| Prefab | Weight |
|---|---|
| Bird | 40 |
| MilkJug | 35 |
| ResourceBalloon | 25 |

**Pity rule:** if coal ≤ 3 and no balloon has spawned in the last N spawns, force a balloon. This prevents unwinnable runs from bad luck. You can also lower the spacing between spawns as altitude climbs.

### Altitude meter (left side)

A vertical bar on the left edge of the screen (outside view; optionally in both):
- **Bottom of the bar = the cloud sea. Ship marker height = `gap / maxGap`** (the gap between the ship and the clouds). This makes the danger readable at a glance: the marker sinking toward the bottom means trouble.
- Color the bar fill green → yellow → red as the gap shrinks.
- A big **altitude number** above the bar: `1,240 m`.
- Optional: the bar shakes or pulses red when the gap is under 20 m.
- Reads from `ship.Altitude`, `cloudSea.Altitude`, and `cloudSea.MaxGap` (expose a getter).

### HUD

- **Coal counter** (icon + number) and a **heat bar**, visible in both views. Anchor them at a corner the PiP doesn't cover (top-right is safe in both views).
- **DebugHUD:** hidden by default, toggled with **F1**.
- **Game over panel:** `GAME OVER`, `Altitude: X m`, `Best: Y m`, `Press R to restart`.
  - Best altitude is saved with `PlayerPrefs.SetFloat("BestAltitude", ...)`.

---

## Steps

### 1. Scene

```
OutsideZone
└── Ship
    └── JugSlots        (empty)
        ├── Slot0 … Slot5   (empties along the ship's bottom edge)
Canvas
├── AltitudeMeter       (Image bar background + fill Image + ship marker Image + TMP altitude text)
├── ResourceHUD         (TMP coal text, heat bar Image)
├── PipFrame / PipView
├── DebugText           (hidden by default)
└── GameOverPanel       (+ final/best altitude TMP texts)
```

**Prefabs:**
- `MilkJug`: white rectangle with a blue "cap", `BoxCollider2D` (trigger), `MilkJug` script.
- `ResourceBalloon`: colored circle + brown crate child, `CircleCollider2D` (trigger), `ResourceBalloon` script.
- Add both to the `ObstacleSpawner` weighted list.

**Click note:** the jug's collider stays enabled after attaching so `ClickInput` can still find it. Because `Obstacle.used` is set on the first hit, it won't re-trigger on the ship.

### 2. Scripts

| Script | New/Changed | Responsibility |
|---|---|---|
| `MilkJug.cs` | New | Attach on hit, add weight, tap to remove |
| `ResourceBalloon.cs` | New | Give coal on hit (`Collect()`) |
| `AltitudeMeter.cs` | New | Left bar: cloud-gap marker, color, altitude text |
| `ResourceHUD.cs` | New | Coal count + heat bar |
| `AltitudeObject.cs` | Changed | Respect an `attached`/`frozen` flag (skip positioning and despawn) |
| `ShipController.cs` | Changed | Jug slots, `AddWeight`/`RemoveWeight` |
| `ObstacleSpawner.cs` | Changed | Pity rule for balloons |
| `CloudSea.cs` | Changed | Expose `MaxGap` |
| `GameManager.cs` | Changed | Final/best altitude on the game over panel |
| `DebugHUD.cs` | Changed | F1 toggle, hidden by default |

### 3. Scene builder

Extend the builder to generate the jug slots, the `MilkJug` and `ResourceBalloon` prefabs, the meter, and the HUD.

---

## Test

1. Hit a jug: it sticks under the ship, the climb slows, and the debug speed drops by about `weightPerJug`.
2. Go outside and click the jug: it falls off and the speed recovers.
3. Hit 6+ jugs: the extras don't attach (no overlap or stacking bugs).
4. Hit a balloon: coal goes up by 5 and the balloon pops.
5. Let coal get low: a balloon shows up soon (pity rule).
6. The meter marker falls as the clouds approach, the color shifts to red, and game over triggers when the marker hits the bottom.
7. Game over shows the correct final altitude; the best altitude persists after restarting the editor.
8. **Play 5 full runs** without the debug HUD. Are runs 1–3 minutes long? Does switching views feel necessary? Tune until both are yes.

### Gotchas
- **Clicking an attached jug does nothing:** check that the outside camera is the one `ClickInput` uses (the main camera, in the outside view), and that the jug's collider is still enabled.
- **An attached jug drifts away or despawns:** `AltitudeObject.Update` isn't checking the `attached` flag.
- **Jugs keep hitting the ship after attaching:** the `used` flag isn't set, or a second collider is on the prefab.

---

## Tuning Knobs

| Feel | Tweak |
|---|---|
| Jugs are trivial | ↑ `weightPerJug`, ↑ `tapsToRemove`, ↑ jug spawn weight |
| Jugs feel unfair | ↓ `weightPerJug`, fewer slots |
| Always out of coal | ↑ `coalAmount`, ↑ balloon weight, ↓ balloon `driftSpeed` |
| Never worried about coal | ↓ `coalAmount`, ↓ `startingCoal` |
| Runs are too long | ↑ cloud `riseAcceleration` |

---

## Commit

`Phase 4: milk jugs, resource balloons, HUD - base game complete`

**After this:** the base game is done. Next priorities (from the planning doc): **harpoon**, **docks every X meters**, then polish (art from your teammate, sound, screen shake, title screen).
