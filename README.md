# JamKit

Rapid game-jam toolkit for Unity 6.6. It ships the parts every jam rebuilds in hour one, so that hour goes to the game instead: menus, pause, settings, scene flow, screen fades, audio, tweens, camera shake and a small set of gameplay components.

Read [PILLARS.md](PILLARS.md) for the rules the kit is built to.

| Layer | Owns |
| --- | --- |
| **[Ripple](https://github.com/123tris/Ripple)** | State and events: variables, event assets, listeners, runtime sets, and the `Signal` type every component uses for its events |
| **JamKit** | Services, audio, juice, gameplay components, UI Toolkit menus, editor tooling |
| **Feel**, **FMOD** | Optional. Feel for deeper feedback authoring, FMOD as an audio backend |

## Requirements

- Unity 6.6 (6000.6) or newer, with URP, Cinemachine 3 and the Input System
- **Odin Inspector**
- **`com.metz.ripple`** 0.2.0: `https://github.com/123tris/Ripple.git#v0.2.0`

UltEvents is no longer used anywhere in the stack.

## Quickstart

1. Install Odin, Ripple, then JamKit.
2. Run `JamKit > New Jam Project`. It creates, under `Assets/_Project`:
   - `Services/`: Time, Scene, Input and Screen Fade service assets
   - `Variables/`: volumes and high score (persistent), score, timer, and a `Players` runtime set
   - `Events/` and `GameFlow.asset`: the Play, MainMenu, Restart and GameOver events and which scene each one leads to
   - `Prefabs/`: `JamKitCore`, `JamKitMenu` and the starter prefabs
   - `Scenes/`: Bootstrap, Game and GameOver
3. Press Play. Start, Settings, Game (Esc pauses), GameOver and Retry work end to end.

To end a run, raise the `GameOver` event from anything, for example by adding it to a `Health.OnDied` signal.

## Events: Signal

Every event on a JamKit component is a Ripple `Signal`. In the inspector a Signal is a standard `UnityEvent` plus a list of Ripple event assets to raise. In code it takes listeners.

```csharp
public Signal<float> OnDamaged = new();
public Signal OnDied = new();

health.OnDied.AddListener(HandleDeath);
health.OnDamaged += amount => Debug.Log(amount);
```

One event on one object reacts through the `UnityEvent`. Anything global is a Ripple event asset in the raise list, heard elsewhere with Ripple's `EventListener` components.

## Services

Services are ScriptableObject assets. They host themselves: nothing has to be in the scene for them to work, and a component dropped into an empty scene runs.

| Service | What it does |
| --- | --- |
| `TimeServiceSO` | The one owner of `Time.timeScale`. `Push(scale)` returns a modifier you `Release()`; the lowest active modifier wins, so pause always beats slow-mo and hit-stop. `Pause`, `Resume`, `FreezeForSeconds`, `PushSmooth`. |
| `SceneServiceSO` | `Load(name)` and `Reload()`, fading out and back in through an optional `ScreenFadeSO`. `OnLoadStarted` and `OnLoadCompleted` signals. `LoadAsync` returns an `Awaitable`. |
| `ScreenFadeSO` | `FadeOut`, `FadeIn`, `Flash`, callable from any Signal. |
| `InputServiceSO` | Owns the input mode: `SwitchToGameplay` and `SwitchToUI` enable one action map and release or lock the cursor. |

Components read input through `InputActionReference` fields, so a custom action needs no kit change.

`GameFlow` maps Ripple events to scenes. A `GameFlowDriver` in each scene (on `JamKitCore`) listens for the events that apply there. On Unity 6.4 or newer the same asset can be authored as a graph: `Assets > Create > JamKit > Game Flow Graph`.

## Audio

A sound is an asset with `Play()` and `PlayAt(position)`. Components and Signals reference the abstract `Sound` and `Music` types, so the backend is chosen by which asset you create.

- **Unity:** `ClipSound` plays an `AudioResource` (a clip, or an Audio Random Container for variation). `ClipMusic` crossfades and keeps playing across scene loads. `MixerVolume` binds a Ripple volume variable to a mixer parameter.
- **FMOD:** `FmodSound`, `FmodMusic` and `FmodBusVolume`, compiled when FMOD for Unity is installed. This backend has not been compiled or run yet.

Wire `Health.OnDamaged → HitSound.Play()` directly; there is no audio service to reference.

## Juice

- `Tween.Run(seconds, step, ease)` and the `MoveTo`, `ScaleTo` and `FadeTo` extensions
- `PunchScale`, `ColorFlash` and `CameraShake` components, each with a `Play` method to wire from a Signal
- `HitStop` for freeze-frames through the time service

## Components

- **Movement:** `Mover2D`, `Mover3D`, `FirstPersonLook`, `ChaseMover`, `PatrolMover`
- **Combat:** `Health`, `HealthDrain`, `Damager`, `Respawner`, `HitStop`
- **Spawning:** `Spawner`, `SpawnBurst`, `ProjectileShooter`, `AutoDespawn`
- **Triggers and rules:** `TriggerZone`, `Interactor` and `Interactable`, `GameTimer`, `HighScoreTracker`, `LifecycleSignals`, `SceneLoader`
- **UI:** `MenuController` and `MenuView`, `Toast`, `DebugPanel`, and the `ValueLabel` and `ValueBar` controls for HUDs bound in UXML

A pickup is a `TriggerZone` with `DestroySelf` and `OnEntered → Score.Add`. A kill zone is a trigger collider with a `Damager` set to `Kill`.

## Editor tooling

- **Main toolbar:** scene switcher, play-from-first-scene toggle, time scale slider, Doctor and Build buttons
- **`JamKit > New Jam Project`:** the scaffold above
- **`JamKit > Doctor`:** project checks with fix buttons
- **Auto-assign:** adding a JamKit component fills its service references, and its required `InputActionReference` fields by name, when there is exactly one candidate
- **`GameObject > JamKit`:** places starter prefabs

## Known gaps

- The five samples still reference components and event data from before 1.0 and need re-authoring in the editor.
- The settings sliders are bound from C# (`MenuSettingsBinder`). Ripple variables are UI Toolkit data sources, so they can move to UXML bindings like the HUD values did.
- The WebGL build is still a menu command, not a Build Profile.

## License

MIT. See [LICENSE.md](LICENSE.md).
