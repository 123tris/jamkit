# JamKit

Rapid game-jam toolkit for Unity 6 — the **glue layer** of a four-part stack. **No singletons,
less code that works really well.** Read [PILLARS.md](PILLARS.md) first: modular, editable,
debuggable, lean — every design decision in the kit traces back to it.

| Layer | Owns |
| --- | --- |
| **[Ripple](https://github.com/123tris/Ripple)** | State + events: SO variables (opt-in persistence), typed events, listeners, references, runtime sets — with raise buttons and an event logger built in |
| **[Feel](https://feel.moremountains.com/)** | Visual/physical feedback: author MMF_Player stacks, JamKit's gameplay events trigger them |
| **[FMOD](https://www.fmod.com/)** | Audio: events, buses, adaptive music (JamKit's FMOD service is the bridge) |
| **JamKit** | Services for *behavior* (scene flow, timescale, input, pooling, saves, audio bridge), a trimmed set of gameplay primitives, the UI Toolkit menu flow, and the editor tooling that wires it all |

## Why

Every jam burns the first hours on the same scaffolding: a start menu, settings, scene
transitions, pooling, save/load, feedback plumbing. JamKit ships all of it pre-wired so hour 1
goes to the actual game — without inheriting a pile of singletons or a framework you have to
fight on day two.

## Architecture at a glance

- **No global statics.** Every system is a `ServiceSO` asset plus an optional scene `Runner`
  MonoBehaviour (one shared base handles registration and the per-session reset that makes
  Domain-Reload-off safe). Components hold serialized SO references — the inspector shows
  exactly what each thing depends on. The editor auto-fills those references; the runtime never
  looks anything up.
- **Services are for behavior, not state.** Scene loading, the timescale stack, input maps,
  pooling, file IO, FMOD instances. Anything that is *just data* — score, volumes, HP, a timer
  readout — is a Ripple variable. (Score and Timer used to be services; they aren't anymore.)
- **Two event granularities, one naming rule.** Per-instance reactions use serialized UltEvents
  named `On*` (`Health.OnDamaged` → *this* enemy's feedback player); global reactions use Ripple
  event assets named `Broadcast*` (`Health.BroadcastDied` → any-enemy-died counters). Both are
  visible in the inspector; there is no hidden `GetComponentInParent` magic anywhere.
- **Feedback is Feel's job.** JamKit ships no tween/flash/shake components — wire
  `Health.OnDamaged → MMF_Player.PlayFeedbacks()` and author the feel in Feel. The exceptions
  are the things Feel can't do: `HitStop` (freeze-frames must route through the TimeService
  stack — Feel's time feedbacks fight the pause menu) and the SFX bridges (Feel can't drive
  FMOD).
- **Scenes are clean slates.** No `DontDestroyOnLoad`. Persistent state lives in SO assets;
  every scene instances the `JamKitCore` prefab (all runners + score tracker + debug panel) and
  works cold.

## Requirements

- Unity 6 (6000.0+); URP 17+, Cinemachine 3.1+, Input System 1.11+ (auto-resolved UPM deps)
- **Odin Inspector** (Asset Store) — required; the whole stack gates on `ODIN_INSPECTOR`
- **Kybernetik UltEvents** (Asset Store / UPM)
- **`com.metz.ripple`** — required
- Recommended: **Feel** (feedback) and **FMOD for Unity** (audio) — both auto-detected, never
  compile-time required by the package

Install order: Odin → UltEvents → Ripple → JamKit. All JamKit assemblies carry
`ODIN_INSPECTOR` + `ULTEVENTS` define constraints, so a missing dependency is a clean assembly
skip, not an error wall.

## Quickstart

1. Install JamKit.
2. `JamKit > New Jam Project`. One click creates:
   - `Assets/_Project/Services/` — the service SOs (Time, Scene, Input, Save, Pool; plus the
     Unity-mixer Audio service *or* — with FMOD installed — the FMOD audio service instead)
   - `Assets/_Project/Variables/` — Ripple variables: Master/Music/Sfx volume (persistent),
     Score, HighScore (persistent), Timer
   - `Assets/_Project/Prefabs/` — `JamKitCore` + `JamKitMenu`, and `Starters/` (see below)
   - `Bootstrap.unity`, `Game.unity`, `GameOver.unity` — each a list of prefab instances
3. Press Play. Start → Settings → Game (Esc pauses) → GameOver → Retry works end-to-end,
   gamepad included. Backquote (`) toggles the DebugPanel — in builds too.

## Starter prefabs (the Lego bricks)

The wizard scaffolds `Assets/_Project/Prefabs/Starters/`: Player2D (platformer/top-down),
Player3D, chaser enemies (2D/3D), Pickup, Spawner, KillZones, FollowCams — each pre-wired to
your service assets, with visible UltEvent trigger wiring (players: `OnDamaged → HitStop`;
enemies: `OnDied → SpawnBurst`). With Feel installed, Health starters also carry an
`MMF_Player` with `OnDamaged → PlayFeedbacks()` already connected — author the feel, the
trigger is done.

**Customize by making prefab VARIANTS** (right-click starter → Create > Prefab Variant), never
by editing scenes object-by-object: scenes stay lists of prefab instances, check-ins happen at
the prefab level, and a starter fix propagates everywhere. `GameObject > JamKit > …` places
starter instances.

## Service SOs

| Service | Type | What it does | Scene runner? |
| --- | --- | --- | --- |
| Audio (FMOD) | `FmodAudioServiceSO` | The primary audio path when FMOD is installed: `PlaySfx(event)`, `PlayMusic` (survives scene loads), `SetMusicParameter`, stingers/ducking. Volume drives FMOD buses through the persistent Ripple variables. | Yes |
| Audio (Unity) | `AudioServiceSO` | Fallback backend without FMOD: mixer + pooled one-shots + crossfade music. Same variable-driven volume. | Yes |
| Time | `TimeServiceSO` | `Pause/Resume`, `Push(scale)/Pop`, `FreezeForSeconds`. The stack composes pause + hit-stop + slow-mo — and it is the ONLY thing allowed to touch `Time.timeScale`. | For `FreezeForSeconds` |
| Scenes | `SceneServiceSO` | `LoadAsync(name)`, `ReloadCurrent()` with fade. Broadcast events on load start/end. | Yes |
| Input | `InputServiceSO` | Move/Look/Jump/Attack/Pause from an `InputActionAsset`; `SwitchToUI/Gameplay()`. | No |
| Save | `SaveServiceSO` | `Write/Read<T>` JSON to `persistentDataPath` — for game saves. Settings persistence belongs to Ripple persistent variables, not here. | No |
| Pool | `PoolServiceSO` | `Spawn/Despawn/Prewarm`, one pool per prefab. | Optional |

Select any service asset **during play** — its Debug foldout shows live state (timescale stack
depth, pool counts, music state) and its `[Button]`s poke the real thing.

## Components (the trimmed catalog)

A component earns its place by appearing in 3+ genres (see ROADMAP's archetype matrix); the
rest live in samples as hackable copies.

- **Movement/camera:** `Mover2D`, `Mover3D`, `ChaseMover` (runtime-set targeting via
  `RuntimeSetMember`, tag fallback), `PatrolMover`, `FollowCamera` (2D+3D, impulse-listener
  equipped for Feel shakes).
- **Combat:** `Health` (the hub — see below), `Damager` (2D+3D in one, projectiles/contact/
  melee via `OncePerTarget`), `Respawner` (pure teleporter — death/refill wired visibly),
  `HitStop`.
- **Spawning/scoring:** `Spawner`, `SpawnBurst`, `ProjectileShooter`, `AutoDespawn`, `Pickup`,
  `TriggerZone` (kill pit / goal / score gate / level exit in one), `GameTimer` (scene-owned
  round clock), `HighScoreTracker` (the only score logic — score itself is a Ripple variable).
- **Interaction:** `Interactor` + `Interactable`.
- **Audio glue:** `SfxOnEvent` / `FmodSfxOnEvent` — one-shots triggered per-instance
  (`Health.OnDamaged → Play`) or globally (Ripple `PlayOn` slot).
- **UI/HUD:** `MenuController` (Start/Settings/Pause), `PauseController`,
  `GameOverController`, `FadeOverlay`, `Toast`, `DebugPanel`, `LabelBinding` / `BarBinding`.
- **Utilities:** `Timer` / `Cooldown` structs, `GameObjectPool` + `IPoolable`, `RandomBag<T>`,
  `MathX` / `VectorX` / `ColorX`.

## Health — the hub

```
Per-instance (UltEvents, THE wiring surface):   OnDamaged(float) · OnHealed(float) · OnDied
Global (Ripple assets):                          CurrentVariable · BroadcastDamaged · BroadcastDied
```

Wire feedbacks (`MMF_Player.PlayFeedbacks`), reactions (`Respawner.RespawnAfterDelay`,
`SpawnBurst.Burst`), or game code onto the per-instance slots; bind HUDs and global logic to
the Ripple side. The inspector's Debug buttons (Damage 1 / Heal 1 / Kill / Reset) fire the
*real* chain — that's the feature-done test from PILLARS.md.

## Feel integration

See [Documentation~/feel-integration.md](Documentation~/feel-integration.md) for the coverage
map (what replaced each old Juice Lite component) and the timescale rule. Short version: add an
`MMF_Player`, wire `Health.OnDamaged → PlayFeedbacks()` (starters come pre-wired), author
feedbacks in Feel. For damage-scaled intensity use the `FeelPlayer` shim from sample 03; for
freeze-frames use `HitStop` or the sample's `MMF_JamKitHitStop` — never Feel's time feedbacks.

## FMOD integration

Automatic: an editor probe toggles `JAMKIT_FMOD` with FMOD's presence. With FMOD installed the
wizard scaffolds **FMOD-first** — no Unity mixer, no Unity audio service; `FmodAudioService`
rides `JamKitCore`, `FmodMenuSounds` covers menu hover/click, and the settings sliders drive
`bus:/Music` + `bus:/SFX` through the same persistent variables. Music survives scene loads
(the EventInstance lives on the SO). Retrofit any time via `JamKit > Setup > Add FMOD Audio
Service`; the Doctor checks the FMOD side too.

## Editor tooling

- **Auto-assign** — adding any JamKit component (or dragging in a prefab) fills its null
  JamKit-typed references when exactly one candidate exists. Ambiguity is never guessed.
  Ripple slots are deliberately excluded: which event something listens to is design, not
  plumbing.
- **`JamKit > New Jam Project`** — the one-click scaffold (see Quickstart).
- **`JamKit > Doctor`** — project-shape checks with Fix buttons (mixer params, themes, Build
  Settings, EventSystem, HitStop↔TimeRunner pairing) plus the scenes-as-prefab-lists nudge.
  Per-field misconfiguration is [Required]'s job — red in the inspector, listed project-wide in
  **Odin Validator**.
- **`JamKit > Build > WebGL (itch.io)`** — one-click itch-safe browser build.
- **`GameObject > JamKit > …`** — places starter prefab instances.
- Debugging & runtime tuning: [Documentation~/debugging-and-tuning.md](Documentation~/debugging-and-tuning.md).

## Samples

`Window > Package Manager > JamKit > Samples` — each is a scene of prefab instances (no
build-the-world-in-code), and importing offers one-click setup that wires the sample's prefabs
to *your* service assets:

- **00 Hour Zero** — the kit tour, zero scripts, zero input.
- **01 Platformer** — the 2D pack + the visible-wiring lesson on the Player prefab.
- **02 Survivor** — the full loop in `Game.unity`; home of `WaveSpawner`/`Aimer`/`Knockback`.
- **03 Feel Showcase** — *requires Feel*; the trigger chain + `MMF_JamKitHitStop`.
- **04 Arcade** — breakout-pong; home of `Bouncer2D`/`Paddle`/`GridMover`/`ThrustMover2D`/`ScreenWrap2D`.

## License

MIT. See [LICENSE.md](LICENSE.md).
