# Phase 2: Cabin View, Camera Switching, PiP, Clicking the Furnace

**Goal:** Add the cabin below the outside view, scroll smoothly between them, show the "other" view in a picture-in-picture rectangle, and replace the Space key with clicking the furnace.

**Builds on:** Phase 1 (altitude, coal, cloud sea, game over).

**Not in this phase:** steering, obstacles, milk jugs, balloons, the altitude meter UI.

**Done when:**
- [ ] Scroll wheel (and W/S or Up/Down) fades the whole screen briefly while switching between the outside and cabin
- [ ] In the outside view, a PiP rect at the **bottom** of the screen shows the cabin live
- [ ] In the cabin view, a PiP rect at the **top** of the screen shows the outside live
- [ ] Clicking the furnace adds coal; Space no longer does
- [ ] The furnace visibly reacts to heat (color/glow)
- [ ] There's a reusable click system that Phase 3 (wheel) and Phase 4 (milk jugs) can plug into

---

## Design

### World layout

Both zones live in the same scene, stacked far apart so they never overlap.

```
y =   0   ── OUTSIDE ZONE ──  Ship, cloud sea, (later) obstacles
            ...
y = -50   ── CABIN ZONE ────  Cabin background, Furnace, (later) Wheel, Harpoon
```

- `-50` is safe: the cloud sea is never more than ~25 units below the ship (`maxGap` 150 m × 0.1 + the 10-unit visual).
- Put each zone under its own parent (`OutsideZone`, `CabinZone`) so the zones can be moved around easily.

### Camera switching

- `CameraSwitcher` has two target Y positions (outside = 0, cabin = -50).
- Input: scroll down / S / Down Arrow goes to the cabin; scroll up / W / Up Arrow goes to the outside.
- The full-screen transition overlay fades to black, then the main camera and PiP jump to their new positions while obscured, and the screen fades back in. Keep this transition short (about 0.26 seconds total).
- The camera's projection and rendering settings do not change during a view switch; only its Y position changes while the screen is fully covered.
- It exposes `CurrentView` (an `Outside` / `Cabin` enum) and an `OnViewChanged` event so the PiP (and later the UI) can react after the screen is covered.
- It uses **unscaled** time, so it still works on the game over screen.

### PiP (picture-in-picture)

- **PipCamera:** a second orthographic camera, same size as the main camera, rendering to a **Render Texture** (`PipTexture`, 640×360).
- **PipView:** a UI `RawImage` on the Canvas showing `PipTexture`, about 480×270, with a thin border (an `Image` behind it, slightly larger).
- **TransitionFade:** a full-screen black UI `RawImage`, last in the Canvas draw order, whose alpha is controlled by `CameraSwitcher`.
- When the view changes:
  - The screen fades fully to black.
  - Outside view: the PipCamera moves to the cabin (y = -50) and the rect anchors to the **bottom-center**.
  - Cabin view: the PipCamera moves to the outside (y = 0) and the rect anchors to the **top-center**.
  - The screen fades back in after both cameras and the PiP layout have updated.
- **Bonus (cheap):** clicking the PiP rect switches to that view.

### Click system

One central script reads the mouse and talks to anything clickable. Phase 3 and 4 reuse this.

```csharp
public interface IClickable
{
    void OnPress();   // mouse went down on this object
    void OnRelease(); // mouse went up (anywhere) after pressing this object
}
```

`ClickInput` (on Managers):
- On left mouse **down**:
  - If the pointer is over UI (`EventSystem.current.IsPointerOverGameObject()`), ignore it, so UI clicks don't hit the world.
  - Convert to a world point with the main camera (`ScreenToWorldPoint`), then `Physics2D.OverlapPoint`.
  - If the hit object has an `IClickable`, call `OnPress()` and remember it as `held`.
- On left mouse **up**: call `held.OnRelease()` and clear `held`.
- Ignore input while `GameManager.Instance.IsGameOver`.
- Clicking only works on what the **main** camera sees. Clicks in the PiP don't reach the world (this is intended; the bonus above handles the PiP).

### Furnace changes

- Remove the Space-key block from `Furnace.Update`.
- `Furnace` implements `IClickable`; `OnPress()` calls `AddCoal()`.
- Add a `SpriteRenderer` + `BoxCollider2D` to the Furnace and move it into `CabinZone`.
- Heat feedback: lerp the sprite color from dark gray (cold) to bright orange (hot) using `Heat01`.
- Optional juice: a small scale "punch" when coal goes in, and a "no coal" shake or flash when empty.

---

