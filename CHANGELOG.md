# Changelog

All notable changes to JamKit are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Theme: an object-oriented review pass (*The Object Oriented Way*, Okhravi) — kill conditionals re-run in the core, move string failures from run-time to edit-time. Rationale and the full validated/rejected map live in [Documentation~/object-oriented-review.md](Documentation~/object-oriented-review.md); PILLARS.md gained a strategy-object rule (2) and a silent-no-op rule (3).

### Breaking — serialized fields changed shape
- **Scene names are now `SceneRef`, not `string`.** `MenuController.GameSceneName`/`MainMenuSceneName` → `GameScene`/`MainMenuScene`; `GameOverController.RetrySceneName`/`MainMenuSceneName` → `RetryScene`/`MainMenuScene`; `TriggerZone.LoadScene` and the sample `SurvivorLoop.GameOverScene` keep their names but change type. `SceneRef` is picked from a Build Settings dropdown, so a misspelled scene is caught at edit time instead of returning null at run time. **Migration:** the shipped samples are migrated; in your own scenes/prefabs, defaults (`"Game"`/`"Bootstrap"`) reapply and any *customized* scene name must be re-picked from the dropdown. `SceneServiceSO.LoadAsync(string)` is unchanged (`SceneRef` converts implicitly), so game code passing raw strings still compiles.
- **`PatrolMover.Mode` is a `[SerializeReference]` strategy, not an enum.** The `EndMode` enum + `switch` became `PathEndBehavior` with `PingPong`/`Loop`/`Stop`/`TeleportToStart` classes, picked from the inspector's type dropdown. **Migration:** legacy `Mode` values reset to `PingPong` (the old default); re-pick if a mover used Loop/Stop/TeleportToStart.
- **Gameplay tuning numbers are now Ripple `FloatReference`/`IntReference` (constant-or-variable), not bare `float`/`int`** — so one shared Ripple variable can drive a value everywhere at once (an upgrade raising max HP, a difficulty knob scaling damage/speed/spawn-rate) while a plain constant still works with no asset. Converted: `Health.Max`; `Damager.Damage`; `TriggerZone.Damage`/`ScoreValue`; `Pickup.ScoreValue`; `ProjectileShooter.Speed`/`FireInterval`; `Spawner.Interval`/`MaxAlive`; `Mover2D`/`Mover3D.MoveSpeed`/`JumpSpeed`; `ChaseMover.Speed`/`StopDistance`; `PatrolMover.Speed`/`WaitAtPoints`; `AutoDespawn.Seconds`; `SpawnBurst.Count`/`Scatter`/`LaunchSpeed`; `Respawner.Delay`; `HitStop.Duration`. **Migration:** Unity can't read an old scalar into the new reference struct, so each converted field **resets to its default constant** — re-enter any *customized* value (the inspector shows a Constant/Variable toggle; flip to Variable to point at a shared asset). Regenerate the shipped samples (sample authoring tool) and `Assets/_Project` (`JamKit > New Jam Project`); the defaults match the old numbers, so only hand-tuned instances need attention. Reads via the implicit `FloatReference → float` (e.g. `float d = damager.Damage;`) still compile; code that *writes* these fields now assigns `new FloatReference(x)` / `new IntReference(x)`.
- **`Health.CurrentVariable` is now a *two-way* link** (it was write-only). It still mirrors current HP outward for HUD binding, but an external write to that variable — a checkpoint, a cheat, a debug panel, another system — now drives the Health back: clamped to `Max`, firing `OnDied` if it reaches zero. Leave it null on pooled/multiple instances so each keeps its own HP (a shared asset pools their health together).

