# JamKit

Rapid game-jam toolkit for Unity 6.5. It ships the parts every jam rebuilds in hour one, so that hour goes to the game instead: menus, pause, settings, scene flow, screen fades, audio, tweens, camera shake and a small set of gameplay components.

Read [PILLARS.md](PILLARS.md) for the rules the kit is built to.

| Layer | Owns |
| --- | --- |
| **[Ripple](https://github.com/123tris/Ripple)** | State and events: variables, event assets, listeners, runtime sets, and the `Signal` type every component uses for its events |
| **JamKit** | Services, audio, juice, gameplay components, UI Toolkit menus, editor tooling |
| **Feel**, **FMOD** | Optional. Feel for deeper feedback authoring, FMOD as an audio backend |

## Requirements

- Unity 6.5 (6000.5) or newer, with URP, Cinemachine 3 and the Input System
- **`com.metz.ripple`** 0.3.0: `https://github.com/123tris/Ripple.git#v0.3.0`

Nothing from the Asset Store: Odin Inspector and UltEvents are no longer used anywhere in the stack.

## Quickstart

1. Install Ripple, then JamKit.
2. Run `JamKit > New Jam Project`. It copies the project template into `Assets/_Project`:
   - `Services/`: Time, Scene, Input and Screen Fade service assets
   - `Audio/`: the mixer, and seven placeholder synth sounds the starters already play
   - `Variables/`: volumes and high score (persistent), score, timer, and a `Players` runtime set
   - `Events/` and `GameFlow.asset`: the Play, MainMenu, Restart and GameOver events and which scene each one leads to
   - `Prefabs/`: `JamKitCore`, `JamKitMenu` and the starters
   - `Scenes/`: Bootstrap, Game and GameOver, and `Templates/Level`, which New Level copies
   - `UI/`: the menu documents, their stylesheet and panel settings
3. Press Play. Start, Settings, Game (Esc pauses), GameOver and Retry work end to end.

Everything it copies is yours to edit. It never overwrites a file, so running it again only brings back what you deleted.

To end a run, raise the `GameOver` event from anything, for example by adding it to a `Health.OnDied` signal.

The Game scene's `Run` object resets `Score` while the scene loads. Variables that belong to one run, such as coins, kills or the wave number, go on its `OnEnabled` signal too.

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
| `ScreenFadeSO` | `FadeOut`, `FadeIn`, `Flash`, callable from any Signal. Each asset draws on its own overlay, and `FlashAlpha` sets how opaque a flash gets, so a second asset is a hit flash: the template's `DamageFlash` plays on the players' `OnDamaged`. |
| `InputServiceSO` | Owns the input mode. `SwitchToUI` turns on the UI map and silences every other map, so two-player maps pause too; `SwitchToGameplay` turns them back on. It also locks or releases the cursor. |

Components read input through `InputActionReference` fields, so a custom action needs no kit change.

`GameFlow` maps Ripple events to scenes. A `GameFlowDriver` in each scene (on `JamKitCore`) listens for the events that apply there. On Unity 6.4 or newer the same asset can be authored as a graph: `Assets > Create > JamKit > Game Flow Graph`.

## Audio

A sound is an asset with `Play()` and `PlayAt(position)`. Components and Signals reference the abstract `Sound` and `Music` types, so the backend is chosen by which asset you create.

- **Placeholders:** `SynthSound` synthesizes a sound from a preset (Pickup, Hit, Jump, Explosion, Powerup, Blip, Laser) and a seed, with no audio file. Randomize and Mutate pick another one while playing. The starters' hits, deaths and pickups play the template's synth sounds; swap an asset for a `ClipSound` when the real sound exists.
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
- **Settings:** the settings screen is UXML bound two-way to persistent Ripple variables (volumes, `QualityLevel`, `ResolutionIndex`, `Fullscreen`, `VSync`). `DisplaySettings` on `JamKitCore` applies the display ones; `QualityDropdown` and `ResolutionDropdown` fill their own choices. To add a setting, add a persistent variable and a bound row: mouse sensitivity is a slider bound to the variable `FirstPersonLook.MouseSensitivity` points at.

A pickup is a `TriggerZone` with `DestroySelf` and `OnEntered → Score.Add`. A kill zone is a trigger collider with a `Damager` set to `Kill`.

## Editor tooling

- **Main toolbar:** scene switcher, play-from-first-scene toggle, time scale slider, Doctor button showing the problem count, Publish button
- **`JamKit > New Jam Project`:** scaffolds the project from the template, as above
- **`JamKit > Doctor`:** runs by itself after a reload, on scene save, before Play and before a build, and logs what it finds. Project checks with fix buttons: every `[Required]` reference left empty in open scenes, prefabs and assets; build scenes without `JamKitCore`; Game Flow scenes missing from the build; sounds that skip the mixer; camera shake nobody listens to; a first-person rig whose mover fights its yaw
- **Inspectors:** every JamKit component and asset groups its Signals under **Events**, and its `[Button]` actions and live `[ShowInInspector]` values under **Debug**. The attributes live in `Metz.JamKit.Inspector` and work on your own scripts too: `[Required]` turns an empty reference red.
- **Auto-assign:** adding a JamKit component fills its service references, and its required `InputActionReference` fields by name, when there is exactly one candidate
- **`JamKit > New Level`** (also at the bottom of the toolbar's Scenes menu): a copy of `Scenes/Templates/Level.unity` with `JamKitCore`, the menu (Esc pauses), a camera, a light and an EventSystem, added to the build before GameOver.
- **Keep These Values After Play:** in Play mode, a component's ⋮ menu takes its current values and writes them back when Play ends.
- **`JamKit > Clear Saved Data`:** clears the saved value of every persistent Ripple variable, after listing them.
- **`JamKit > Publish to itch.io`** (the toolbar's Publish button): builds for the web, zips the build with `index.html` at the root, and pushes it with butler when butler is installed and your itch.io page is set in Project Settings > JamKit. It asks first while the Doctor reports errors. A WebGL Build Profile set there is built instead of Build Settings.
- **Starter picker:** `GameObject > JamKit > Starter…` or the toolbar's Starters button opens a searchable list of every prefab in `Assets/_Project/Prefabs/Starters`, your own variants included. Enter places the first match at the Scene view's pivot, or under the object you right-clicked.

## Known gaps

- The five samples still reference components and event data from before 1.0 and need re-authoring in the editor.
- The project template ships no WebGL Build Profile yet, so Publish builds from Build Settings unless you assign one.

## License

MIT. See [LICENSE.md](LICENSE.md).
