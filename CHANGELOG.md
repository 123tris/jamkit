# Changelog

All notable changes to JamKit are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
