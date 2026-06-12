# JamKit

Rapid game-jam toolkit for Unity 6. **ScriptableObject service architecture, no singletons.** Ripple-native event/variable wiring, UI Toolkit menus, and a recommended integration path with the Feel asset for juice.

## Why

Every jam burns the first hours on the same scaffolding: a start menu, settings, audio mixer, scene transitions, pooling, save/load, juice. JamKit ships all of it pre-wired so you can spend hour 1 on the actual game — without inheriting a pile of singletons.

## Architecture at a glance

- **No global statics.** No `ServiceLocator`, no `Singleton<T>`, no auto-spawned root. Every system is a `ScriptableObject` (the "service") plus an optional scene `Runner` MonoBehaviour. Components hold serialized SO references — you can see exactly what each thing depends on by looking at its inspector.
- **Ripple-native.** [`com.metz.ripple`](https://github.com/<you>/Ripple) (your SO Variables/Events framework) is a hard dependency. Volume sliders are bound to `FloatVariableSO`s; damage and death broadcast through `FloatEvent` / `VoidEventSO`. Designers wire reactions in the inspector with UltEvents.
- **Feel for juice.** Screen shake, hit-freeze, sprite flash, floating text, etc. are not in JamKit — Feel does that better. Wire any Ripple event to `MMF_Player.PlayFeedbacks()` via UltEvents and the designer gets full control without code.

## Requirements

- Unity 6 (6000.0+)
- URP 17+, Cinemachine 3.1+, Input System 1.11+, UGUI 2.0+ (auto-resolved as UPM dependencies)
- **Kybernetik UltEvents** (Asset Store / UPM)
- **`com.metz.ripple`** (your local fork or UPM)
- Optional but recommended: **MoreMountains Feel** (Asset Store) — recommended for all juice

The asmdefs have a `ULTEVENTS` define constraint (auto-defined by the UltEvents package), so JamKit compiles once UltEvents + Ripple are present. No paid assets are required to compile.

## Install

Add to your Unity 6 project's `Packages/manifest.json`:

```jsonc
"com.metz.jamkit": "file:/absolute/path/to/Packages/com.metz.jamkit"
```

Or `Window > Package Manager > + > Add package from disk`.

Ensure UltEvents and Ripple are already in the project before adding JamKit.

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

- **Movement / camera:** `Mover2D`, `Mover3D`, `CinemachineFollow2D`, `CinemachineFollow3D`.
- **Combat:** `Health` (Ripple variable + events), `Damager` / `Damager2D` (simple), and `Hitbox` + `Hurtbox` (weak points, one hit per swing). `Knockback` helper for hit reactions.
- **Spawning:** `Spawner` (interval, concurrent `MaxAlive`), `WaveSpawner` (sequenced waves), `ProjectileShooter` (pooled firing), `AutoDespawn` (pool-aware lifetime), `Pickup` (trigger → score/event/despawn).
- **UI / HUD:** `MenuController` (Start/Settings/Pause), `PauseController`, `GameOverController`, `FadeOverlay`, `DebugPanel`, and the bindings `LabelBinding` / `BarBinding` that drive a UI Toolkit label/bar from a `FloatVariableSO` with no code.
- **Utilities:** `Timer` / `Stopwatch` / `Cooldown` structs, `FSM<TState>`, `ObjectPool<T>`, `RandomBag<T>`, and the `MathX` / `VectorX` / `ColorX` / `GizmoX` extension helpers.

## Ripple integration

JamKit is built on Ripple. Default uses:

- `AudioServiceSO.MasterVolume / MusicVolume / SfxVolume` are `FloatVariableSO`s — bind UI sliders to them, listeners react automatically.
- `Health.OnDamaged` is a `FloatEvent`, `Health.OnDied` is a `VoidEventSO`. Wire them to your HUD or to Feel.
- `Health.CurrentVariable` is an optional `FloatVariableSO` — bind your HP bar to it.
- `WaveSpawner.OnWaveStarted` / `OnWaveEnded` are `IntEvent`s, `OnAllWavesDone` is a `VoidEventSO`.
- `SceneServiceSO.OnSceneLoadStarted / OnSceneLoadCompleted` are `VoidEventSO`s.
- `Spawner` and `WaveSpawner` reuse Ripple events for designer wiring.

## Feel integration

JamKit does not ship its own screen shake / hit-freeze / sprite flash / floating text — that's Feel's job. The pattern is:

1. Add an `MMF_Player` GameObject in the scene (or a prefab in a Feel-only manager).
2. Configure feedbacks: Camera Shake, Sprite Flicker, Freeze Frame, Floating Text, etc.
3. In your Ripple event asset (e.g. `Health.OnDamaged`), open the UltEvent inspector.
4. Add a persistent call to `MyMMFPlayer.PlayFeedbacks()`.

Result: damage triggers full Feel polish without any compile-time coupling between JamKit and Feel. Designers tune feedbacks without touching code.

## Editor menu

- `JamKit > New Jam Project` — one-click full setup (see Quickstart).
- `JamKit > Create Bootstrap Scene Only` — drops the bootstrap scene + services into an existing project.
- `JamKit > Create Menu Prefab` — saves a reusable `JamKitMenu.prefab` (assign services after dragging in).
- `JamKit > Setup > Create Audio Mixer` / `Create Panel Settings` — re-creates the assets if you deleted them.

## Samples

Open `Window > Package Manager > JamKit > Samples` and import:

- **00 Bootstrap** — Pool spawning, AudioServiceSO injection, Ripple click event.
- **01 2D Platformer Mini** — Mover2D + Cinemachine 2D + Health with Ripple events. Wire OnDamaged to a Feel MMF_Player for juice.
- **02 3D Walker Mini** — Mover3D + AudioServiceSO + pickup `AudioClipEvent`.
- **03 UI Card Flip Mini** — UI Toolkit demo with SaveServiceSO-backed persistent high score.
- **04 Survivor Mini** — a full tiny loop: Mover3D + Spawner/Pool + Pickup + Score/Timer services + HUD + GameOver, using only primitive meshes.

## License

MIT. See [LICENSE.md](LICENSE.md).
