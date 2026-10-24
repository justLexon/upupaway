# Phase 1: Altitude, Coal, Cloud Sea, Lose Condition

**Goal:** Starting from an empty project, get a ship that climbs when you burn coal, sinks when you don't, and loses when the rising cloud sea catches it.

**Not in this phase:** cabin view, PiP camera, clicking, steering, obstacles. Coal is added with **Space** as a temporary stand-in for clicking the furnace (Phase 2 replaces it).

**Done when:**
- [ ] Pressing Space spends coal and heats the furnace
- [ ] Heat makes the ship climb; no heat makes it sink
- [ ] The cloud sea rises, visibly approaches the ship, and ends the game on contact
- [ ] Game over screen shows; **R** restarts
- [ ] A debug readout shows altitude, speed, coal, heat, and cloud distance

---

## How It Works (quick recap)

- The **ship never moves vertically on screen.** Altitude is just a number in `ShipController`.
- The **cloud sea** has its own altitude. Its sprite is positioned *relative to the ship* based on the altitude gap, so it looks like it's rising toward you.
- Scale: **1 world unit = 10 meters** (`unitsPerMeter = 0.1`).

```
Furnace heat (0–100) ──► ShipController target speed = heat% × maxLift − sink − weight
                                   │
                                   ▼
                              Altitude (m)
                                   │
CloudSea altitude (rising) ────────┴──► if cloud ≥ ship → GameManager.GameOver()
```

---

## Step 1: Project Cleanup & Folders

1. Delete `Assets/Welcome` (template leftover).
2. Rename `Assets/Scenes/SampleScene` to **`Main`**.
3. Create these folders under `Assets`:
   ```
   Assets/
   ├── Docs/
   ├── Scenes/
   ├── Scripts/
   ├── Sprites/
   └── Prefabs/
   ```
4. Move `testSprite.png` into `Sprites/`.
5. **File > Build Profiles > Scene List:** add `Main` (needed for restart to work).
6. **Check input:** *Edit > Project Settings > Player > Active Input Handling* should be **Input System Package (New)** or **Both**. The 2D template defaults to New.
7. **TextMeshPro:** the first time you create a TMP text, Unity asks to import **TMP Essentials**. Click Import.

---

## Step 2: Scene Setup

Open `Main` and build this hierarchy:

```
Main Camera
Ship                (Sprite: square)
CloudSea            (empty, has CloudSea script)
└── CloudVisual     (Sprite: square)
Furnace             (empty, has Furnace script)
Managers            (empty, has GameManager + ResourceManager)
Canvas
├── DebugText       (TextMeshPro - Text)
└── GameOverPanel   (Panel)
    └── GameOverText (TextMeshPro - Text)
EventSystem         (auto-created with Canvas)
```

### Main Camera
- Position `(0, 0, -10)`, Projection **Orthographic**, Size **5**.
- Background: a light sky blue.

### Ship
- *Create > 2D > Sprites > Square* (saves a Square sprite asset; put it in `Sprites/`).
- Drag the square into the scene and name it `Ship`.
- Position `(0, 1, 0)`, Scale `(2, 1, 1)`, Color: brown.

### CloudSea
- Create Empty `CloudSea` at `(0, -10, 0)`. The script moves it; the starting position doesn't matter much.
- Child `CloudVisual`: Square sprite, local position `(0, -5, 0)`, scale `(30, 10, 1)`, color white.
  - This puts the **top edge** of the clouds exactly at the `CloudSea` origin, so the parent's Y equals the cloud surface.
- Set the `CloudVisual` **Order in Layer** to `10` so the clouds draw over the ship.

### Furnace
- Create Empty `Furnace`. It has no visual yet; Phase 2 moves it into the cabin.

### Managers
- Create Empty `Managers`.

### Canvas
- *UI > Canvas*. Canvas Scaler: **Scale With Screen Size**, reference `1920 × 1080`.
- `DebugText`: anchor top-left, about `400 × 250`, font size 28, color black.
- `GameOverPanel`: stretch to full screen, color black at ~70% alpha.
  - `GameOverText`: centered, font size 72, white, text: `GAME OVER\nPress R to restart`.

---

## Step 3: Scripts

Create these in `Assets/Scripts/`. **The file name must match the class name.**

### `ResourceManager.cs`

```csharp
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private int startingCoal = 20;

    public int Coal { get; private set; }

    private void Awake()
    {
        Instance = this;
        Coal = startingCoal;
    }

    public bool TrySpendCoal(int amount = 1)
    {
        if (Coal < amount) return false;
        Coal -= amount;
        return true;
    }

    public void AddCoal(int amount)
    {
        Coal += amount;
    }
}
```

### `GameManager.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        if (IsGameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }
}
```

### `Furnace.cs`

Hotter furnace = faster climb, but it also burns heat faster. That's the "more coal goes faster but runs out faster" tradeoff.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class Furnace : MonoBehaviour
{
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float startingHeat = 50f;
    [SerializeField] private float heatPerCoal = 20f;
    [SerializeField] private float baseBurnRate = 4f;       // heat lost per second, always
    [SerializeField] private float burnRatePerHeat = 0.04f; // extra loss per point of heat (hotter burns faster)

    public float Heat { get; private set; }
    public float MaxHeat => maxHeat;
    public float Heat01 => Heat / maxHeat;

    private void Awake()
    {
        Heat = startingHeat;
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        // TEMP (Phase 1): Space adds coal. Phase 2 replaces this with clicking the furnace.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            AddCoal();

        float burn = baseBurnRate + Heat * burnRatePerHeat;
        Heat = Mathf.Max(0f, Heat - burn * Time.deltaTime);
    }

    public void AddCoal()
    {
        if (Heat >= maxHeat) return; // don't waste coal on a full furnace
        if (!ResourceManager.Instance.TrySpendCoal()) return;
        Heat = Mathf.Min(maxHeat, Heat + heatPerCoal);
    }
}
```

