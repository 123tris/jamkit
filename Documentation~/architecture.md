# JamKit architecture

## Layers

```
Ripple    variables, events, listeners, runtime sets, Signal
JamKit    services, audio, juice, gameplay, menus, editor tooling
```

JamKit depends on Ripple and on nothing from the Asset Store. Feel and FMOD are optional.

## Inspectors

`JamKitEditor` (UI Toolkit) draws every JamKit component and asset, registered by a one-line subclass per type. Fields keep their order; `Signal` fields collect under an Events foldout; methods marked `[Button]` and members marked `[ShowInInspector]` collect under a Debug foldout, live in Play mode. `[ShowIf]` and `[HideIf]` hide a field by a bool beside it. `[Required]` and `[UxmlName]` are property drawers, so they also work in other inspectors. The attributes are in `Metz.JamKit.Inspector`, out of the way of game code that also uses Odin or NaughtyAttributes.

`RequiredReferences` finds empty `[Required]` fields, following lists and nested serializable types and skipping fields their condition hides. The Doctor runs it over open scenes, prefabs and assets.

## Events

A component exposes `Signal` fields. A Signal holds a `UnityEvent` (inspector-wired calls), an array of Ripple event assets to raise, and C# listeners.

- This object reacting to itself: wire the `UnityEvent`.
- Anything elsewhere reacting: add a Ripple event to the raise list and listen with an `EventListener`.
- Code: `signal.AddListener(handler)` or `signal += handler`.

`UnityEvent` only binds public methods with zero or one parameter. Methods meant for wiring are therefore `void` and take at most one argument; awaitable variants are named `...Async`.

## Services

A service is a `ServiceSO` asset. Its only base behaviour is `ResetState`, called when the editor enters play mode, which keeps services correct with domain reload disabled.

Work that spans frames runs as a coroutine on `JamKitHost`, one persistent object created the first time a service needs it. Coroutines there survive scene loads and stop when play mode ends. `JamKitHost.Run` returns an `Awaitable`, so callers can `await` or `yield return` it. The host also carries the fade overlay and the audio sources.

| Service | State it owns |
| --- | --- |
| `TimeServiceSO` | `Time.timeScale`, as the minimum of the active `TimeModifier`s times `BaseScale` |
| `InputServiceSO` | Which action map is enabled, and the cursor |
| `SceneServiceSO` | The load in progress |
| `ScreenFadeSO` | The overlay alpha |

## Scene flow

`GameFlow` is a list of transitions: an optional source scene, a Ripple `VoidEventSO`, and a target scene (empty reloads). `GameFlowDriver` subscribes to the transitions that apply in its scene and calls `SceneServiceSO.Load`. Menus and gameplay raise the events; neither knows scene names.

On Unity 6.4+ a `.gameflow` graph asset (Graph Toolkit) imports to the same `GameFlow` object: Scene nodes, an Any Scene node, and Transition nodes holding the event.

## Project template

`Template~/` holds the project template: the scenes, prefabs, assets and UI documents a new project starts from, each with its `.meta` file. `JamKit > New Jam Project` scaffolds a project by copying every template file the project does not have into `Assets/_Project`, then registering the three scenes in the build. It never overwrites a file.

Because the `.meta` files travel with the assets, an asset has the same GUID in every project and references inside the template survive the copy. Unity ignores `Template~/`, so the template is edited in a scaffolded project and written back with `Tools~/export-template.sh`. `docs/adr/0001` records why.

`ProjectTemplate` scaffolds and names the pieces other tooling needs: the three scenes, `JamKitCore`, the starters folder and the scripts folder. The Doctor's checks live in `JamKitDoctor` and run without its window.

## Audio

`Sound` and `Music` are abstract ScriptableObjects. `ClipSound` / `ClipMusic` play through `AudioPlayer` on the host. `FmodSound` / `FmodMusic` live in the `Metz.JamKit.Fmod` assembly, compiled only when the `JAMKIT_FMOD` define is set by `FmodDefineSync`.

## Input

Components hold `InputActionReference` fields and enable their actions on enable. `InputServiceSO` enables or disables whole maps, so UI mode silences gameplay actions.

## Bodies

`Body` is an internal struct that resolves a `Rigidbody2D`, a `Rigidbody` or a bare `Transform` once and offers `MoveTo`, `RotateTo`, `Teleport`, `Halt` and `Velocity`. Movers, `Respawner`, `SpawnBurst` and `ProjectileShooter` use it instead of branching on body type.

## Assemblies

| Assembly | Notes |
| --- | --- |
| `Metz.JamKit.Runtime` | |
| `Metz.JamKit.Fmod` | Requires `JAMKIT_FMOD` |
| `Metz.JamKit.Editor` | Project template, wizard, Doctor, auto-assign, drawers |
| `Metz.JamKit.Toolbar.Editor` | Main toolbar elements (Unity 6.3+ API) |
| `Metz.JamKit.Flow.Editor` | Game Flow graph and importer (Unity 6.4+ API) |
| `Metz.JamKit.Tests`, `Metz.JamKit.EditorTests` | |

## Verifying changes

`Tools~/compile-check.sh` compiles Ripple from source and then every JamKit assembly with Roslyn against the host project's reference assemblies. It does not run tests; use the Test Runner for that.

After editing the project template in a scaffolded project, save in Unity and run `Tools~/export-template.sh` to mirror `Assets/_Project` back into `Template~/`.