### Added
- **`SceneRef`** (`Runtime/Scenes/`) + Build Settings dropdown drawer (`Editor/Drawers/`).
- **`IMotor` + `Motor`** (`Runtime/Gameplay/Motor.cs`) — the single home of the `Rigidbody2D`/`Rigidbody`/`Transform` branch, resolved once at `Awake` (or one-shot via `Motor.LaunchBody`). Collapses that branch out of `PatrolMover`, `Respawner`, `SpawnBurst`, `ProjectileShooter` — the per-frame conditional is gone. (`ChaseMover` keeps its branch: its 2D/3D paths differ in behavior, not just body type.)
- **`SaveServiceSO.TryRead<T>(key, out value)`** — distinguishes "no save" (false, no log) from "unreadable/corrupt" (false, logs the cause); `Read` delegates to it. Save keys are now validated against path traversal (`..`, separators) and refused loudly.
- **One-shot misconfiguration warnings** where `[Required]` can't reach (it guards objects, not strings): `LabelBinding`/`BarBinding` unresolved element name, `SceneServiceSO` load with no runner, `MenuController` dead button.
- **`Health.SetCurrent(float)` + `Health.MaxValue`** — set current HP directly (clamped to `[0, Max]`, fires `OnDied` at zero) for checkpoints, cheats, and revives; `MaxValue` resolves the `Max` reference for gameplay/HUD code. With the two-way `CurrentVariable`, a `BarBinding`/`LabelBinding` HUD and any gameplay system can now both read *and* edit current HP through one shared asset.
- Tests: `MotorTests`, `SceneRefTests`, and `SaveServiceSOTests` extended (missing-vs-corrupt, path-traversal rejection).

## [0.9.1] - 2026-07-21

### Changed
- **The menu markup is now a project-owned template.** `JamKitMenu.uxml` + `.uss` moved out of the package's `Runtime/UI/Resources/` into `Editor/Templates/`, and `JamKit > New Jam Project` copies them into `Assets/_Project/UI/Resources/` — the same flow the mixer and PanelSettings already used. Designers edit the real menu in UI Builder instead of read-only files in `Library/PackageCache`, and the copies are never overwritten on re-scaffold. The UXML's `<Style>` is now a relative `src="JamKitMenu.uss"`, so template and copy each bind to their own sibling stylesheet with no GUID rewriting.
- **Existing projects migrate by re-running the wizard** — `JamKit > New Jam Project` re-points an already-scaffolded `JamKitMenu.prefab` (`UIDocument.visualTreeAsset` + `MenuController.MenuUxml`) from the package asset to the project copy. `JamKitDefaultTheme.tss` stays package-side; it's plumbing, not designer content.

## [0.9.0] - 2026-07-17

Theme: the pillar redesign — **modular / editable / debuggable / lean** ([PILLARS.md](PILLARS.md) is new and governs everything). JamKit is now explicitly the glue layer of a four-part stack: Ripple owns state + events, Feel owns feedback, FMOD owns audio, JamKit owns behavior services, primitives, menus, and editor tooling. Less code that works really well.

### Breaking — the headline cuts
- **Juice Lite is gone.** `JuiceBehaviour` (and its hidden `GetComponentInParent<Health>()` sibling-trigger magic), `PunchScale`, `MaterialFlash`, `SpriteFlash`, `CameraShake`, `ParticleBurst`, `FloatingText` + `FloatingTextLayer` — all deleted. Feel covers every one (coverage map in `Documentation~/feel-integration.md`). Survivors, as plain components with no base class: `HitStop` (freeze-frames must route through the TimeService stack; Feel's time feedbacks fight the pause menu — hard rule), `SfxOnEvent` (moved to `Runtime/Audio/`), `FmodSfxOnEvent` (audio glue — Feel can't drive FMOD).
- **`ScoreServiceSO` and `TimerServiceSO` (+ runner) deleted** — services are for *behavior*, state belongs to Ripple. Score/HighScore are plain `FloatVariableSO`s (HighScore persistent) plus a 35-line `HighScoreTracker` on JamKitCore; the round clock is the new scene-owned `GameTimer` component (a round timer is scene state — clean-slates pillar). Fixes the domain-reload-off score leak by deletion.
- **Health redesigned as the hub.** Per-instance surface is now serialized UltEvents — `OnDamaged(float)` / `OnHealed(float)` / `OnDied` — replacing the C# events; wire feedback players, respawners, bursts, or code onto the same visible slots. Global Ripple slots renamed `Broadcast*` (`BroadcastDamaged`, `BroadcastDied`; the global heal slot is cut) so global-vs-instance is unmissable. Kit-wide naming rule: `On*` = per-instance UltEvent, `Broadcast*` = global Ripple asset (also applied to `Pickup`, `TriggerZone`, `Respawner`, `Interactable`, `ProjectileShooter`).
- **Catalog trim (the archetype-matrix rule, enforced).** Merged: `Damager2D` → `Damager` (2D+3D callbacks in one, plus `OncePerTarget` absorbed from the deleted `Hitbox`/`Hurtbox`); `CinemachineFollow2D/3D` → `FollowCamera`. Moved to samples as hackable local scripts: `WaveSpawner`, `Aimer`, `Knockback` (02 Survivor); `Bouncer2D`, `Paddle`, `GridMover`, `ThrustMover2D`, `ScreenWrap2D` (04 Arcade). Cut: `FSM`, `ObjectPool<T>`, `GizmoX`, `Stopwatch`. `Respawner` is now a pure teleporter — death-trigger and refill are visible UltEvent wiring, pre-wired on starters.
- **Requires Odin Inspector.** All assemblies gate on `ODIN_INSPECTOR` (the stack already did — Ripple requires it); a missing Odin is a clean assembly skip.