### `ShipController.cs`

```csharp
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private Furnace furnace;
    [SerializeField] private float startingAltitude = 100f;
    [SerializeField] private float maxLiftSpeed = 15f; // m/s up at full heat
    [SerializeField] private float sinkSpeed = 5f;     // m/s down, always applied
    [SerializeField] private float acceleration = 4f;  // how quickly speed approaches its target

    public float Altitude { get; private set; }
    public float MaxAltitude { get; private set; }
    public float VerticalSpeed { get; private set; }

    // Extra sink in m/s. Phase 4: milk jugs add to this.
    public float Weight { get; set; }

    private void Awake()
    {
        Altitude = startingAltitude;
        MaxAltitude = startingAltitude;
    }

    private void Update()
    {
        float targetSpeed = furnace.Heat01 * maxLiftSpeed - sinkSpeed - Weight;
        VerticalSpeed = Mathf.MoveTowards(VerticalSpeed, targetSpeed, acceleration * Time.deltaTime);

        Altitude += VerticalSpeed * Time.deltaTime;
        MaxAltitude = Mathf.Max(MaxAltitude, Altitude);
    }
}
```

With these defaults, the ship needs **more than ~33% heat** just to hold steady.

### `CloudSea.cs`

```csharp
using UnityEngine;

public class CloudSea : MonoBehaviour
{
    [SerializeField] private ShipController ship;
    [SerializeField] private float startingAltitude = 0f;
    [SerializeField] private float riseSpeed = 2f;           // m/s at start
    [SerializeField] private float riseAcceleration = 0.02f; // rise speed gained per second
    [SerializeField] private float maxGap = 150f;            // clouds never fall further behind than this
    [SerializeField] private float unitsPerMeter = 0.1f;

    public float Altitude { get; private set; }

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
        position.y = ship.transform.position.y + gap * unitsPerMeter;
        transform.position = position;

        if (Altitude >= ship.Altitude)
            GameManager.Instance.GameOver();
    }
}
```

### `DebugHUD.cs`

```csharp
using TMPro;
using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private ShipController ship;
    [SerializeField] private Furnace furnace;
    [SerializeField] private CloudSea cloudSea;

    private void Update()
    {
        text.text =
            $"Altitude: {ship.Altitude:0} m (best {ship.MaxAltitude:0})\n" +
            $"Speed: {ship.VerticalSpeed:+0.0;-0.0} m/s\n" +
            $"Coal: {ResourceManager.Instance.Coal}\n" +
            $"Heat: {furnace.Heat:0} / {furnace.MaxHeat:0}\n" +
            $"Clouds: {ship.Altitude - cloudSea.Altitude:0} m below";
    }
}
```

---

## Step 4: Hook It Up

| GameObject | Add Component | Assign in Inspector |
|---|---|---|
| `Managers` | `GameManager` | Game Over Panel → `GameOverPanel` |
| `Managers` | `ResourceManager` | — |
| `Furnace` | `Furnace` | — |
| `Ship` | `ShipController` | Furnace → `Furnace` |
| `CloudSea` | `CloudSea` | Ship → `Ship` |
| `Canvas` | `DebugHUD` | Text → `DebugText`, Ship, Furnace, Cloud Sea |

---

## Step 5: Test

Press Play.

1. **Do nothing:** heat drains, the ship slows and sinks, the clouds rise into view from the bottom and the game ends.
2. **Spam Space:** heat hits 100, the ship climbs fast, and coal drops quickly.
3. **Tap Space steadily:** you can hover around 40–60% heat and stretch your coal.
4. **Run out of coal:** you can't add heat anymore and you eventually lose. (Expected for now. Resource balloons come in Phase 4.)
5. **At game over:** the panel shows, and **R** reloads the scene.

### If something's wrong
- **NullReferenceException:** an Inspector field isn't assigned (see the Step 4 table).
- **R doesn't restart:** `Main` isn't in the Build Profiles scene list.
- **Keyboard does nothing:** Active Input Handling is set to Old (see Step 1).
- **Clouds are invisible:** check the `CloudVisual` Order in Layer, and make sure the cloud color isn't the same as the sky.

---

## Tuning Knobs

All of these are in the Inspector, so you can tweak them while in Play mode (changes made in Play mode reset when you stop; note good values down).

| Feel | Tweak |
|---|---|
| Game too hard / short | ↑ `startingCoal`, ↑ `heatPerCoal`, ↓ cloud `riseSpeed` |
| Climbing feels sluggish | ↑ `maxLiftSpeed`, ↑ ship `acceleration` |
| Coal lasts too long | ↑ `burnRatePerHeat` |
| Not enough pressure | ↑ `riseAcceleration`, ↓ `maxGap` |

---

## Step 6: Commit

Once it plays end to end, commit to git: `Phase 1: altitude, coal, cloud sea, game over`.

**Next up: Phase 2** adds the cabin zone, smooth camera switching, the PiP render-texture views, and clicking the furnace instead of pressing Space.
