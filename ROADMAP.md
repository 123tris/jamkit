# JamKit Roadmap

Three goals, in tension with each other:

1. **Efficiency** — minutes from "create project" to "playable thing on screen", and minutes from "I need X" to "X is wired".
2. **Juice** — JamKit games should *feel* good by default, because juice is what jam ratings reward.
3. **Flexibility** — nothing in the kit may assume a genre. Platformer, survivor, puzzle, card game: all first-class.

## Design principles (the contract every feature must pass)

- **Explicit wiring, zero-friction defaults.** Components keep serialized SO references (no runtime lookup, no singletons) — the *editor* auto-fills them when the answer is unambiguous (exactly one candidate; never a guess). Convenience at edit time, transparency at runtime. *(Shipped in 0.5.0 — keep every new component compatible: public fields, JamKit-typed references.)*
- **Two event granularities — pick deliberately.** Ripple SO events are global (HUD, stingers, any-X-died). Per-instance reactions go through plain C# events on the component (`Health.Damaged`) that siblings subscribe to automatically. A feature that wires per-instance feedback through a shared SO event is a bug (every enemy flashes when one is hit).
- **Juice = receivers with three triggers.** Sibling Health (zero wiring), Ripple slots (global), public `Play(strength)` (UltEvents/Feel/code). JamKit ships the lightweight 80% with zero new dependencies; Feel is the deluxe path on the same triggers. Hard cap: ~100 lines per juice component, no sequencers, no curve editors.
- **Small orthogonal components, not frameworks.** Flexibility comes from composition. The measure of a new primitive is how many archetype rows it unlocks (see matrix below), not how complete it is.
- **Nothing ships unverified.** Every milestone ends with a clean compile (Roslyn script or Unity), the Validate window green, and the samples compiling.

## The archetype matrix (the evaluation tool)

When considering a new primitive, check it against the classics. A primitive that appears in 3+ columns is kit material; 1 column = game code, leave it out.

| Primitive | Pong | Breakout | Frogger | Invaders | Asteroids | Flappy | Platformer | Survivor | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Input mover | ✓ | ✓ | | ✓ | | ✓ | ✓ | ✓ | ✅ Mover2D/3D |
| Ball bounce | ✓ | ✓ | | | | | | | ✅ Bouncer2D |
| Grid step | | | ✓ | | | | | | ✅ GridMover |
| Rotate+thrust | | | | | ✓ | | | | ✅ ThrustMover2D |
| Chase target | | | | | | | ✓ | ✓ | ✅ ChaseMover |
| Patrol/conveyor | | | ✓ | ~ | | ✓ | ✓ | | ✅ PatrolMover |
| Screen wrap | | | | | ✓ | | | | ✅ ScreenWrap2D |
| Zones (kill/goal/score) | ✓ | ✓ | ✓ | ✓ | | ✓ | ✓ | | ✅ TriggerZone |
| Aim cursor/stick | | | | | | | | ✓ | ✅ Aimer |
| Spawn on death | | ✓ | | ✓ | ✓ | | | ✓ | ✅ SpawnBurst |
| Respawn/checkpoint | ✓ | ✓ | ✓ | | ✓ | | ✓ | | ✅ Respawner |
| Press-E interact | | | | | | | ✓ | ✓ | ✅ Interactor/-able |
| Shoot | | | | ✓ | ✓ | | ✓ | ✓ | ✅ ProjectileShooter |
| HP/damage/waves/pickups/score/menus | — | — | — | — | — | — | — | — | ✅ since 0.4 |
| Juice (shake/stop/flash/pop/burst/sfx/text/toast) | all | all | all | all | all | all | all | all | ✅ Juice Lite 0.5 |

Genres still thin: **card/board** (drag-drop, hand layout), **puzzle-match** (grid queries beyond movement), **rhythm** (beat clock). Add rows when a jam demands them.

## Done (0.5.0) — was M1/M2/M3

- M1: editor auto-assign, `GameObject > JamKit` presets (16), `JamKit > Validate Setup` with Fix buttons.
- M2: Juice Lite — CameraShake, HitStop, SpriteFlash, MaterialFlash, PunchScale, ParticleBurst, SfxOnEvent, FloatingText(+Layer), Toast; `JuiceBehaviour` triple-trigger base; per-instance Health events.
- M3: Bouncer2D, GridMover, ThrustMover2D, ChaseMover, PatrolMover, ScreenWrap2D, TriggerZone, Aimer, SpawnBurst, Respawner, Interactor/Interactable.

