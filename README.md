# JamKit

Rapid game-jam toolkit for Unity 6. **ScriptableObject service architecture, no singletons.** Ripple-native event/variable wiring, UI Toolkit menus, built-in Juice Lite feedback layer, genre primitives for the classic jam archetypes, and a clean graduation path to the Feel asset.

## Why

Every jam burns the first hours on the same scaffolding: a start menu, settings, audio mixer, scene transitions, pooling, save/load, juice. JamKit ships all of it pre-wired so you can spend hour 1 on the actual game — without inheriting a pile of singletons.

## Architecture at a glance

- **No global statics.** No `ServiceLocator`, no `Singleton<T>`, no auto-spawned root. Every system is a `ScriptableObject` (the "service") plus an optional scene `Runner` MonoBehaviour. Components hold serialized SO references — you can see exactly what each thing depends on by looking at its inspector. The *editor* auto-fills those references when there's exactly one candidate; the runtime never looks anything up.
- **Ripple-native.** [`com.metz.ripple`](https://github.com/<you>/Ripple) (your SO Variables/Events framework) is a hard dependency. Volume sliders are bound to `FloatVariableSO`s; damage and death broadcast through `FloatEvent` / `VoidEventSO`. Designers wire reactions in the inspector with UltEvents.
- **Two event granularities.** Ripple SO events are *global* (any listener hears every instance) — right for HUDs, stingers, screen shake. For per-instance reactions (only the hit enemy flashes), `Health` also exposes plain C# events (`Damaged` / `Healed` / `Died`), and every Juice Lite component subscribes to its sibling `Health` automatically — zero wiring.
- **Juice Lite built-in, Feel for depth.** Screen shake, hit-stop, flashes, punches, floating text, and toasts ship in the box with zero extra dependencies. Feel remains the deluxe path: point the same triggers at an `MMF_Player.PlayFeedbacks()` and delete nothing.

## Requirements

- Unity 6 (6000.0+)
- URP 17+, Cinemachine 3.1+, Input System 1.11+, UGUI 2.0+ (auto-resolved as UPM dependencies)
- **Kybernetik UltEvents** (Asset Store / UPM)
- **`com.metz.ripple`** (your local fork or UPM)
- Optional but recommended: **MoreMountains Feel** (Asset Store) — recommended for all juice

The asmdefs have a `ULTEVENTS` define constraint (auto-defined by the UltEvents package), so JamKit compiles once UltEvents + Ripple are present. No paid assets are required to compile.

## Install

Order matters: UltEvents and Ripple first, then JamKit.

1. **UltEvents** — Asset Store (or UPM if you have a source copy).
2. **Ripple** — one of:
   ```jsonc
   // git URL (default — note the branch pin; Ripple's main is an older API):
   "com.metz.ripple": "https://github.com/123tris/Ripple.git#feature/abstraction+runtime-registry"
   // local checkout (for developing Ripple itself):
   "com.metz.ripple": "file:C:/Repos/Ripple"
   ```
3. **JamKit**:
   ```jsonc
   "com.metz.jamkit": "file:/absolute/path/to/Packages/com.metz.jamkit"
   ```
   Or `Window > Package Manager > + > Add package from disk`.

## Quickstart

1. Install JamKit.
2. Open `JamKit > New Jam Project`. The wizard creates:
   - `Assets/_Project/Services/` — eight SO assets (Audio, Time, Scene, Input, Save, Pool, Score, Timer)
   - `Assets/_Project/Variables/` — Ripple `FloatVariableSO`s for Master / Music / SFX and Score / HighScore / Timer
   - `Assets/_Project/Audio/Resources/JamKitMixer.mixer` — audio mixer
   - `Assets/_Project/UI/Resources/JamKitPanelSettings.asset` — UI Toolkit panel settings
   - `Assets/_Project/Scenes/Bootstrap.unity`, `Game.unity`, `GameOver.unity` — each with a self-contained `JamKitCore` (all runners) and an `EventSystem` for gamepad/keyboard nav
3. Press Play. Start menu → Settings → Game (Esc to pause) → GameOver works end-to-end, gamepad included.

To add JamKit functionality in your own scenes: reference the service SOs in your components' inspectors. The wizard makes them all easy to find under `Assets/_Project/Services/`.

## Service SOs

Each service is a `ScriptableObject` you can drop into any component's inspector. They expose plain methods — no static accessors.

| Service | Type | What it does | Needs a scene Runner? |
| --- | --- | --- | --- |
| Audio  | `AudioServiceSO`  | `PlaySfx(clip)`, `PlayMusic(clip)`. Routes through an AudioMixer; volume is Ripple-bound. | Yes — `AudioServiceRunner` (audio sources). |
| Time   | `TimeServiceSO`   | `Pause()`, `Resume()`, `Push(scale)`, `Pop()`. Stack composes pause + freeze + slow-mo. | Only for `FreezeForSeconds` (needs a coroutine driver). |
| Scenes | `SceneServiceSO`  | `LoadAsync(name)`, `ReloadCurrent()`. Optional Ripple `VoidEventSO`s announce load start/end. | Yes — `SceneServiceRunner` (coroutine + fade). |
| Input  | `InputServiceSO`  | Exposes Move / Look / Jump / Attack / Pause from a configured `InputActionAsset`. | No — but call `SwitchToGameplay()` / `SwitchToUI()` to switch maps. |
| Save   | `SaveServiceSO`   | `Write(key, T)` / `Read<T>(key, fallback)`. JSON to `persistentDataPath`. | No. |
| Pool   | `PoolServiceSO`   | `Spawn(prefab, pos, rot)`, `Despawn(go)`. One pool per prefab. | Optional — `PoolServiceRunner` provides a clean parent transform. |
| Score  | `ScoreServiceSO`  | `Add(n)`, `Set(n)`, `ResetScore()`. High score persists via `SaveService`; mirrors to Ripple variables for HUD binding. | No. |
| Timer  | `TimerServiceSO`  | `StartTimer(seconds)`, `Pause()`, `Resume()`, count down or up. Fires `OnTimerComplete`. | Yes — `TimerServiceRunner` advances it. |

## Components

Drop-in MonoBehaviours that reference service SOs in their inspector — never a static lookup.

- **Movement / camera:** `Mover2D` (platformer/top-down, per-axis lock for paddles), `Mover3D`, `GridMover` (frogger/sokoban stepping), `ThrustMover2D` (asteroids/lander), `ChaseMover` (2D+3D pursuit, find-by-tag), `PatrolMover` (waypoints, ping-pong/loop/conveyor), `Bouncer2D` (constant-speed arcade ball; add the `Paddle` marker to anything that should bend bounces by hit offset), `Aimer` (mouse/right-stick aiming), `ScreenWrap2D`, `CinemachineFollow2D`, `CinemachineFollow3D`.
- **Combat:** `Health` (Ripple + per-instance C# events, pool-aware refill), `Damager` / `Damager2D` (pool-aware), `Hitbox` + `Hurtbox` (weak points, one hit per swing), `Knockback`, `Respawner` (checkpoints, death respawn).
- **Spawning:** `Spawner` (interval, concurrent `MaxAlive`), `WaveSpawner` (sequenced waves), `ProjectileShooter` (pooled firing), `SpawnBurst` (death explosions / asteroid splits / loot drops), `AutoDespawn` (pool-aware lifetime), `Pickup` (trigger → score/event/despawn), `TriggerZone` (kill pit / goal / score gate / level exit in one component).
- **Interaction:** `Interactor` (on the player: nearest-target probe, 2D+3D, allocation-free) + `Interactable` ("press E" targets with a zero-plumbing prompt visual).
- **Juice Lite** (all trigger from sibling `Health`, Ripple events, or a public `Play()` — see below): `CameraShake`, `HitStop`, `SpriteFlash`, `MaterialFlash`, `PunchScale`, `ParticleBurst`, `SfxOnEvent`, `FloatingText` (+ scene `FloatingTextLayer`), `Toast`.
- **UI / HUD:** `MenuController` (Start/Settings/Pause), `PauseController`, `GameOverController`, `FadeOverlay`, `DebugPanel`, and the bindings `LabelBinding` / `BarBinding` that drive a UI Toolkit label/bar from a `FloatVariableSO` with no code.
- **Utilities:** `Timer` / `Stopwatch` / `Cooldown` structs, `FSM<TState>`, `ObjectPool<T>`, `RandomBag<T>`, and the `MathX` / `VectorX` / `ColorX` / `GizmoX` extension helpers.

## Juice Lite

Every jam game should feel good without paid assets. Each juice component is small and orthogonal, and triggers three ways (combinable):

1. **Sibling `Health`** — drop `SpriteFlash` + `PunchScale` on an enemy prefab and it flashes/pops on damage. No wiring at all.
2. **Ripple events** — point `CameraShake.PlayOn` at a shared event for global reactions.
3. **`Play()` / `Play(strength)`** — call from UltEvents, UnityEvents, or code.

Recipe for a juicy enemy in ~10 seconds: `GameObject > JamKit > 3D > Enemy (Chaser)` — it lands with chase movement, health, damage, flash, punch, and a death `SpawnBurst` pre-wired.

## Archetype coverage

The kit is genre-neutral; these compositions are the proof (each is minutes, not hours):

| Game | Composition |
| --- | --- |
| Pong / Breakout | `Paddle` preset + `Ball` preset (`Bouncer2D`) + `TriggerZone` goals + `Respawner` serve — two players via the bundled `Gameplay1`/`Gameplay2` keyboard-split maps ([walkthrough](Documentation~/pong-in-60-seconds.md)) |
| Frogger | `GridMover` player + `PatrolMover` cars (TeleportToStart) + `TriggerZone` water/goal + `Respawner` |
| Space Invaders | `Mover2D` (AxisScale 1,0) + `ProjectileShooter` + `Spawner`/`WaveSpawner` + `TriggerZone` bottom |
| Asteroids | `Ship` preset (`ThrustMover2D` + `ScreenWrap2D` + shooter) + `SpawnBurst` splits |
| Platformer | `Mover2D` + `PatrolMover` platforms + `TriggerZone` pits + `Respawner` checkpoints |
| Survivor / twin-stick | `Mover3D`/`Mover2D` + `Aimer` + `ChaseMover` enemies + waves/pickups/score (Sample 04) |

## Ripple integration

JamKit is built on Ripple. Default uses:

- `AudioServiceSO.MasterVolume / MusicVolume / SfxVolume` are `FloatVariableSO`s — bind UI sliders to them, listeners react automatically.
- `Health.OnDamaged` is a `FloatEvent`, `Health.OnDied` is a `VoidEventSO`. Wire them to your HUD or to Feel.
- `Health.CurrentVariable` is an optional `FloatVariableSO` — bind your HP bar to it.
- `WaveSpawner.OnWaveStarted` / `OnWaveEnded` are `IntEvent`s, `OnAllWavesDone` is a `VoidEventSO`.
- `SceneServiceSO.OnSceneLoadStarted / OnSceneLoadCompleted` are `VoidEventSO`s.
- `Spawner` and `WaveSpawner` reuse Ripple events for designer wiring.

## Graduating to Feel

Juice Lite is the 80%; Feel is the deluxe path on the exact same triggers:

1. Add an `MMF_Player` GameObject and configure feedbacks (Camera Shake, Sprite Flicker, Freeze Frame, Floating Text…).
2. In your Ripple event asset (e.g. `Health.OnDamaged`), add a persistent UltEvent call to `MyMMFPlayer.PlayFeedbacks()`.
3. Remove (or keep!) the Juice Lite components — nothing else changes.

No compile-time coupling between JamKit and Feel; designers tune feedbacks without touching code.

## Editor tooling

- **Auto-assign.** Adding any JamKit component auto-fills its null JamKit-typed references (service SOs from the project, scene components like `FloatingTextLayer`) — but only when exactly one candidate exists; anything ambiguous is left for you and surfaced by Validate. `JamKit > Auto-Assign References In Open Scenes` re-runs the pass.
- **`GameObject > JamKit > …` presets.** Pre-composed archetypes: 2D platformer/top-down/grid players, asteroids ship, chaser enemies (2D+3D), ball, paddle, patrol hazard, kill zones, follow cameras, spawner, pickup, floating-text layer, toast. Placeholder art, real wiring.
- **`JamKit > Validate Setup`.** Jam-day insurance: checks mixer exposure, PanelSettings themes, Build Settings, EventSystem, unassigned references, missing runners/layers — each with a Fix button where the fix is unambiguous.
- **`JamKit > Build > WebGL (itch.io)`.** One-click browser build with itch-safe settings (gzip + decompression fallback), then reveals the folder to zip.
- `JamKit > New Jam Project` — one-click full setup (see Quickstart). Output is **prefab-first**: `JamKitCore` (runners + fade + FloatingTextLayer + Toast) and `JamKitMenu` are prefabs instanced into every scene — edit once, all scenes update. Also offers fast enter-play-mode (domain reload off; JamKit's services are built for it).
- `JamKit > Create Bootstrap Scene Only` — drops the bootstrap scene + services into an existing project.
- `JamKit > Create Menu Prefab` — saves a reusable `JamKitMenu.prefab` (assign services after dragging in).
- `JamKit > Setup > Create Audio Mixer` / `Create Panel Settings` — re-creates the assets if you deleted them.

## Sound polish

- `MenuController` takes optional hover/click clips — hover covers gamepad focus too.
- `AudioServiceSO.PlayStinger(clip)` ducks the music under a one-shot; `SfxOnEvent` has a `DuckMusic` toggle for the same thing, no code.
- Two-player keyboard input ships in `JamKitInput`: maps `Gameplay1` (WASD) and `Gameplay2` (arrows). Make a second `InputServiceSO` pointing at `Gameplay2`, tick `AutoEnableGameplay`, assign it to player 2's mover — see [Pong in 60 seconds](Documentation~/pong-in-60-seconds.md).

## Samples

Open `Window > Package Manager > JamKit > Samples` and import:

- **00 Bootstrap** — Pool spawning, AudioServiceSO injection, Ripple click event.
- **01 2D Platformer Mini** — Mover2D + Cinemachine 2D + Health with Ripple events. Wire OnDamaged to a Feel MMF_Player for juice.
- **02 3D Walker Mini** — Mover3D + AudioServiceSO + pickup `AudioClipEvent`.
- **03 UI Card Flip Mini** — UI Toolkit demo with SaveServiceSO-backed persistent high score.
- **04 Survivor Mini** — a full tiny loop: Mover3D + Spawner/Pool + Pickup + Score/Timer services + HUD + GameOver, using only primitive meshes.
- **05 Juice Toggle** — the Juice Lite before/after: turret vs target, press J to flip every receiver at once (synthesized SFX, zero assets).
- **06 Arcade Playground** — frogger crossing + interactable lever + self-playing breakout in one scene: the any-genre kit composed.

Importing offers one-click setup, and `JamKit > Samples > Set Up <sample>` does the same any time: project scaffold if missing, a ready-to-play scene with a JamKitCore, the demo component with services assigned. Each sample README keeps the manual steps (they document what the button does) plus the Feel wiring to try.

## License

MIT. See [LICENSE.md](LICENSE.md).
