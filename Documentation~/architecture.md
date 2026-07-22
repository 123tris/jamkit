# JamKit Internals & Extension Guide

A technical tour of how JamKit is built and how to build on it. Written for a programmer who wants to
understand the internals quickly — what each piece does, *why* it's shaped that way, and what that
implies when you extend it. Pairs with [PILLARS.md](../PILLARS.md) (the rules) and
[object-oriented-review.md](object-oriented-review.md) (the design rationale for recent refactors).

- [1. Mental model](#1-mental-model)
- [2. The dependency model](#2-the-dependency-model-no-singletons-injected-references)
- [3. Services: the SO/Runner split](#3-services-the-soRunner-split)
- [4. State vs behavior, and persistence](#4-state-vs-behavior-and-persistence)
- [5. Gameplay primitives](#5-gameplay-primitives)
- [6. The events model](#6-the-events-model)
- [7. Time ownership](#7-time-ownership)
- [8. Pooling](#8-pooling)
- [9. The UI layer](#9-the-ui-layer)
- [10. Editor tooling](#10-editor-tooling)
- [11. Value types](#11-value-types)
- [12. Constraints & implications](#12-constraints--implications)
- [13. How to extend](#13-how-to-extend)
- [14. File map](#14-file-map)

---

## 1. Mental model

JamKit is the **glue layer** of a four-part Unity stack. It owns none of the hard problems; it wires
the tools that do into a jam-ready whole:

| Layer | Owns | JamKit depends on it for |
|---|---|---|
| **Ripple** | State + events: variables (opt-in persistence), typed events, listeners, runtime sets | every piece of shared state and every global event |
| **Feel** (optional) | Visual/physical feedback: MMF_Player stacks | juice — flashes, shakes, particles, floating text |
| **FMOD** (optional) | Audio: events, buses, music states | the FMOD audio backend |
| **Odin** | Editor UX: buttons, live state, validation, dropdowns | every inspector affordance |
| **JamKit** | *The glue*: behavior services, gameplay primitives, menus/scene flow, editor tooling | — |

The whole design falls out of one sentence: **data lives in ScriptableObject assets; systems are
small machines that process that data.** A jam game is assembled by dropping prefab instances into a
scene and wiring their inspector slots — ideally with no new code. Three assemblies carry it:
`Metz.JamKit.Runtime`, `Metz.JamKit.Editor`, and the FMOD-gated `Metz.JamKit.Fmod` (+ `.Editor`).

Four properties are load-bearing everywhere, so internalize them before reading on:

1. **No singletons, no `DontDestroyOnLoad`, no service locator.** References are serialized fields,
   filled by the editor. The runtime never looks anything up.
2. **Scenes are clean slates.** Everything that must persist lives in an SO asset that exists
   independently of any scene. No `if (scene.name == …)`, no smuggled state.
3. **Components do one thing.** Variation comes from *combining* small components, not from
   configuring big ones or subclassing.
4. **Loud at edit time, and — post OO-review — loud at run time too** where the editor can't reach.

---

## 2. The dependency model: no singletons, injected references

Every cross-system reference is a **serialized field** holding an SO asset (a service, a Ripple
variable/event) or, occasionally, a scene component. In *The Object Oriented Way*'s terms this is
**constructor injection done through the inspector** — the "concretion + injection" quadrant: the
compile-time type is concrete (`InputServiceSO`, not `IInput`), but the dependency is injected, not
instantiated or located.

```csharp
// Runtime/Gameplay/Mover2D.cs
[Required] public InputServiceSO InputService;   // filled in the inspector, used directly
```

**Why not a locator/singleton?** Because you can *see* the wiring. If a mover reads the wrong input
service you find it by looking at the slot, not by tracing static state. The cost — dragging references
onto components — is paid entirely at edit time by tooling:

- **`JamKitAutoAssign`** ([Editor/AutoAssign/JamKitAutoAssign.cs](../Editor/AutoAssign/JamKitAutoAssign.cs))
  hooks `ObjectFactory.componentWasAdded` and `ObjectChangeEvents.changesPublished`. When a JamKit
  component appears, it reflects over the component's **public fields whose type lives in
  `Metz.JamKit`** (`CandidateFields`), and fills each null one **only when exactly one candidate
  exists** in the project/scene (`FindCandidate`). Zero or many → left null (never a guess). It writes
  ordinary serialized references you can still see and change.
- Ripple event/variable fields are **deliberately excluded** — which event an effect listens to is a
  design choice, not plumbing.
- `[Required]` (Odin) marks a still-null load-bearing ref red and surfaces it in Odin Validator; the
  **Doctor** window (`JamKit > Doctor`) catches project-shape problems with Fix buttons. Three nets,
  none load-bearing on its own.

**Implication when you extend:** if you add a component with a field typed as a JamKit service, it
auto-fills for free. If you add a *new service type*, auto-assign covers it automatically (it's
type-driven). If you need a reference to something outside `Metz.JamKit`, you wire it by hand — by
design.

---

## 3. Services: the SO/Runner split

A **service** exists only where behavior must wrap engine/native machinery — scene loading, the
timescale, input maps, pooling, file IO, FMOD. If a thing is *just data* (score, volume, HP, a timer
readout) it is a Ripple variable, **not** a service. That line is enforced socially by PILLARS and
structurally by the base classes in [Runtime/Services/ServiceBase.cs](../Runtime/Services/ServiceBase.cs).

### The two halves

A service is split into a persistent **asset** and a disposable per-scene **runner**:

- **`ServiceSO`** — the asset. Persists across scenes (it's a project asset, not a scene object).
- **`ServiceRunner`** — a `MonoBehaviour` that lives in a scene (on `JamKitCore`) and registers itself
  with the asset on enable. It provides what an SO can't: coroutines, `AudioSource`s, a parent transform.

```csharp
public abstract class ServiceSO<TRunner> : ServiceSO where TRunner : class
{
    protected TRunner Runner { get; private set; }
    public bool HasRunner => Runner != null;              // live in the Debug foldout

    public void RegisterRunner(TRunner runner) { Runner = runner; OnRunnerRegistered(); }
    public void UnregisterRunner(TRunner runner) {
        if (!ReferenceEquals(Runner, runner)) return;      // ignore a stale runner unregistering
        Runner = null; OnRunnerUnregistered();
    }
    protected virtual void OnRunnerRegistered() => ResetState();  // see below
    protected virtual void OnRunnerUnregistered() { }
}
```

Every public service method is a **null-safe forward** to the runner — a hand-rolled Null Object:

```csharp
// AudioServiceSO
public void PlayMusic(AudioClip clip, float fade = 0.5f, float vol = 1f) => Runner?.PlayMusicImpl(clip, fade, vol);
```

With no runner registered, every routed call is a silent no-op — which is *correct* for tests, editor
preview, and menu-only scenes. (Post OO-review, `SceneServiceSO` is the exception that **warns once**
when a load is attempted with no runner, because "the button does nothing" is a bug, not a valid state.)

### The CRTP runner base

```csharp
public abstract class ServiceRunner<TService, TRunner> : MonoBehaviour
    where TService : ServiceSO<TRunner>
    where TRunner  : ServiceRunner<TService, TRunner>   // F-bounded: makes (TRunner)this safe
{
    [Required] public TService Service;
    protected bool IsRegistered { get; private set; }

    protected virtual void OnEnable() {
        if (Service == null) { Debug.LogWarning($"[JamKit] {name}: no Service assigned.", this); return; }
        Service.RegisterRunner((TRunner)this); IsRegistered = true;
    }
    protected virtual void OnDisable() { if (Service != null) Service.UnregisterRunner((TRunner)this); IsRegistered = false; }
}
```

The mutual `where` constraints (the Curiously Recurring Template Pattern) are what let the base cast
`this` to `TRunner` without a runtime check. `PoolServiceRunner` is a *completely empty* class — it
exists only to be a transform and inherit registration.

### The `ResetState` lifecycle contract

This is the subtlest and most important part. **With Domain Reload disabled** (the jam default, for
fast Play-mode entry), an SO's fields survive between Play sessions — a leaked timescale push or a
stale pool would carry over. Two hooks solve it:

- `ServiceSO.OnEnable` subscribes to `playModeStateChanged`; on `ExitingEditMode` (entering Play) it
  calls `ResetState()`. Builds get a fresh process, so no hook is needed there.
- `ServiceSO<TRunner>.OnRunnerRegistered` **also** defaults to `ResetState()` — so every scene load
  that contains a runner re-clears the service. `FmodAudioServiceSO` overrides this to an empty body,
  the one opt-out, so its music instance survives scene transitions.

Override `ResetState()` to drop your service's play-session state. `TimeServiceSO.ResetState` clears
the timescale stack; `PoolServiceSO.ResetState` forgets its pools; `InputServiceSO.ResetState` drops
cached maps. If you write a stateful service and *don't* override it, stale state leaks across sessions.

### The concrete services

| Service | Base | Runner? | Wraps | Key API |
|---|---|---|---|---|
| `AudioServiceSO` | `ServiceSO<AudioServiceRunner>` | ✓ | Unity AudioMixer + sources | `PlaySfx`, `PlayMusic`, `DuckMusic`, `PlayStinger` |
| `FmodAudioServiceSO` | `ServiceSO<FmodAudioServiceRunner>` | ✓ | FMOD (own assembly, `JAMKIT_FMOD`) | same shape, `EventReference` payloads |
| `TimeServiceSO` | `ServiceSO<TimeServiceRunner>` | ✓ | `Time.timeScale` (push/pop stack) | `Push`/`Pop`, `Pause`/`Resume`, `FreezeForSeconds` |
| `SceneServiceSO` | `ServiceSO<SceneServiceRunner>` | ✓ | async scene load + fade | `LoadAsync`, `ReloadCurrent` |
| `PoolServiceSO` | `ServiceSO<PoolServiceRunner>` | ✓ | `GameObjectPool` per prefab | `Spawn`, `Despawn`, `Prewarm` |
| `InputServiceSO` | `ServiceSO` | — | `InputActionAsset` + maps | `Move`/`Jump`/…, `SwitchToUI`/`SwitchToGameplay` |
| `SaveServiceSO` | `ServiceSO` | — | JSON file IO | `Write`, `Read`/`TryRead`, `Has`, `Delete` |

The two runner-less services (Input, Save) skip the generic middle layer — they wrap only asset data
and direct file IO, which an SO can do alone.

---

## 4. State vs behavior, and persistence

The dividing line: **behavior → service; state → Ripple variable.** Score, volume, HP, high score,
and the round-clock readout are all `FloatVariableSO`s, not services. (`ScoreServiceSO`/`TimerServiceSO`
used to exist and were deleted — the CHANGELOG 0.9.0 "headline cuts" records why.)

Persistence has **exactly one mechanism**: Ripple **persistent variables** (a variable marked Persist
survives sessions — volumes, high score). There is no settings-save code in JamKit; the audio runners
have no PlayerPrefs blocks. `SaveServiceSO` is a *different* concern — game-shaped JSON blobs
(`Write<T>("slot1", saveBlob)`), not settings. Keep them separate: a volume slider writes a persistent
Ripple variable; a save game writes `SaveServiceSO`.

One Ripple subtlety to know: a variable resets `current → initial` each Play session by design. Tune
the *initial* value in the asset (edits persist in Play mode), or mark the variable persistent.

---

## 5. Gameplay primitives

Primitives live in [Runtime/Gameplay/](../Runtime/Gameplay). Every one is `sealed` and does exactly
one thing; a game is a *combination* of them plus inspector wiring. A component earns a place in the kit
only by appearing in **3+ genre columns** of the ROADMAP archetype matrix; 1–2 columns and it lives in a
sample as a copyable local script (`Bouncer2D`, `GridMover`, `WaveSpawner`, …).

### Health — the hub

[Health.cs](../Runtime/Gameplay/Health.cs) is a bare HP tracker and the object everything hangs off. It
exposes **two broadcast layers** (see §6): per-instance UltEvents (`OnDamaged`/`OnHealed`/`OnDied`) for
*this object's* reactions, and optional global Ripple assets (`CurrentVariable`, `BroadcastDamaged`,
`BroadcastDied`) for HUDs and any-enemy counters. `Damage()` runs the whole chain — clamp, push to the
Ripple variable, fire the instance event, fire the broadcast, then despawn/destroy on death. It's
`IPoolable`: `OnSpawn → ResetFull()`, so a pooled enemy comes back alive.

### Composition, not configuration or subtyping

The kit answers "make it different" with *another small component*, never a subclass:

- `Mover2D` / `Mover3D` are **siblings**, not a `MoverBase`.
- `Respawner` is a pure teleporter with **no Health knowledge** — you wire `Health.OnDied →
  Respawner.RespawnAfterDelay()` and `Respawner.OnRespawned → Health.ResetFull()` in the inspector.
- `Interactor` / `Interactable` are a **pair**, not a hierarchy.

This is the book's §11.7 ("don't use subtypes for data variation") applied wholesale: variation is
captured as *objects and serialized fields*, not classes.

### Two axes of variation: flags vs strategies

Where a single component *does* need modes, JamKit uses one of two shapes:

- **Boolean/enum flags** for cheap, orthogonal toggles: `TriggerZone` is the flag maximum — kill pit,
  goal, score gate, and level exit are one component with six optional behaviors gated by flags
  (`Kill`, `Damage`, `ScoreVariable`, `RemoveEnterer`, `LoadScene`, `OneShot`). `Mover2D.TopDown`,
  `ProjectileShooter.Is2D`/`UseAttackInput`, and the `Unscaled` toggles are the same idea.
- **`[SerializeReference]` strategy objects** for genuinely *behavioral* variation that would otherwise
  be a growing `switch`. `PatrolMover.Mode` is the reference example (post OO-review): a
  `PathEndBehavior` abstract base with `PingPong`/`Loop`/`Stop`/`TeleportToStart` nested classes,
  picked from an Odin type dropdown. A new end behavior is a new class, not a new `case`. See §13 for
  the recipe and PILLAR 2 for the rule (behavior → strategy; data → field).

### Motor — the body-type abstraction

[Motor.cs](../Runtime/Gameplay/Motor.cs) is the single home of the "is this a `Rigidbody2D`, a
`Rigidbody`, or a plain `Transform`?" branch that used to be copy-pasted across five components and
*re-evaluated every `FixedUpdate`*. The body type is a fixed fact about the object, so the conditional
belongs at the edge (resolution), not in the per-frame core:

```csharp
internal interface IMotor { void MoveTo(Vector3 p); void Teleport(Vector3 p); void Halt(); }

internal static class Motor {
    public static IMotor Resolve(GameObject go);            // rb2d ?? rb ?? transform, called from Awake
    public static void   LaunchBody(GameObject go, Vector3 velocity);  // alloc-free one-shot for spawned bodies
}
```

`PatrolMover` and `Respawner` cache an `IMotor` at `Awake`; `SpawnBurst` and `ProjectileShooter` use
`Motor.LaunchBody` on freshly spawned projectiles. `ChaseMover` deliberately keeps its own branch — its
2D and 3D paths differ in *behavior* (LockY handling, `MoveRotation` vs `Slerp(LookRotation)`), not just
body type, so folding it through `IMotor` would make the interface leak. (This is the §11.7 line again,
applied to the abstraction itself.)

### The pool-or-instantiate idiom

Spawners share one idiom: `PoolService != null ? PoolService.Spawn(...) : Instantiate(...)`, and the
mirror for despawn. The pool is always optional — a component works without one, it just doesn't pool.
See §8.

---

## 6. The events model

Four mechanisms coexist. The two that matter for wiring have a strict naming rule so global-vs-instance
is unmissable:

| Kind | Naming | Scope | Defined by | Use for |
|---|---|---|---|---|
| **Ripple SO event** | `Broadcast*` | Global — "anyone may care" | Ripple (`VoidEventSO`, `FloatEvent`, …) | HUD binds, kill counters, global shake |
| **UltEvent** | `On*` | Per-instance — "this exact object reacts" | UltEvents | feedback players, respawns, death debris |
| **C# `event`** | — | Code-only extension seam | JamKit (`InputServiceSO.MapSwitched`, `Interactor.TargetChanged`) | game code that needs a callback |
| **`Action` param** | — | Local strategy/callback | — | `MenuController.Bind`, editor closures |

**The rule** (also in ROADMAP): a feature that wires *per-instance* feedback through a *shared* SO event
is a bug — every enemy would flash when one is hit. Instance reactions go on UltEvents; global reactions
subscribe to Ripple broadcasts. Feedback components never know *why* they played.

**Subscription discipline.** Ripple subscriptions live on persistent SO assets, so they *outlive the
scene* and must be torn down explicitly. The uniform protocol is null-checked `AddListener` in
`OnEnable`, `RemoveListener` in `OnDisable`. Where subscriptions are dynamic (`Toast`,
`MenuSettingsBinder`) the component keeps an explicit teardown list. UI-element callbacks, by contrast,
die with the visual tree and need no teardown — `MenuSettingsBinder`'s comment documents that asymmetry.

Raise sites null-check the *asset*, never the delegate: `if (BroadcastDied != null) BroadcastDied.Invoke();`.

---

## 7. Time ownership

`Time.timeScale` has **exactly one owner**: [TimeServiceSO](../Runtime/Services/TimeServiceSO.cs). It's a
push/pop **stack**, so pause + hit-stop + slow-mo *compose* instead of stomping each other:

```csharp
public void Push(float scale) { _stack.Push(scale); Apply(); }   // Apply → Time.timeScale = _stack.Peek()
public void Pop()  { if (_stack.Count > 0) _stack.Pop(); Apply(); }
public void Pause()  => Push(0f);
public void Resume() => Pop();
public Coroutine FreezeForSeconds(float s, float scale = 0f) => Runner?.StartFreeze(s, scale); // needs runner
```

`ResetState` clears the stack each session, and `OnDisable` forces `Time.timeScale = 1f` — the one
invariant the kit treats as too dangerous to fail silently (a leaked freeze would soft-lock the game).
`MenuController` likewise resumes on disable. Because of single ownership, **Feel's time feedbacks are
off-limits** — hit-stop routes through `HitStop`/`MMF_JamKitHitStop`, which push onto this stack.

---

## 8. Pooling

[GameObjectPool.cs](../Runtime/Pool/GameObjectPool.cs) is a plain (non-generic) pool: one instance per
prefab, an idle `Stack<GameObject>`, `maxIdle` overflow → destroy. `PoolServiceSO` holds a
`Dictionary<GameObject, GameObjectPool>` and a reverse map so `Despawn(instance)` finds its pool.
`PoolServiceRunner` supplies the parent transform idle instances live under (without a runner, spawns
still work but parent to the active scene root).

**`IPoolable`** is the kit's only interface — a two-method callback contract (`OnSpawn`/`OnDespawn`).
The pool dispatches it via an alloc-free component scan into a shared static scratch list:

```csharp
static readonly List<IPoolable> _scratch = new();
static void InvokeSpawn(GameObject go) { go.GetComponents(_scratch); for (…) _scratch[i].OnSpawn(); _scratch.Clear(); }
```

`Health`, `AutoDespawn`, and `PatrolMover` implement it (reset HP, restart the despawn timer, restart the
path). **Caveat:** the static scratch buffer is not reentrancy-safe — an `OnSpawn()` that itself spawns
another pooled object clears the outer loop's list mid-iteration. Don't spawn from a spawn callback.

---

## 9. The UI layer

Built on **UI Toolkit** (UXML/USS), not uGUI. The menu markup is a *project-owned template* — the wizard
copies `JamKitMenu.uxml/.uss` into `Assets/_Project/UI/Resources/` so designers edit a real file, not a
read-only package asset.

### UIBindingBase — a Template Method

[UIBindingBase.cs](../Runtime/UI/UIBindingBase.cs) solves one awkward problem — the `UIDocument` may
build its visual tree *after* this component's `OnEnable` — and delegates the rest to four abstract steps:

```csharp
protected virtual void OnEnable() { if (Document == null) Document = GetComponent<UIDocument>(); Subscribe(); StartCoroutine(BindWhenReady()); }
IEnumerator BindWhenReady() { while (Root == null) yield return null; Resolve(); Apply(); }   // defer until the tree exists

protected abstract void Subscribe();    // hook the data source; each change calls Apply()
protected abstract void Unsubscribe();
protected abstract void Resolve();      // find the element(s) in Root
protected abstract void Apply();        // push current value(s) to the element(s)
```

`LabelBinding` and `BarBinding` are the two implementers — bind a HUD number or a fill bar to a Ripple
`FloatVariableSO` with zero code. Post OO-review, both call `WarnUnresolved(name)` when the tree is built
but the named element isn't found — a typo in the element name is otherwise a permanently blank HUD with
no diagnostic (and `[Required]` can't guard a string).

### MenuController — a State machine over a stack

[MenuController.cs](../Runtime/UI/MenuController.cs) drives Start/Settings/Pause views in one `UIDocument`
via a `Stack<View>` (`Push`/`Pop`/`ResetStack`/`Apply`). It centralizes element names in a nested
`const` class `N` so a typo is a compile error; `Bind` warns once per missing button. `MenuSettingsBinder`
handles the Settings view's sliders and is the package's one `Dispose()` lifetime object (its SO
subscriptions outlive the scene). Scene navigation uses `SceneRef` fields (§11).

Other UI: `PauseController` (routes Esc to `MenuController.HandleBack`), `GameOverController` (code-built
screen using the menu USS), `Toast` (transient messages driven by Ripple events), `DebugPanel` (ships in
every scaffolded scene — Backquote — because WebGL builds have no inspector), `JamKitUI` (theme/panel
settings loader), `FadeOverlay` (scene-load fades).

---

## 10. Editor tooling

The ergonomics live here so the runtime stays tiny and explicit:

- **`JamKitAutoAssign`** — reference auto-fill (§2).
- **`JamProjectWizard`** (`JamKit > New Jam Project`) — scaffolds the project: services, `JamKitCore`
  prefab, bootstrap/game/game-over scenes, Build Settings, the menu template copy, and the **starter
  prefab library**. Re-runnable; migrates existing projects.
- **`StarterPrefabLibrary`** — a **Strategy pattern via delegate table**: a `static readonly Starter[]`
  where each entry pairs a name with a `Func<Context, GameObject>` closure that composes a pre-wired
  archetype. `Context` is a parameter object bundling the four common service deps.
- **`JamKitDoctorWindow`** (`JamKit > Doctor`) — project-shape checks with Fix buttons (mixer params,
  Build Settings, EventSystem, HitStop↔TimeRunner pairing). Per-field null checks are `[Required]`'s job.
- **`SceneRefDrawer`** ([Editor/Drawers/](../Editor/Drawers)) — the Build Settings dropdown for `SceneRef`
  (§11). The template for any future typed-reference drawer.
- **FMOD editor** (`FmodJamKitSetup`, `FmodDefineSync`) — define-sync and setup when FMOD is present.
- **`WebGLItchBuild`** — one-click itch.io-shaped WebGL build.

---

## 11. Value types

Small `[Serializable]` structs and helpers that make impossible states harder to express:

- **`SceneRef`** ([Runtime/Scenes/SceneRef.cs](../Runtime/Scenes/SceneRef.cs)) — a scene reference picked
  from Build Settings instead of a raw string (§13 has the "make your own" recipe). Wraps a name + asset
  GUID; converts implicitly to the scene-name string the loaders take. Turns a misspelled scene from a
  run-time null into an edit-time non-choice.
- **`Timer` / `Cooldown` / `UnscaledCooldown`** ([Runtime/Time/](../Runtime/Time)) — tiny time-tracking
  structs. `Cooldown.TryUse()` returns true at most once per duration. (`UnscaledCooldown` is a
  copy-paste of `Cooldown` on `unscaledTime` — a spot where a shared abstraction could later collapse the
  two, tracked in the review doc.)
- **`RandomBag<T>`** ([Runtime/Utils/RandomBag.cs](../Runtime/Utils)) — a shuffle-bag (draw without
  repeats until empty).

---

## 12. Constraints & implications

The four pillars judge every change; internalize the ones that bite when extending:

- **Single-owner rules.** `Time.timeScale` → `TimeServiceSO` only. Persistence → Ripple persistent
  variables only (`SaveServiceSO` is for game saves, not settings). Feedback triggering → UltEvents
  (instance) / Ripple broadcasts (global) only.
- **Domain Reload is off.** Any play-session state on an SO **must** be cleared in `ResetState()`, or it
  leaks into the next session. This is the single most common way to introduce a subtle bug here.
- **Reentrancy.** The pool's `IPoolable` dispatch uses a shared static scratch list — don't spawn from a
  spawn callback (§8).
- **Non-goals (the identity fence).** No tween library, no node graphs, no netcode, no ECS, no Feel
  clone. JamKit is the wiring and the 80% defaults; depth lives in the dedicated assets. A new primitive
  is measured by how many archetype rows it unlocks, not how complete it is.
- **Loud failure.** `[Required]` for object refs; **warn once** for inspector-authored strings the editor
  can't validate (element names, scene names, save keys). A silent no-op is a bug (PILLAR 3).

---

## 13. How to extend

Recipes, ordered from most common to most ambitious. Each follows an existing pattern in the kit.

### Add a gameplay primitive

1. One `sealed` component, one responsibility, in `Runtime/Gameplay/`. Cache an `IMotor` in `Awake` if it
   moves a body; take an optional `PoolServiceSO` if it spawns.
2. Expose reactions as `On*` UltEvents (instance) and/or `Broadcast*` Ripple events (global) — never mix
   the scopes.
3. `[Required]` the load-bearing refs; add a `Debug` foldout with `[ShowInInspector]` live state and
   `[Button]` debug actions that exercise the *real* wiring.
4. **Decide kit vs sample by the 3-column rule:** if it appears in 3+ genre columns of the ROADMAP
   matrix, it's kit; 1–2 columns, it ships as a copyable script in a `Samples~/` folder.
5. Add a `.cs.meta` (see §14 / the memory note: two lines, fresh GUID) and one README sentence.

### Add a service

Only if it must wrap engine/native machinery (else it's a Ripple variable). Recipe:

```csharp
// Asset
[CreateAssetMenu(menuName = "JamKit/Services/My Service", fileName = "MyService")]
public sealed class MyServiceSO : ServiceSO<MyServiceRunner> {
    public void DoThing() => Runner?.DoThingImpl();          // null-safe forward
    public override void ResetState() { /* clear play-session state */ }  // REQUIRED if stateful
}
// Runner (scene-side, lives on JamKitCore)
public sealed class MyServiceRunner : ServiceRunner<MyServiceSO, MyServiceRunner> {
    internal void DoThingImpl() { /* coroutines, native handles, … */ }
}
```

Auto-assign covers the new type for free (it's type-driven). If the service is runner-less (pure asset
data + direct IO, like Input/Save), derive from `ServiceSO` directly and skip the generic layer.

### Add a strategy behavior (replace a growing switch)

When a component grows an enum + `switch` that keeps gaining cases *and the cases are behaviorally
different*, lift it to a `[SerializeReference]` strategy — the `PatrolMover.PathEndBehavior` pattern:

```csharp
[Serializable] public abstract class FooBehavior { public abstract void Apply(MyComp c); }
[Serializable] public sealed class BarFoo : FooBehavior { public override void Apply(MyComp c) { … } }
// on the component:
[SerializeReference] public FooBehavior Mode = new BarFoo();   // Odin renders the type picker
void Awake() => Mode ??= new BarFoo();                          // guard legacy/empty references
```

Nest the strategy classes inside the component (they can then touch its private state, and names like
`Stop`/`Loop` don't pollute the namespace). Guard `null` in `Awake` so a failed migration degrades to a
sensible default. **Only for behavior** — data variation (a speed, a color) stays a field or an SO (§12).
Note `[SerializeReference]` is rename-fragile (use `[MovedFrom]`) and has rough edges in nested prefabs;
weigh it against a plain enum for a small, stable set of cases.

### Add a typed reference (the SceneRef pattern)

`SceneRef` is a template for killing any string-keyed lookup — a `PrefabRef`, `TagRef`, `LayerRef`, etc.:

1. A `[Serializable] struct` wrapping the primitive (+ a GUID/id for stability), with a `Name`/`Value`
   accessor, a `HasValue`, and an implicit conversion to the primitive so existing APIs still take it.
2. A `[CustomPropertyDrawer]` in `Editor/Drawers/` offering a dropdown of valid choices (copy
   `SceneRefDrawer`). Prefer a stable id (GUID) and heal the display name against it.
3. Keep the primitive-accepting overload on the consumer so raw values still compile.

This converts a run-time "not found" into an edit-time non-choice — the §10 primitive-obsession fix.

### Add a UI binding

Subclass `UIBindingBase` and implement `Subscribe`/`Unsubscribe`/`Resolve`/`Apply`. Call
`WarnUnresolved(name)` in `Resolve` when the tree is ready but the element is missing. (Both current
bindings hardwire `FloatVariableSO`; a `UIBindingBase<TVariable>` is blocked by Unity's serializer not
supporting open-generic MonoBehaviours — a per-type subclass is the current answer.)

### Add an input action

Add the action to the `InputActionAsset`, then expose it on `InputServiceSO` following the cached-property
pattern (add a field, resolve it in `EnsureCache`, expose a property that calls `EnsureCache`). The cache
avoids a per-frame dictionary lookup. For local co-op, set `AutoEnableGameplay` on secondary player
services with distinct map names (`Gameplay1`/`Gameplay2`).

### Extend persistence

For richer saves, add to `SaveServiceSO`: it's generic (`Write<T>`/`TryRead<T>`) over `JsonUtility`. Use
`TryRead` to distinguish "no save" from "corrupt". Remember `JsonUtility`'s limits (no dictionaries, no
polymorphism, no bare-primitive roots — hence the internal `Wrapper<T>`). For *settings*, don't touch
`SaveServiceSO` — mark a Ripple variable persistent instead.

### Larger, opinionated extensions (tracked in the review doc)

- **Grow `IMotor`** with `SetVelocity`/rotation and fold `ChaseMover` in — worth it only if its 2D/3D
  facing logic can be expressed without leaking.
- **A null-object `PoolServiceSO`** or `ISpawnStrategy` would erase the `pool != null ? … : …` idiom from
  ~8 sites.
- **`IAudioService`** over the two audio backends — *rejected* today because their payload and return
  types diverge (forcing one interface would make implementations that can't honestly satisfy it, the
  §22.6 `ICollection`/`ReadOnlyCollection` trap). Revisit only if the two APIs converge.

---

## 14. File map

```
Runtime/
  Services/    ServiceBase.cs (SO/Runner/CRTP bases) · Audio/Time/Scene/Pool/Input/Save ServiceSO + Runners
  Gameplay/    Health, Motor, Mover2D/3D, ChaseMover, PatrolMover, ProjectileShooter, SpawnBurst, Spawner,
               Damager, Respawner, Pickup, Interactor/Interactable, TriggerZone, GameTimer, HighScoreTracker,
               AutoDespawn, RuntimeSetMember, HitStop
  UI/          UIBindingBase, LabelBinding, BarBinding, MenuController, MenuSettingsBinder, PauseController,
               GameOverController, Toast, DebugPanel, JamKitUI
  Pool/        GameObjectPool, IPoolable
  Scenes/      SceneRef, FadeOverlay
  Time/        Timer, Cooldown (+ UnscaledCooldown)
  Audio/       SfxOnEvent
  Fmod/        FmodAudioServiceSO + Runner, FmodSfxOnEvent, FmodMenuSounds  (JAMKIT_FMOD assembly)
  Utils/       RandomBag
  AssemblyInfo.cs  (InternalsVisibleTo the test assembly)

Editor/
  AutoAssign/  JamKitAutoAssign          Wizards/  JamProjectWizard
  Scaffold/    StarterPrefabLibrary, TemplateAssets    Windows/  JamKitDoctorWindow
  Drawers/     SceneRefDrawer            Fmod/     FmodJamKitSetup     Build/  WebGLItchBuild

Tests/Runtime  &  Tests/Editor          Samples~/ (00 Hour Zero · 01 Platformer · 02 Survivor · 03 Feel · 04 Arcade)
```

**Building on the code:** this is a standalone package repo — there is no Unity project inside it, so C#
can't be compiled or tested here. Add the package to a host Unity project and run
`Tools~/compile-check.sh` from that project's root, plus the Unity Test Runner (Edit + Play mode). New
`.cs` files need a hand-written two-line `.meta` (`fileFormatVersion: 2` + a fresh 32-hex `guid:`).