### Added
- **`PILLARS.md`** — the engineering contract (modular / editable / debuggable / lean) with the layer-ownership table and the feature-done checklist.
- **`ServiceSO` / `ServiceSO<TRunner>` / `ServiceRunner<TService,TRunner>`** — one base for every service: registration, null-safe no-op routing, `HasRunner` debug, and the play-session `ResetState` contract (ExitingEditMode hook — domain-reload-off safe). Kills eight hand-rolled copies. The FMOD service overrides the runner-registration reset so music survives scene loads — the opt-out proving the hook.
- **Ripple persistent variables power all settings persistence** — volume + HighScore variables ship with Persist on; both audio runners lost their PlayerPrefs blocks. One persistence mechanism for the whole stack (requires Ripple with the 0.9 persistence feature).
- **Starter prefab library** — the wizard scaffolds `Assets/_Project/Prefabs/Starters/` (11 archetypes) pre-wired to the project's services, with visible UltEvent trigger wiring (players: `OnDamaged → HitStop.Play`; enemies: `OnDied → SpawnBurst.Burst`); with Feel installed, Health starters also carry an `MMF_Player` with `OnDamaged → PlayFeedbacks()` already connected. `GameObject > JamKit > …` now places starter instances — composition lives in prefab assets, scenes stay lists of prefab instances, designers customize via prefab variants.
- **FMOD-first scaffold** — with FMOD installed the wizard creates no mixer and no Unity audio service; `FmodAudioService` rides JamKitCore and the menu sliders drive buses through the persistent variables. The Unity-mixer backend remains the FMOD-less fallback.
- **Template assets** — authored `JamKitMixer.mixer` + `JamKitPanelSettings.asset` ship in `Editor/Templates/` and are copied on scaffold, replacing ~270 lines of reflection-based builders.
- **Odin-powered debuggability** — `Debug` foldouts with live state on every stateful service/component, `[Button]` debug actions that exercise the real wiring (Health Damage/Heal/Kill/Reset replaces the custom `HealthInspector`), `[Required]` on load-bearing refs (surfaced by Odin Validator). `DebugPanel` auto-populates scene buttons from Build Settings and ships on JamKitCore (Backquote — works in WebGL builds).
- **`RuntimeSetMember`** + `ChaseMover.Targets` — data-driven targeting through Ripple runtime sets (`GameObjectListSO`); the tag scan remains as fallback.
- **Auto-assign upgrades** — an `ObjectChangeEvents` hook fills references when a prefab is dragged into a scene; `FillPrefabAsset` wires imported sample prefabs at the asset level.
- **Docs**: `feel-integration.md`, `debugging-and-tuning.md`; README rewritten around the stack; `Tools~/SampleAuthoring/` holds the batch-mode script that (re)builds all sample content.

### Changed
- **Samples rebuilt prefab-first (7 → 5):** 00 Hour Zero, 01 Platformer, 02 Survivor, 03 Feel Showcase (requires Feel; ships `MMF_JamKitHitStop` + `FeelPlayer`), 04 Arcade. Each is a shipped scene of prefab instances — the build-the-world-in-`Awake` demo pattern is dead. One-click setup now wires the imported prefab *assets* to your service assets and drops the project's JamKitCore into the shipped scene.
- **`JamKit > Validate Setup` → `JamKit > Doctor`** — per-field null scanning removed (`[Required]` + Odin Validator own it); keeps project-shape checks with Fix buttons; adds the scenes-as-prefab-lists nudge.
- `MenuCanvasBuilder` / `MenuCanvasPrefabCreator` / `AudioMixerCreator` / `PanelSettingsCreator` deleted (wizard + templates absorbed them).
- `compile-check.sh`: Windows-path fix, empty-source-leg skip, staged test references, Feel-sample exclusion.

