# 04 Survivor Mini

A complete (tiny) game loop built only from JamKit components and primitive meshes — no art needed. Move around a tilted top-down arena and grab the gold spheres before the timer runs out; then it loads the GameOver scene with your score.

Demonstrates how the pieces compose: `Mover3D`, `Spawner` + `PoolService`, `Pickup` + `AutoDespawn`, `ScoreService`, `TimerService`, and a HUD.

## Setup

1. Run `JamKit > New Jam Project` so the service SOs exist and the Game / GameOver scenes are wired.
2. Open `Assets/_Project/Scenes/Game.unity` (it already has a `JamKitCore`, so the pool/timer/scene runners are present).
3. Add an empty GameObject with the `SurvivorDemo` component.
4. Inspector — assign from `Assets/_Project/Services/`:
   - `InputService`, `PoolService`, `ScoreService`, `TimerService`, `SceneService`
5. Press Play. Move with WASD / left stick, collect spheres for points, survive the countdown.
6. When the timer hits zero it loads `GameOver`, which shows your score and best (persisted by `SaveService`).

The whole arena is built at runtime in `Awake`, so the GameObject only needs the component + service references.

## No-code HUD (optional)

The sample updates its HUD labels in `Update` for clarity. To do it the JamKit way instead — zero code:

1. Build a HUD UXML with a `Label` named `score` and a `Label` named `time`.
2. Add two `LabelBinding` components to the HUD GameObject:
   - one with `ElementName = "score"`, `Variable = ScoreService.ScoreVariable`, `Format = "Score: {0:0}"`
   - one with `ElementName = "time"`, `Variable = TimerService.TimeVariable`, `Format = "{0:0.0}s"`
3. For an HP/charge bar, use `BarBinding`: point it at a fill element and a `FloatVariableSO` (e.g. `Health.CurrentVariable`).

## Juice it (Feel)

Wire `Pickup.OnCollected` (a Ripple `VoidEventSO`) to an `MMF_Player.PlayFeedbacks()` via UltEvents for a pop + sound on every grab — no code change.
