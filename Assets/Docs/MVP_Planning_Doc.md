# Up Up Away — MVP Planning Doc

**Jam theme:** Ascension (just go up)
**Time budget:** ~40 hours
**Engine:** Unity 6 (6000.6.7f1), 2D URP, new Input System

---

## Concept

A point-and-click airship management game. There is no player character; the player interacts by clicking with the mouse. The goal is to climb as high as possible by managing resources, mainly coal, while dodging obstacles and staying above a rising sea of clouds.

---

## Core Loop

1. Shovel coal into the furnace to climb.
   - More coal = faster climb, but coal runs out faster.
   - Not enough coal = the ship sinks.
2. Steer left/right to dodge obstacles and grab resource balloons.
3. Switch between the outside view and the cabin view to handle problems.
4. Don't fall into the rising cloud sea, or the game is over.

---

## Screens

### Top: Outside View
- Shows the airship exterior, obstacles, and resource balloons.
- **Altitude meter** on the left side of the screen.
- **PiP rectangle at the bottom of the screen** shows a live camera view of the cabin.
- Click milk jugs stuck to the ship to knock them off.

### Bottom: Cabin View
- Interior with interactive stations.
- **PiP rectangle at the top of the screen** shows a live camera view of the outside, so the player can see obstacles while steering.

### Switching
- Scroll (wheel / keys) moves between views.
- Movement is **smooth but fast** (e.g. `Vector3.SmoothDamp`, ~0.15s).

---

## Cabin Stations

| Station | Interaction | Effect |
|---|---|---|
| **Furnace** | Click to add coal | Burns coal over time, which produces lift |
| **Steering Wheel** | Click and hold, move mouse left/right | Moves the ship horizontally |
| **Harpoon** *(post-MVP)* | Aim and fire | Grab resource balloons as they pass |

---

## Obstacles & Pickups

- **Milk Gallons:** stick to the outside base of the ship on hit and **add weight**. More weight means more coal is needed to climb. The player clicks them (outside view) to remove them.
- **Resource Balloons:** carry a crate/barrel/present with coal. Collect them by flying into them (or with the harpoon, post-MVP).

---

## Lose Condition

- A **rising sea of clouds** climbs from below, possibly speeding up over time.
- If the ship's altitude drops to or below the cloud sea, the game is over.

---

## Technical Approach

- **One scene.** The outside zone and the cabin zone are stacked far apart in world space.
- **Main Camera** slides between the two zones.
- **PiP Camera** renders the other zone to a **Render Texture**, shown on a UI **RawImage**. The rect flips between the bottom and top of the screen depending on the current view.
- **Fake vertical movement:** the ship stays fixed and the world/obstacles scroll down. Altitude is just a number.
- **Clicking world objects:** use the new Input System + `Physics2D.OverlapPoint` or `IPointerClickHandler` with a `Physics2DRaycaster`. Avoid `OnMouseDown`.

### Scripts (MVP)

| Script | Responsibility |
|---|---|
| `GameManager` | Start, game over, restart |
| `ShipController` | Altitude, vertical speed (lift − weight), horizontal position |
| `ResourceManager` | Coal count (later: harpoon ammo, etc.) |
| `Furnace` | Click to add coal; burn rate → lift |
| `SteeringWheel` | Hold + mouse delta X → rotation → horizontal movement |
| `ObstacleSpawner` | Spawns milk jugs and resource balloons that scroll down |
| `MilkJug` | Attaches on hit, adds weight, click to remove |
| `ResourceBalloon` | Gives coal on collect |
| `CloudSea` | Rises over time; triggers game over |
| `CameraSwitcher` | Main cam movement + PiP camera/rect flipping |
| `AltitudeMeter` | Left-side UI bar |

---

## Build Order (stop wherever time runs out)

1. **Ship altitude + coal + cloud sea + lose condition**
2. **Two-view camera switch + PiP + furnace clicking**
3. **Steering wheel + one obstacle type**
4. **Milk jugs (weight) + resource balloons**
5. **Harpoon** (third station)
6. **Stops/docks every X distance** to gain materials
7. **Polish:** art, sound, juice, menus

If 1–3 work, we have a game. Everything after that is a bonus.

---

## Post-MVP Ideas

- Harpoon station
- Docks/stops every X distance (collect materials, maybe upgrades)
- Pressure valve / boiler gauge (overfeeding coal causes danger)
- Ballast lever (drop sandbags for an emergency boost)
- Balloon leaks to patch
- Cloud sea speeds up over time / difficulty scaling