## [0.8.0] - 2026-07-12

Theme: FMOD as a first-class audio backend. Install FMOD for Unity and the kit grows an FMOD service, juice receiver, and menu sounds — remove it and they vanish instead of breaking the compile.

### Added
- **FMOD integration** (`Metz.JamKit.Fmod` + `.Editor` assemblies, gated on the new `JAMKIT_FMOD` define):
  - `FmodAudioServiceSO` / `FmodAudioServiceRunner` — the FMOD flavor of the audio service. One-shots (`PlaySfx` positional/attached), music with code-driven fades, `DuckMusic` / `PlayStinger` parity with the Unity path, plus `SetMusicParameter` / `SetGlobalParameter` for Studio-authored intensity layers. Volume drives the `bus:/Music` / `bus:/SFX` buses (paths configurable) through the **same Ripple variables and PlayerPrefs keys** as `AudioServiceSO`, so menu sliders and persisted settings work unchanged with either backend. The music `EventInstance` lives on the SO and survives scene loads; instances mid-fade-out are swept by the departing runner so nothing plays unowned. Missing buses warn once with the authoring fix; an uninitialized FMOD system degrades to warned no-ops instead of throwing.
  - `FmodSfxOnEvent` — Juice Lite receiver for FMOD events (same three triggers as `SfxOnEvent`; randomization/pitch stay in Studio where they belong). `FmodMenuSounds` — hover/click FMOD events for a sibling `MenuController`.
  - `FmodDefineSync` (in the main editor assembly, so it needs no FMOD) — probes for the FMODUnity assembly after every domain reload and toggles `JAMKIT_FMOD` on the jam build targets. Install FMOD → components appear; delete FMOD → they disappear cleanly.
  - `FmodJamKitSetup` — post-scaffold step that creates `FmodAudioService.asset` (sharing the wizard's Ripple volume variables), puts `FmodAudioServiceRunner` on the `JamKitCore` prefab and `FmodMenuSounds` on `JamKitMenu`. Runs inside `New Jam Project` and one-click sample setup when FMOD is present, and on demand via `JamKit > Setup > Add FMOD Audio Service` for projects scaffolded before FMOD was installed. All create-or-load.
  - Validate window: FMOD checks — integration not linked to a Studio project, missing `FmodAudioServiceSO`, missing volume variables, core prefab without the runner (with Fix buttons).
- **Extension points for optional integrations:** `JamProjectWizard.PostScaffold` (steps appended from `[InitializeOnLoad]`, run at the end of every `Scaffold`) and `JamKitValidateWindow.ExtraScans` (extra checks reporting through the window's `IssueReporter`). FMOD is the first consumer; Wwise or Steam could ride the same rails.
- `Tools~/compile-check.sh` builds the FMOD assemblies too (compiling `FMODUnity.dll` from the integration's sources first) whenever the project has `Assets/Plugins/FMOD` or `FMOD_SRC` points at one; skipped with a note otherwise, so the base kit stays verifiable without FMOD.

### Changed
- `JamProjectWizard` exposes `MenuPrefabPath` alongside the existing scene/prefab path constants.

## [0.7.0] - 2026-07-12

Theme: zero-step samples. Every sample README's setup section is now a single menu click (or one dialog button at import time) — the manual steps stay in the READMEs as documentation of what the automation does.

### Added
- **One-click sample setup** — `JamKit > Samples > Set Up <sample>`, also offered by a dialog right after a sample is imported (once per sample per project; never in batch mode). It scaffolds `Assets/_Project` if missing (the wizard, minus its dialogs), opens or builds the demo scene, adds the demo component, auto-assigns its service references, saves, selects the object, and logs the play hint plus any optional fields left for the README's Feel/SFX wiring. Idempotent: re-running reopens the scene and re-fills references; nothing is ever overwritten.
  - Each sample gets its own scene saved beside the imported sample (camera + CinemachineBrain + impulse listener, directional light, JamKitCore instance, EventSystem), so removing the sample removes its scene too. Exception: **04 Survivor Mini** is added to the wizard's `Game.unity` — the GameOver screen's Retry loads the scene *named* "Game", so that's where the demo must live for the loop to cycle.
  - Menu entries gray out until the sample is imported; `JamKit > Samples > Import Samples (Package Manager)…` is the door to the import UI.
- **Editor-test compile coverage** — `Tools~/compile-check.sh` now also builds `Tests/Editor` (against the runtime-tests reference set + the fresh Runtime/Editor dlls).
- **Sample-registry tests** (`Tests/Editor/SampleSetupRegistryTests.cs`) — registry ↔ `Samples~` folders ↔ `package.json` consistency: folder, demo script, README, and display-name entries all have to agree, so a sample rename can't silently break one-click setup.

### Changed
- **`JamProjectWizard.Scaffold(bool overwriteScenes)`** — the wizard's work is now callable without its dialogs (sample setup uses it; `New Jam Project` behavior is unchanged). Scene/prefab path constants (`GameScenePath`, `CorePrefabPath`, …) and the camera/light/EventSystem scene-piece builders are exposed to editor tooling.
- Sample READMEs lead with the one-click setup; the numbered manual steps remain as the reference.

## [0.6.0] - 2026-07-08

Theme: everything left on the roadmap that doesn't require a human with a GitHub account — samples that prove the kit, docs that double as test scripts, tests for the new math, prefab-first scaffolding, sound polish, two-player input, and a WebGL button. Plus three re-evaluations from the open-questions list.

### Added
- **Sample 05 "Juice Toggle"** — turret vs target; press J to flip every `JuiceBehaviour` on/off at once. SFX are synthesized in code, so the before/after needs zero assets.
- **Sample 06 "Arcade Playground"** — frogger crossing (GridMover, conveyor PatrolMover cars, TriggerZone kill-water + goal, Respawner), an Interactable lever, and a self-playing breakout pit (Bouncer2D + angled Paddle + brick rows with per-brick juice) in one runtime-built scene.
- **Sample compile harness** — `Tools~/compile-check.sh` now compiles all of `Samples~` against the fresh Runtime dll; sample code can never silently rot again (it immediately caught the impulse-listener bug below).
- **`Documentation~`** — Hour Zero Checklist, Pong in 60 Seconds, Frogger in 5 Minutes, Graduating to Feel. Written as manual test scripts: if a step fails, that's a bug.
- **Tests** — ScoreServiceSO (accumulate/clamp/high-score/reset), TimerServiceSO (both modes, cap, pause, reset; driven via the internal Tick — new `InternalsVisibleTo`), `Bouncer2D.ClampAwayFromAxes`, `GridMover.SnapToGrid`.
- **Two-player keyboard input** — `JamKitInput` gains `Gameplay1` (WASD) and `Gameplay2` (arrows) maps; `InputServiceSO.AutoEnableGameplay` lets a secondary service activate its own map (menus only drive the default service's). Two paddles = two SO assets.
- **Menu sounds** — optional hover/click clips on `MenuController`; hover fires on gamepad focus too.
- **Music ducking** — `AudioServiceSO.DuckMusic()` and `PlayStinger(clip)` (duck through the mixer param, restore to the Ripple value so the settings slider always wins); `SfxOnEvent.DuckMusic` toggle turns any event sound into a stinger.
- **`JamKit > Build > WebGL (itch.io)`** — gzip + decompression fallback (the two settings that black-screen itch uploads), builds enabled scenes, reveals the folder with zip instructions.
- **CI scaffold** — `.github/workflows/ci.yml` (game-ci edit-mode tests on an assembled temp project), gated off with documented TODOs: publish Ripple to a git URL, provide UltEvents, add the UNITY_LICENSE secret.

### Changed
- **Prefab-first wizard.** `JamKitCore` (now also carrying `FloatingTextLayer` + `Toast` children) and `JamKitMenu` are prefab assets instanced into each scene — a fix propagates everywhere; scenes override only `InitialView`/`PauseController`. The wizard also offers fast enter-play-mode (domain reload off) after setup.
- **`Paddle` marker replaces `Bouncer2D.PaddleLayers`** (open question resolved): layers are project-global state the kit shouldn't own; a component auto-wires, carries a per-paddle `EnglishMultiplier`, and reads clearer in the hierarchy.
- **Player presets ship CameraShake + HitStop** (open question resolved) — player damage is the hit that matters; enemies keep flash/pop only.
- Testability: min-angle clamp and grid snap are public statics (`ClampAwayFromAxes`, `SnapToGrid`).

### Fixed
- **Camera shake never worked in wizard scenes**: the wizard put `CinemachineImpulseListener` (a vcam *extension*) on a plain camera with only a Brain, where it never runs. Replaced with `CinemachineExternalImpulseListener`, the standalone listener for regular cameras.
- **`Bouncer2D` min-angle clamp under-delivered**: nudging the flat component to `sin(minAngle)` and re-normalizing shrank it back below the threshold, so near-flat rallies stayed slightly flatter than configured. The clamp now rebuilds the direction exactly at the limit angle (unit length by construction). Caught by the new play-mode tests on their first run.

## [0.5.0] - 2026-07-08

Theme: the three roadmap milestones landed together — Juice Lite (M2), the any-genre primitives (M3), and the editor friction killers (M1). A pong, frogger, asteroids, breakout, or survivor core is now a handful of `GameObject > JamKit` clicks.

### Added
- **Juice Lite** (`Runtime/Juice/`) — zero-new-dependency feedback, all ≤ ~100 lines each: `CameraShake` (Cinemachine impulse), `HitStop` (TimeService freeze), `SpriteFlash` (vertex-color tint, no shader tricks), `MaterialFlash` (MaterialPropertyBlock), `PunchScale` (squash & stretch, allocation-free), `ParticleBurst`, `SfxOnEvent` (random clip + pitch variation via AudioService), `FloatingText` + `FloatingTextLayer` (pooled UI Toolkit damage numbers, world→panel projection, no TMP), and `Toast` (event → banner text, Void and Int flavors).
- **`JuiceBehaviour` base + per-instance Health events.** Every juice component triggers three ways: sibling `Health` C# events (`Damaged`/`Healed`/`Died` — new; per-instance, zero wiring), optional global Ripple event slots, and a public `Play(strength)` for UltEvents/Feel/code. This fixes the granularity gap where shared Ripple assets would flash *every* enemy when one was hit.
- **Genre primitives:** `Bouncer2D` (constant-speed reflection, per-bounce speed-up, paddle english, min-angle anti-deadlock, OnBounce event), `GridMover` (4-way stepped movement, XY/XZ, block layers, hold-to-repeat), `ThrustMover2D` (rotate+thrust, drift or damped), `ChaseMover` (2D/3D pursuit, nearest-by-tag with rescan), `PatrolMover` (offsets-from-start waypoints; PingPong/Loop/Stop/TeleportToStart conveyor mode; pool-aware), `ScreenWrap2D`, `Aimer` (mouse cursor or right stick, XY/XZ), `TriggerZone` (tag/layer-filtered damage/kill/score/remove/scene-load/events — kill pit, goal, and score gate in one component), `SpawnBurst` (N pooled prefabs with scatter + outward launch — death explosions, asteroid splits, loot), `Respawner` (checkpoint teleport + health refill on death or on demand), and `Interactor` + `Interactable` ("press E" with a zero-plumbing prompt visual; 2D+3D probing, allocation-free).
- **Editor: auto-assign.** Adding a JamKit component fills its null JamKit-typed references automatically when exactly one candidate exists (service SOs from assets, scene components from the open scenes). Menu: `JamKit > Auto-Assign References In Open Scenes`.
- **Editor: `GameObject > JamKit` presets.** 16 pre-wired archetypes (players ×3 flavors, ship, enemies ×2, ball, paddle, patrol hazard, kill zones ×2, cameras ×2, spawner, pickup, floating-text layer, toast) using built-in sprites / primitives, auto-assigned on creation.
- **Editor: `JamKit > Validate Setup` window.** Checks mixer exposed params, PanelSettings themes, wizard scenes in Build Settings, EventSystem presence, unassigned JamKit references (with ambiguity listing), HitStop-without-TimeServiceRunner, FloatingText-without-Layer — Fix buttons where the fix is unambiguous.

### Changed
- `Health`: pool-aware (refills on `IPoolable.OnSpawn`) and can despawn to a `DespawnPool` instead of destroying.
- `Damager` / `Damager2D`: optional `PoolService` so `DestroyOnHit` despawns pooled projectiles instead of destroying them.
- `Mover2D`: new `AxisScale` locks top-down movement to one axis (paddles, invaders-style players).
- README rewritten around the three-trigger juice model and an archetype-coverage table.

## [0.4.1] - 2026-06-12

### Fixed
- **Volume sliders actually drive the mixer.** `AudioMixerCreator` exposed no parameters (it probed for a nonexistent `AddExposedParameter` method and swallowed the failure) — the Editor log showed `Exposed name does not exist: MasterVol/MusicVol/SfxVol`. Exposure now goes through the controller's `exposedParameters` array, the whole routine is idempotent (re-running `JamKit > Setup > Create Audio Mixer` repairs an existing broken mixer), and the result is verified with `AudioMixer.GetFloat` — failures log manual-setup steps instead of silence.
- **PanelSettings always get a theme.** Every runtime-created PanelSettings lacked a `ThemeStyleSheet` ("UI will not render properly" warning; unstyled controls). JamKit now ships `JamKitDefaultTheme.tss` (imports `unity-theme://default`) and the new `JamKitUI` helper assigns it everywhere — including patching theme-less saved assets at load, and a repair branch in `JamKit > Setup > Create Panel Settings`.
- Persisted volumes now apply in `Start` instead of `OnEnable` (`AudioMixer.SetFloat` is unreliable during Awake/OnEnable).
- `FadeOverlay` reuses one cached PanelSettings instead of leaking a new one per scene load.

### Changed
- **Wizard no longer stomps scenes.** If Bootstrap/Game/GameOver already exist, `New Jam Project` asks Keep Existing (default) / Overwrite All / Cancel, and prompts to save open scenes first. Asset creation was already create-or-load.
- **`Pickup` is tag-filtered.** New `RequiredTag` (defaults to Unity's built-in `Player` tag) alongside the layer mask, so enemies/projectiles on the same layer can't hoover up pickups. Set it empty to allow anything.
- **WebGL-aware UI.** On WebGL the Settings menu hides the Resolution/VSync rows and the Start menu hides Quit (the browser owns all three). `SaveServiceSO` flushes IndexedDB via a bundled jslib so saves survive the tab closing. `DebugPanel` toggle default moved F1 → Backquote (browsers swallow F-keys).
- `MenuController` internals: Settings-view logic extracted into `MenuSettingsBinder` (plain class — no serialized fields changed, existing scenes unaffected).
- The package is now a git repository.

## [0.4.0] - 2026-06-07

### Added
- **Score + Timer services.** `ScoreServiceSO` (current/high score, high score persisted via `SaveServiceSO`, Ripple mirrors + `OnScoreChanged`/`OnNewHighScore`) and `TimerServiceSO` + `TimerServiceRunner` (count down/up, Ripple time mirror, `OnTimerComplete`).
- **No-code HUD bindings.** `LabelBinding` and `BarBinding` drive a UI Toolkit label/bar from a `FloatVariableSO` (HP, score, timer) with no glue code, via the shared `UIBindingBase`.
- **Gameplay primitives.** `AutoDespawn` (pool-aware lifetime), `Pickup` (trigger → score/event/despawn, 2D+3D), `ProjectileShooter` (pooled firing on input/interval), and a `Hitbox`/`Hurtbox` pair (weak points, once-per-swing) alongside the existing `Damager`.
- **`GameOverController`** — a wired Retry / Main Menu screen that reuses the menu USS.
- **`DebugPanel`** — toggle-key overlay with FPS, a time-scale slider, scene quick-jump/reload, and quit.
- **Sample 04 — Survivor Mini.** A full tiny loop (Mover3D + Spawner/Pool + Pickup + Score/Timer + HUD + GameOver) using only primitive meshes.

### Changed
- **No Odin dependency.** Removed the unused `ODIN_INSPECTOR` define constraint (and the vestigial `TextMeshPro` reference) from both asmdefs. JamKit now compiles with just Ripple + UltEvents — no paid asset required.
- **Wizard scaffolds the whole loop.** `New Jam Project` now creates the Score/Timer assets, puts a self-contained `JamKitCore` (all runners) in *every* scene so audio/pause/input/transitions work everywhere without a persistent root, adds an `EventSystem` (Input System UI module) per scene for gamepad/keyboard navigation, gives the Game scene a hidden pause layer (Esc), and builds a real GameOver scene.
- **Menu hardening.** Esc now backs out of a stacked submenu instead of pushing a duplicate Pause; volume sliders bind two-way to their Ripple variables with `OnDisable` teardown; the resolution list is deduped by width×height; element names are centralized; the active view's primary control takes focus for controller nav.
- **Input efficiency.** `InputServiceSO` caches action/map lookups instead of doing a string `FindAction` on every access.

### Fixed
- `Spawner.MaxAlive` now caps *concurrent* instances (the counter was never decremented, so it permanently blocked spawning after N total).
- Removed the dead `#fade-overlay` element from `JamKitMenu.uxml` (the real fade is `FadeOverlay`'s own UIDocument) and corrected the docs.
- Reset guards on `TimeServiceSO` / `PoolServiceSO` / `TimerServiceSO` / `InputServiceSO` so mutable state can't leak across play sessions when Domain Reload is disabled.
- Stale doc references (`AudioManager`, `Health.Damaged`).

## [0.3.0] - 2026-05-29

### Changed (breaking)
- **No more singletons.** `ServiceLocator`, `Singleton<T>`, `JamKitRoot` removed. All managers (`AudioManager`, `TimeManager`, `SaveManager`, `SceneFlowManager`, `InputManager`) and their static facades (`Audio`, `Time2`, `Save`, `Scenes`, `Input2`) replaced with `ScriptableObject` services + scene `Runner` MonoBehaviours that components reference directly.
- **Ripple is now a hard dependency.** The optional bridge folder is gone. Audio volumes are `FloatVariableSO`s; `Health`, `Spawner`, `WaveSpawner`, `SceneServiceSO` raise Ripple events natively. Runtime asmdef now requires `ODIN_INSPECTOR` + `ULTEVENTS`.
- **Feel module removed.** `ScreenShake`, `HitFreeze`, `SpriteFlash`, `MaterialFlash`, `FloatingText`, `Tween` deleted. Use Feel (`MoreMountains.Feedbacks`) — wire any Ripple event to `MMF_Player.PlayFeedbacks()` via UltEvents.
- **EventBus removed.** Use Ripple's `GameEvent<T>` / `VoidEventSO` / typed events directly.
- `Pool` static facade removed; `PoolServiceSO` replaces it.
- `PrefsBinding<T>` removed; settings persistence is inline PlayerPrefs reads/writes (graphics) or Ripple variable subscriptions (audio).

### Added
- `Runtime/Services/` folder with: `AudioServiceSO` + `AudioServiceRunner`, `TimeServiceSO` + `TimeServiceRunner`, `SceneServiceSO` + `SceneServiceRunner`, `InputServiceSO`, `SaveServiceSO`, `PoolServiceSO` + `PoolServiceRunner`.
- `JamProjectWizard` now creates the SO assets, the three Ripple volume variables, and a `JamKitCore` GameObject hosting every runner.
- `Health.CurrentVariable` (FloatVariableSO), `OnDamaged` / `OnHealed` (FloatEvent), `OnDied` (VoidEventSO) for designer-friendly HUD wiring.
- `WaveSpawner` events `OnWaveStarted` / `OnWaveEnded` (IntEvent), `OnAllWavesDone` (VoidEventSO).

## [0.2.0] - 2026-05-28

### Changed
- Menus rewritten on UI Toolkit. UGUI menu classes removed; replaced by `MenuController` + `JamKitMenu.uxml` / `.uss`.
- `FadeOverlay` now renders via UIDocument.
- Sample `03 UI Card Flip Mini` rewritten on UI Toolkit.

### Added
- `JamKit > Setup > Create Panel Settings` editor menu.

## [0.1.0] - 2026-05-27

### Added
- Initial release.