## Steps

### 1. Scene

```
Main Camera              (+ CameraSwitcher)
PipCamera                (Camera, Target Texture = PipTexture, no AudioListener)
OutsideZone              (empty, y = 0)
├── Ship
└── CloudSea
    └── CloudVisual
CabinZone                (empty, y = -50)
├── CabinBackground      (Square sprite, 18 × 10, dark wood color, Order -10)
└── Furnace              (Square sprite 2 × 2, BoxCollider2D, + Furnace)
Managers                 (+ GameManager, ResourceManager, ClickInput)
Canvas
├── DebugText
├── PipFrame             (Image, dark border)
│   └── PipView          (RawImage, texture = PipTexture, + PipClickToSwitch)
├── GameOverPanel
└── TransitionFade       (full-screen black RawImage, alpha controlled by CameraSwitcher)
EventSystem
```

- **Render Texture:** *Create > Rendering > Render Texture*, then save it as `Assets/Rendering/PipTexture` (640 × 360).
- **PipCamera:** copy the Main Camera's settings (orthographic, size 5, background color). Remove its `AudioListener`. URP: make sure **Render Type = Base**.
- **Ship / CloudSea:** move them under `OutsideZone`. The `CloudSea` script positions itself in world space, so this is fine.

### 2. Scripts

| Script | New/Changed | Responsibility |
|---|---|---|
| `IClickable.cs` | New | The interface above |
| `ClickInput.cs` | New | Mouse → world raycast → `OnPress`/`OnRelease` |
| `CameraSwitcher.cs` | New | View input/state, brief full-screen fade, hidden camera reposition, `OnViewChanged` event |
| `PipController.cs` | New | Listens to `OnViewChanged`; moves the PipCamera and re-anchors the PiP rect |
| `PipClickToSwitch.cs` | New (bonus) | `IPointerClickHandler` on the RawImage → `CameraSwitcher.ToggleView()` |
| `Furnace.cs` | Changed | Remove Space input; implement `IClickable`; heat color |

#### `PipController` sketch

```csharp
private void HandleViewChanged(View view)
{
    bool outside = view == View.Outside;
    // PiP shows the *other* zone
    pipCamera.transform.position = new Vector3(0f, outside ? cabinY : outsideY, -10f);
    // Rect: bottom-center when outside, top-center when in the cabin
    float a = outside ? 0f : 1f;
    pipFrame.anchorMin = pipFrame.anchorMax = pipFrame.pivot = new Vector2(0.5f, a);
    pipFrame.anchoredPosition = new Vector2(0f, outside ? 20f : -20f);
}
```

Call it once in `Start` so it's correct from the first frame.

### 3. Scene builder

Extend `Phase1SceneBuilder` (or add `Phase2SceneBuilder`) to add the zones, PipCamera, Render Texture, PiP UI, and furnace sprite/collider, so the scene can be regenerated with one click.

---

## Test

1. Scroll down: the screen briefly fades to black, then reveals the cabin. The PiP at the top shows the ship and clouds live.
2. Scroll up: the screen briefly fades to black, then reveals outside. The PiP at the bottom shows the cabin and the furnace glowing.
3. Click the furnace in the cabin: coal goes down and heat goes up. Space does nothing.
4. Spam scroll in both directions: no camera motion is visible under the fade and the PiP always shows the opposite zone.
5. Clicking on the PiP or debug text doesn't click through to the world.
6. Game over still works, and R restarts.

### Gotchas
- **PiP is black:** the PipCamera's Target Texture isn't set, or the RawImage texture isn't set.
- **Clicks do nothing:** the Furnace has no `Collider2D`, or `ClickInput` isn't in the scene.
- **The PiP shows the UI:** the PipCamera shouldn't render the UI. It won't with a Screen Space Overlay canvas, which is the default.
- **The transition is not obscuring the full screen:** make sure `TransitionFade` is the last Canvas child and fills the Canvas.

---

## Tuning Knobs

| Feel | Tweak |
|---|---|
| Switching feels slow/floaty | ↓ `fadeOutDuration` / `fadeInDuration` |
| Switching feels abrupt | Slightly ↑ `fadeInDuration` (keep the total transition short) |
| PiP too small to read | ↑ RawImage size, or ↓ PipCamera ortho size to zoom in |
| Scroll too sensitive (trackpads) | Ignore scroll values below a small threshold |

---

## Commit

`Phase 2: cabin view, camera switching, PiP, furnace clicking`

**Next up: Phase 3** adds the steering wheel, world scrolling, and the first obstacle.