## Done (0.6.0) — was M1.5 + M4 + most of M5

- M1.5: prefab-first wizard (JamKitCore carries FloatingTextLayer + Toast; JamKitMenu instanced with per-scene overrides), fast enter-play-mode offer, menu hover/click sounds, music ducking (`DuckMusic`/`PlayStinger`/`SfxOnEvent.DuckMusic`), two-player keyboard input (`Gameplay1`/`Gameplay2` maps + `AutoEnableGameplay`).
- M4: Sample 05 Juice Toggle, Sample 06 Arcade Playground, `Documentation~` (hour-zero, pong-60s, frogger-5min, graduating-to-feel), tests for ScoreService/TimerService/Bouncer math/GridMover snap.
- M5 (the parts a tool can do): sample compile harness in `Tools~/compile-check.sh` (caught the wizard's dead impulse listener on day one), `JamKit > Build > WebGL (itch.io)`, CI workflow scaffold.

### Open questions — resolved

- Players get CameraShake + HitStop in presets: **yes** (enemy→player damage is the hit that matters).
- Toast/FloatingTextLayer: **in the JamKitCore prefab** (every scene gets them for free).
- Paddle english: **marker component**, not a layer mask (layers are project-global state; the marker auto-wires and carries per-paddle `EnglishMultiplier`).

## Done (0.7.0)

- **One-click sample setup** — import offer + `JamKit > Samples` menu automate every sample README's setup section: non-interactive `JamProjectWizard.Scaffold`, per-sample ready-to-play scenes (Survivor rides `Game.unity` so the GameOver → Retry loop cycles), auto-assigned services, registry tests, editor-test compile coverage in the harness.

## Done (0.8.0)

- **FMOD backend** — define-gated `Metz.JamKit.Fmod` assemblies (auto-detected, `JAMKIT_FMOD`): `FmodAudioServiceSO`/Runner on the same Ripple volume variables + PlayerPrefs keys as the Unity path, `FmodSfxOnEvent`, `FmodMenuSounds`, wizard/sample scaffolding + Validate checks, FMOD legs in the compile harness. First consumer of the new integration rails: `JamProjectWizard.PostScaffold` + `JamKitValidateWindow.ExtraScans` (Wwise/Steam could ride the same hooks).

## M5 remainder — needs a human (cannot be done from inside the repo)

1. ~~Publish Ripple to a git URL~~ **Done** — `https://github.com/123tris/Ripple.git#feature/abstraction+runtime-registry` (the branch pin matters: Ripple's `main` is an older API; when Ripple work merges to main, update the pin in the dev manifest, README, and ci.yml).
2. ~~Push JamKit to GitHub~~ **Done** (github.com/123tris/jamkit). Still open: add the `UNITY_LICENSE` secret ([game.ci activation](https://game.ci/docs/github/activation)), decide how CI gets UltEvents (git mirror or vendored), then delete the `if: false` gate in ci.yml.
3. **Editor playtest pass** — 0.5.0/0.6.0 are compile-verified and logic-tested, but the wizard flow, presets, and both new samples deserve one live run in the editor (the Documentation~ walkthroughs are the scripts for exactly this).

## M6 — candidates (pull when a jam demands, per the matrix)

- **Card/board genre row**: UI Toolkit drag-and-drop helper, hand/fan layout.
- **Match/puzzle row**: grid *queries* (neighbors, flood fill) on top of GridMover's grid.
- **Log-riding / moving-platform carry** — parent-on-contact helper; came up in both frogger and platformer recipes.
- **`SynthSfx` placeholder audio** — Sample 05 synthesizes its blips in ~15 lines; promoting that to a tiny runtime util would give every jam hour-zero hit/pickup/death sounds with zero assets. (Samples stay self-contained on purpose — each is a copy-paste reference — so shared helpers only graduate by moving into Runtime, never into a shared samples folder.)
- Input-rebinding screen, save-slot UI, world-space UI Toolkit HUD (6.2), JamKit Hub window.

## Non-goals (the identity fence)

No tween library, no node graphs, no netcode, no ECS, no Feel clone. JamKit is the wiring and the 80% defaults — depth lives in dedicated assets.
