# Object-Oriented Review

A pass over JamKit through the lens of Christopher Okhravi's *The Object Oriented Way* (2025).
The book's through-line is narrow and useful: most OO machinery exists to **eliminate conditionals**
and to **move errors from run-time to compile-time**, and inheritance is almost always the wrong tool
for it. This document records what that lens changed, what it *validated* (so it isn't re-litigated),
and what it explicitly **rejected** (so that isn't either). Section numbers (§) refer to the book.

The book lands well here because JamKit had already made most of the calls it argues for — sealed
types, injection over instantiation, variation-as-data not variation-as-subclass. What it exposed was
sharper and smaller: a physics branch re-evaluated every frame when the answer was fixed at `Awake`,
and domain concepts modeled as raw strings that fail silently at run time.

---

## Changed

### 1. A motor strategy — resolve the body-type branch once (§11.6)

§11.6: conditionals at the *edge* of a system are necessary, but must not leak into the core. The
`Rigidbody2D` / `Rigidbody` / `Transform` triple-branch was copy-pasted across five components **and
re-evaluated inside `FixedUpdate`** — the body type is a fixed fact about the object, so the branch
belonged at the edge (resolution), not in the per-frame core.

- New `Runtime/Gameplay/Motor.cs`: `IMotor` (`MoveTo` / `Teleport` / `Halt`), three implementations,
  `Motor.Resolve(GameObject)` (rb2d > rb > transform), and an allocation-free `Motor.LaunchBody` for
  one-shot velocity on freshly spawned bodies.
- `PatrolMover` and `Respawner` cache an `IMotor` at `Awake` — the branch is gone from their update
  paths. `SpawnBurst` and `ProjectileShooter` route spawned-body velocity through `Motor.LaunchBody`.
- **Behavior note:** `ProjectileShooter` still uses its `Is2D` flag to pick the facing axis
  (`muzzle.right` vs `muzzle.forward`); the motor just applies it. A projectile whose body type
  contradicts `Is2D` now gets launched instead of silently ignored — an edge-case fix, not a regression.

### 2. `PatrolMover` end behavior — `[SerializeReference]` strategy, not an enum switch (§11.5)

The book's canonical "replace conditional with polymorphism." The four-case `switch (Mode)` became a
`PatrolMover.PathEndBehavior` abstract base with `PingPong` / `Loop` / `Stop` / `TeleportToStart`
nested classes, selected by a `[SerializeReference]` field. Odin renders the type picker, so it stays
a dropdown; a new end behavior is a new class, not another `case`. See the PILLAR 2 amendment.

- A null-guard (`Mode ??= new PingPong()` in `Awake`) means any PatrolMover whose reference didn't
  migrate degrades to the old default — which is exactly what `Mode: 0` meant.
- **This was the weakest change on cost/benefit** (four stable cases, `[SerializeReference]` is
  rename-fragile). It is here because the conflict decision was to follow the book where it collides
  with the "inspector is tool #1" pillar — and Odin's picker makes that collision disappear.

### 3. `SceneRef` — a picked scene, not a typed string (§10.1–10.3)

Five raw `string` scene names were textbook primitive obsession (§10.1): a `string` holds "Levl1" as
happily as "Level1" and only fails at run time when the load returns null. `Runtime/Scenes/SceneRef.cs`
is a `[Serializable] struct` (name + asset GUID) with an editor drawer
(`Editor/Drawers/SceneRefDrawer.cs`) that offers a Build Settings dropdown — the failure moves to edit
time (§10.3 bijection: the type's values now map to real scenes). It converts implicitly to the
scene-name string the loaders take, so `SceneServiceSO.LoadAsync(string)` is unchanged.

Replaced in `MenuController`, `GameOverController`, `TriggerZone`, `SurvivorLoop`; the sample prefabs
were migrated to the nested serialized shape.

### 4. Close the silent-failure paths (§10.5 fail fast)

§10.5 is against *error hiding*. JamKit fails fast at edit time (`[Required]`, Doctor) but `[Required]`
can't guard a `string`, so a typo'd element name was a permanently blank HUD with no diagnostic — and
`Apply()` retried `Resolve()` forever, silently. Now warn *once* (the `_warnedNotReady` pattern from
`FmodAudioServiceRunner.cs:257`), naming the fix:

- `UIBindingBase.WarnUnresolved` → used by `LabelBinding` / `BarBinding`.
- `SceneServiceSO` warns when a load is called with no runner registered (was a silent no-op).
- `MenuController.Bind` warns per missing button name (UXML/const drift).

### 5. `SaveServiceSO` — distinguish "no save" from "corrupt save" (§10.4.2 / §10.5)

The old `Read` caught bare `Exception` and returned `fallback`, so a corrupt save was indistinguishable
from a fresh start — the book (§10.4.2) permits redefining a spec to define an error out of existence,
but only when it stays congruent with the domain, and these are two different domain states.

- New `TryRead<T>(key, out value)` returns `false` for both, but logs the *cause* only when the file
  exists and is unreadable/corrupt (a missing save is not an error).
- Keys are validated against path traversal (`..`, separators) and refused loudly (§10.5) — a key
  becomes a file name, so an unchecked one could write outside the save folder.

### PILLARS.md

PILLAR 2 gained the strategy-object rule (behavior → `[SerializeReference]`; data → field/SO, §11.7).
PILLAR 3 gained the silent-no-op rule (an unresolvable inspector string must warn once).

---

## Validated — deliberately unchanged

Recording these matters as much as the changes: several are refactor-bait the book itself argues against.

- **Variation-as-data, not subtypes (§11.7).** `Health` is one sealed class parameterized by
  `Max`/`Current`, not a `WeakHealth`/`TankHealth` hierarchy — exactly `new Attack("Stardust", 20)`
  over `class Stardust : IAttack`. The kit's whole ScriptableObject-as-data model is this principle.
- **The service inheritance chain (§12.8, §12.8.3.3).** `ServiceSO` / `ServiceSO<TRunner>` /
  `ServiceRunner<TService,TRunner>` (`Runtime/Services/ServiceBase.cs`) is a legitimate Template
  Method over a genuinely hierarchical problem, and the CRTP constraint keeps `(TRunner)this` safe.
  40+ of the runtime types are `sealed`. The book asks for inheritance only when you want code reuse
  *and* subtype polymorphism, hierarchically — this qualifies; almost nothing else would.
- **Composition over inheritance is already the default (§17.1).** `Mover2D`/`Mover3D` are siblings,
  not a `MoverBase`; `Respawner` is deliberately `Health`-ignorant. The kit answers variation with
  more small components, which is the Duck-dilemma resolution.

## Rejected — with reasons

- **A shared `IAudioService` over `AudioServiceSO` + `FmodAudioServiceSO`.** They diverge in payload
  (`AudioClip` vs `EventReference`) *and* return type (`AudioSource` vs `void`). Forcing one interface
  would make implementations that can't honestly satisfy it — precisely the §22.6
  `ICollection<T>` / `ReadOnlyCollection<T>` warning ("in the spirit of the law, a violation"). The
  compile-define split (`JAMKIT_FMOD`) is a compile-time strategy selection and is correct as-is.
- **Abstraction+injection for the service references (§18.1 bottom-right quadrant).** The book prefers
  depending on an interface, injected. For services the concretion is already a swappable
  ScriptableObject asset, and an `IFoo` seam would buy nothing a designer can act on while costing the
  inspector legibility the kit optimizes for. Concretion+injection (top-right) is the right quadrant here.
- **`ChaseMover` through the motor abstraction.** Its 2D and 3D paths differ in *behavior*, not just
  body type — `LockY` velocity handling, `MoveRotation` vs `Slerp(LookRotation)` facing. That is the
  §11.7 case where a shared abstraction would leak, so the branch stays local. (This is why the motor
  refactor took four of the five sites, not five.)

---

## Closing note

The book's Epilogue argues OOP is a dead end — that "mixing data and behavior is fundamentally a bad
idea," and that pulling on OO abstractions leads back to functional programming's first-class
functions. JamKit's core structural choice is already a partial answer to that critique: data lives in
ScriptableObject assets, behavior lives in stateless services and small components that *process* that
data. The message is kept separate from the messenger. That framing — more than any single pattern —
is the most useful thing the book offers this kit: when in doubt, push state out to Ripple/SO data and
keep the code a thin machine over it.
