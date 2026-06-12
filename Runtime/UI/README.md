# JamKit Menus — Start / Settings / Pause (UI Toolkit + Ripple)

JamKit's menu system is **UI Toolkit** (UXML + USS) driven by a single `MenuController` MonoBehaviour. Audio volume sliders bind directly to **Ripple `FloatVariableSO`s** so any other UI or system in the project can read or write the same volume state without going through the menu.

## Three ways to get a working menu

1. **One-click setup:** `JamKit > New Jam Project` builds the Bootstrap scene with `JamKitCore` (hosting every Runner) and `JamKitMenu` (UIDocument + MenuController with all service SOs assigned).
2. **Reusable prefab:** `JamKit > Create Menu Prefab` saves a `JamKitMenu.prefab` with empty service slots — drag into any scene, assign the SOs from `Assets/_Project/Services/`.
3. **Manual:** Add `UIDocument` + `MenuController` yourself. Leave UXML blank — `MenuController` auto-loads `Resources/JamKitMenu.uxml`. Assign the four service SOs (`AudioService`, `TimeService`, `SceneService`, `InputService`).

## MenuController inspector fields

| Field | Purpose |
| --- | --- |
| `MenuUxml` | UXML asset. Leave blank to use bundled `Resources/JamKitMenu.uxml`. |
| `ExtraStyles` | Optional USS layered on top of the bundled stylesheet. |
| `AudioService` | `AudioServiceSO` — used to find the master/music/sfx Ripple variables for sliders. |
| `TimeService` | `TimeServiceSO` — used for pause/resume around the Pause menu. |
| `SceneService` | `SceneServiceSO` — used to load scenes when buttons fire. |
| `InputService` | `InputServiceSO` — used to switch the active action map when pausing. |
| `MasterVolumeOverride` / `MusicVolumeOverride` / `SfxVolumeOverride` | Optional FloatVariableSO overrides; otherwise the menu pulls them from the `AudioServiceSO`. |
| `GameSceneName` | Scene the Play button loads. |
| `MainMenuSceneName` | Scene the Pause → Main Menu button loads. |
| `InitialView` | Which view shows on enable (Start / Settings / Pause / None). |

## UXML structure

```
#root .jk-root
├── #start-menu      .jk-view .jk-view--start
│   ├── #start-title .jk-title
│   └── .jk-button-column
│       ├── #start-play     .jk-button
│       ├── #start-settings .jk-button
│       └── #start-quit     .jk-button
├── #pause-menu      .jk-view .jk-view--pause  (display:none)
│   ├── .jk-dim
│   └── .jk-panel
│       ├── .jk-title
│       └── .jk-button-column
│           ├── #pause-resume   .jk-button
│           ├── #pause-settings .jk-button
│           ├── #pause-restart  .jk-button
│           └── #pause-mainmenu .jk-button
├── #settings-menu   .jk-view .jk-view--settings (display:none)
│   ├── .jk-dim
│   └── .jk-panel.jk-panel--wide
│       ├── .jk-title
│       ├── .jk-section     "Audio"
│       │   ├── Slider  #settings-master   (binds to Audio.MasterVolume Ripple variable)
│       │   ├── Slider  #settings-music    (binds to Audio.MusicVolume Ripple variable)
│       │   └── Slider  #settings-sfx      (binds to Audio.SfxVolume Ripple variable)
│       ├── .jk-section     "Graphics"
│       │   ├── DropdownField #settings-quality
│       │   ├── DropdownField #settings-resolution
│       │   ├── Toggle        #settings-fullscreen
│       │   └── Toggle        #settings-vsync
│       └── Button      #settings-back
```

The scene-transition fade is **not** part of this UXML. `FadeOverlay` (on `JamKitCore`) builds its own UIDocument with a high sorting order so it always covers this menu — see `Runtime/Scenes/FadeOverlay.cs`.

Class hooks live in `JamKitMenu.uss`: `.jk-root`, `.jk-view`, `.jk-panel`, `.jk-button`, `.jk-title`, `.jk-row`, `.jk-section`, `.jk-dim`.

## How settings persist

- **Audio:** Volume sliders bind **two-way** to `FloatVariableSO`s — moving the slider sets the variable, and changing the variable from anywhere else moves the slider (the subscription is torn down in `OnDisable`). The `AudioServiceRunner` watches `OnValueChanged` on each variable, applies dB conversion to the mixer, and writes the value to PlayerPrefs under `JamKit.Vol.Master / .Music / .Sfx`. On Awake the runner reads PlayerPrefs back into the variables.
- **Graphics:** Quality / Resolution / Fullscreen / VSync write directly to PlayerPrefs under `JamKit.Gfx.*` and apply via `QualitySettings` + `Screen.SetResolution`. No SO needed since they don't have multi-listener semantics.

## Adding to the menu

The MenuController is `sealed` on purpose. To extend:

1. Add new elements to your own UXML override (point `MenuUxml` at it).
2. Add a sibling MonoBehaviour on the same GameObject that takes a reference to `MenuController`, queries `MenuController.Root` for the new elements, and wires `RegisterValueChangedCallback` / `clicked` callbacks.

## Theme

Every PanelSettings needs a **Theme Style Sheet** or default controls (Button/Slider/Dropdown) render unstyled and Unity warns "UI will not render properly". JamKit ships `Resources/JamKitDefaultTheme.tss` (an import of Unity's built-in default theme); the `JamKitUI` helper assigns it to every PanelSettings JamKit creates, and patches theme-less ones it loads. If you author your own PanelSettings asset, assign a theme yourself (Unity's default runtime theme is fine).

## Gamepad / keyboard navigation

Runtime UI Toolkit needs an `EventSystem` (with the Input System UI module) for stick/keyboard navigation, and a focused element to start from. The wizard adds an `EventSystem` to every scene, and `MenuController` focuses the active view's primary control (Play / Resume / first setting) whenever a view opens — so a controller can drive the menu immediately. If you build menus by hand, add an `EventSystem` yourself.

`Esc` (the Pause action, routed through `PauseController.HandleBack`) backs out of a stacked submenu (Settings → Pause) before toggling pause, so it never double-opens.

## HUD bindings (no code)

To show HP / score / time without writing glue, point the HUD's `FloatVariableSO`s at:

- `LabelBinding` — sets a named `Label`'s text from a variable, with a format string (`"Score: {0:0}"`, `"{0:0.0}s"`).
- `BarBinding` — sizes a named fill element (width/height %) from a variable, normalized against a max value or a max variable (e.g. `Health.Max`).

Both live on the HUD's `UIDocument` GameObject and resolve their elements once the document's tree is built.

## Wiring Feel feedbacks to menu actions

The pause menu does not directly trigger juice. Instead:

1. Drop an `MMF_Player` somewhere in the bootstrap scene with the feedbacks you want on pause (e.g. quick chromatic aberration burst).
2. In a Ripple `VoidEventSO` like `OnPauseOpened`, add a persistent UltEvent call to `MMFPlayer.PlayFeedbacks()`.
3. Have a small custom component call `OnPauseOpened.Invoke()` when MenuController enters the Pause view. (You can subclass MenuController's behaviour with a sibling watcher.)

For most jams you won't need this — pause is usually unflashy — but the pattern works for any UI action.
