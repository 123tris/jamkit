# Changelog

All notable changes to JamKit are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
