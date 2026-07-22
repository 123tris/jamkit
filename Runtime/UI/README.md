# JamKit Menus — Start / Settings / Pause (UI Toolkit + Ripple)

JamKit's menu system is **UI Toolkit** (UXML + USS) driven by a single `MenuController` MonoBehaviour. Audio volume sliders bind directly to **Ripple `FloatVariableSO`s** so any other UI or system in the project can read or write the same volume state without going through the menu.

## The markup is yours to edit

`JamKit > New Jam Project` copies the menu documents + `JamKitMenu.uss` out of the package and into **`Assets/_Project/UI/Resources/`**. Those copies are the real menu — the package keeps only the pristine templates, so open them in UI Builder and restyle freely. Nothing you change there propagates back to the package, and re-running the wizard never overwrites them.

`JamKitMenu.uxml` is a thin **composition root**: it instances one document per view — `JamKitStartMenu.uxml`, `JamKitPauseMenu.uxml`, `JamKitSettingsMenu.uxml` — so each view can be edited on its own in UI Builder. Every reference is relative (`<ui:Template src="JamKitStartMenu.uxml" />`, `<Style src="JamKitMenu.uss" />`), so all five files must stay siblings in the same folder. Keep the name `JamKitMenu` on the root: `MenuController` / `GameOverController` fall back to `Resources.Load("JamKitMenu")` when no asset is assigned. Renaming the root is fine if you assign the result to `MenuUxml` explicitly.

`MenuController` is unchanged by the split — it still reads one `VisualTreeAsset` and finds each view by name (`start-menu`, `pause-menu`, `settings-menu`) with a recursive query, so the extra `<ui:Instance>` container between the root and each view is transparent to it.

## Two ways to get a working menu

1. **One-click setup:** `JamKit > New Jam Project` builds the Bootstrap scene with `JamKitCore` (hosting every Runner) and `JamKitMenu` (UIDocument + MenuController with all service SOs assigned), pointed at your project's UXML copy.
2. **Manual:** Add `UIDocument` + `MenuController` yourself. Leave UXML blank — `MenuController` auto-loads `JamKitMenu.uxml` from Resources. Assign the four service SOs (`AudioService`, `TimeService`, `SceneService`, `InputService`).

## MenuController inspector fields

| Field | Purpose |
| --- | --- |
| `MenuUxml` | UXML asset. Leave blank to load `JamKitMenu.uxml` from Resources (your scaffolded copy). |
| `ExtraStyles` | Optional USS layered on top of the menu stylesheet. |
| `AudioService` | `AudioServiceSO` — used to find the master/music/sfx Ripple variables for sliders. |
| `TimeService` | `TimeServiceSO` — used for pause/resume around the Pause menu. |
| `SceneService` | `SceneServiceSO` — used to load scenes when buttons fire. |
| `InputService` | `InputServiceSO` — used to switch the active action map when pausing. |
| `MasterVolumeOverride` / `MusicVolumeOverride` / `SfxVolumeOverride` | Optional FloatVariableSO overrides; otherwise the menu pulls them from the `AudioServiceSO`. |
| `GameSceneName` | Scene the Play button loads. |
| `MainMenuSceneName` | Scene the Pause → Main Menu button loads. |
| `InitialView` | Which view shows on enable (Start / Settings / Pause / None). |

## UXML structure

The runtime tree (each `#…-menu` view comes from its own file — `JamKitStartMenu.uxml`, `JamKitPauseMenu.uxml`, `JamKitSettingsMenu.uxml` — wrapped in a `.jk-instance` container by `<ui:Instance>`):

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

Class hooks live in `JamKitMenu.uss`: `.jk-root`, `.jk-view`, `.jk-panel`, `.jk-button`, `.jk-title`, `.jk-row`, `.jk-section`, `.jk-dim`. The Game Over screen is built in code (`GameOverController`) but reuses these same classes, so restyling the USS restyles it too.

Element **names** (`#start-play`, `#settings-master`, …) are the contract between the UXML and the controllers — restyle and rearrange freely, but keep the names on the elements you keep, or their buttons and sliders go dead.

## How settings persist

- **Audio:** Volume sliders bind **two-way** to `FloatVariableSO`s — moving the slider sets the variable, and changing the variable from anywhere else moves the slider (the subscription is torn down in `OnDisable`). The `AudioServiceRunner` watches `OnValueChanged` on each variable, applies dB conversion to the mixer, and writes the value to PlayerPrefs under `JamKit.Vol.Master / .Music / .Sfx`. On Awake the runner reads PlayerPrefs back into the variables.
- **Graphics:** Quality / Resolution / Fullscreen / VSync write directly to PlayerPrefs under `JamKit.Gfx.*` and apply via `QualitySettings` + `Screen.SetResolution`. No SO needed since they don't have multi-listener semantics.

## Adding to the menu

The MenuController is `sealed` on purpose. To extend:

1. Add new elements to your project's `Assets/_Project/UI/Resources/JamKitMenu.uxml` (or a separate UXML you point `MenuUxml` at).
2. Add a sibling MonoBehaviour on the same GameObject that takes a reference to `MenuController`, queries `MenuController.Root` for the new elements, and wires `RegisterValueChangedCallback` / `clicked` callbacks.

## Theme

Every PanelSettings needs a **Theme Style Sheet** or default controls (Button/Slider/Dropdown) render unstyled and Unity warns "UI will not render properly". JamKit ships `Resources/JamKitDefaultTheme.tss` (an import of Unity's built-in default theme); the `JamKitUI` helper assigns it to every PanelSettings JamKit creates, and patches theme-less ones it loads. If you author your own PanelSettings asset, assign a theme yourself (Unity's default runtime theme is fine).

## Gamepad / keyboard navigation

Runtime UI Toolkit needs an `EventSystem` (with the Input System UI module) for stick/keyboard navigation, and a focused element to start from. The wizard adds an `EventSystem` to every scene, and `MenuController` focuses the active view's primary control (Play / Resume / first setting) whenever a view opens — so a controller can drive the menu immediately. If you build menus by hand, add an `EventSystem` yourself.

`Esc` (the Pause action, routed through `PauseController.HandleBack`) backs out of a stacked submenu (Settings → Pause) before toggling pause, so it never double-opens.

## HUD bindings (no code)

To show HP / score / time without writing glue, point the HUD's `FloatVariableSO`s at:

- `LabelBinding` — sets a named `Label`'s text from a variable, with a format string (`"Score: {0:0}"`, `"{0:0.0}s"`).
- `BarBinding` — sizes a named fill element (width/height %) from a variable, normalized against a max value or a max variable (point `MaxVariable` at the same shared asset a `Health.Max` references, so the bar scales when max HP changes).

For HP, point `BarBinding.Variable`/`LabelBinding.Variable` at a `Health.CurrentVariable`. That link is two-way: the HUD reads it, and any system that writes it (checkpoint, cheat, buff) drives the Health back.

Both live on the HUD's `UIDocument` GameObject and resolve their elements once the document's tree is built.

## Wiring Feel feedbacks to menu actions

The pause menu does not directly trigger juice. Instead:

1. Drop an `MMF_Player` somewhere in the bootstrap scene with the feedbacks you want on pause (e.g. quick chromatic aberration burst).
2. In a Ripple `VoidEventSO` like `OnPauseOpened`, add a persistent UltEvent call to `MMFPlayer.PlayFeedbacks()`.
3. Have a small custom component call `OnPauseOpened.Invoke()` when MenuController enters the Pause view. (You can subclass MenuController's behaviour with a sibling watcher.)

For most jams you won't need this — pause is usually unflashy — but the pattern works for any UI action.
