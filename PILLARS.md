# JamKit Engineering Pillars

Every feature, refactor, and cut in this kit is judged against four pillars. If a change
strengthens one pillar without wounding another, do it. If it serves none, it doesn't ship.

---

## 1. MODULAR — systems are Lego bricks, not a machine

- **Systems never hard-reference other systems.** An inventory shouldn't know a quest log
  exists. Communication happens through Ripple ScriptableObject events/variables (global,
  "anyone may care") or serialized UltEvents (per-instance, "this exact object reacts") —
  and both are *visible in the inspector*. Hidden `GetComponentInParent` magic is banned:
  if you can't see the wiring, you can't rewire it.
- **Scenes are clean slates.** No `DontDestroyOnLoad`. No transient state smuggled between
  scenes. No `if (scene.name == ...)`. Every scene loads cold and works, because anything
  that must persist lives in ScriptableObject assets (services, Ripple variables) that
  exist independently of any scene.
- **Prefabs work on their own.** A prefab dragged into an empty scene should function (or
  fail loudly via `[Required]`). Scenes are *lists of prefab instances* plus a camera —
  which is also how a team avoids scene merge conflicts: check-ins happen at prefab level.
- **Components do exactly one thing.** A component with one proven outcome can be
  recombined into mechanics nobody planned. That recombination — emergent design — is the
  payoff of the whole pillar.

## 2. EDITABLE — the inspector is tool #1

- **Data lives in ScriptableObjects; systems are machines that process that data.**
  Changing the game should mean changing data, not code.
- **Designers compose without programmers.** Every trigger, tuning value, and reaction is
  an inspector slot. If a behavior can only be reached from C#, it isn't done.
- **Runtime editable.** Balance while playing. ScriptableObject edits made in play mode
  *persist* — lean on that: tune the asset, not the scene object. (Ripple variables reset
  `current → initial` each play session by design; tune the initial value, or mark the
  variable persistent.)
- **Options over assumptions.** Prefer a serialized field with a good default over a
  constant, and a `FloatReference` (constant-or-variable) over a bare float where sharing
  is plausible.

## 3. DEBUGGABLE — a feature isn't done until you can watch it work

- **Every stateful thing shows live state** in a `Debug` foldout in its inspector
  (`[ShowInInspector]` — services show it on the asset itself, selectable mid-play).
- **Every event is raisable, every action pressable.** Ripple events ship an Invoke
  button; JamKit components expose `[Button]` debug actions (Damage, Play, Spawn, Reload).
  Clicking the button must exercise the *real* wiring, not a shortcut.
- **Builds are debuggable too.** The DebugPanel ships in every scaffolded scene (Backquote)
  because WebGL jam builds have no inspector.
- **The feature-done checklist:** live state visible · trigger pressable from the
  inspector · misconfiguration caught by `[Required]`/Doctor · one sentence in the README.

## 4. LEAN — less code that works really well (the meta-pillar)

- **Never duplicate what the stack already ships.** Before writing anything, check the
  layer that owns the concern:

  | Layer | Owns |
  |---|---|
  | **Ripple** | State + events: variables (with opt-in persistence), typed events, listeners, references, runtime sets, raise buttons, event logger |
  | **Feel** | Visual/physical feedback: MMF_Player stacks (scale punches, flashes, shakes, particles, floating text) |
  | **FMOD** | Audio: events, buses, music states, mixing |
  | **Odin** | Editor UX: buttons, live state, validation, dropdowns — attributes instead of custom inspectors |
  | **JamKit** | The glue: services for *behavior*, gameplay primitives, menus/scene flow, and the editor tooling that wires the stack together |

- **Services are for behavior, not state.** A service exists only where code must wrap
  engine/native machinery: scene loading + fades, the timescale stack, input map
  switching, pooling, file IO, FMOD instances. If it's *just data* (score, volume, HP,
  a timer readout), it's a Ripple variable, not a service.
- **A gameplay component earns kit inclusion by appearing in 3+ genre columns** of the
  ROADMAP archetype matrix. 1–2 columns → it lives in a sample, hackable and copyable.
- **Ergonomics live at edit time.** Runtime stays tiny and explicit; wizards, presets,
  and auto-assign carry the convenience. Zero-friction defaults, explicit wiring.

---

### Ownership rules that keep the pillars honest

- `Time.timeScale` has exactly one owner: **TimeServiceSO** (a push/pop stack, so pause +
  hit-stop + slow-mo compose). Feel's time feedbacks are off-limits; use `HitStop` or the
  `MMF_JamKitHitStop` wrapper from the Feel Showcase sample.
- Persistence has exactly one mechanism: **Ripple persistent variables** (volumes, high
  score). `SaveServiceSO` is for game-shaped JSON saves, not settings.
- Feedback triggering has exactly one shape: gameplay exposes UltEvents
  (`Health.OnDamaged → MMF_Player.PlayFeedbacks()`); global reactions subscribe to Ripple
  broadcast events. Feedback components never know *why* they played.
