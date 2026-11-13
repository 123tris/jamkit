# JamKit UI

Menus, the HUD controls and the overlays are UI Toolkit. Each one is a UXML document with a USS stylesheet, shown by a `PanelRenderer`. Scripts find their elements by name when the renderer reports the UI as loaded, and only set what changes while playing.

## The menu markup is yours

`JamKit > New Jam Project` copies the menu documents, `JamKitMenu.uss` and `JamKitPanelSettings.asset` into `Assets/_Project/UI/`. Edit them in UI Builder; the wizard never overwrites them. Copies left in `Assets/_Project/UI/Resources/` by an older wizard are moved there, references intact.

`JamKitMenu.uxml` instances one document per screen (`JamKitStartMenu`, `JamKitPauseMenu`, `JamKitSettingsMenu`, `JamKitGameOverMenu`). The references are relative, so keep the files in one folder.

In UI Builder, pick **Jam Kit Default Theme** in the canvas theme menu so the controls look as they do in the game. `JamKitMenu.uxml` previews the Start screen, because the other screens' instances carry `jk-instance--hidden`. Open a screen's own document to work on that screen; each one previews itself.

## Classes

`JamKitMenu.uss` keeps its colors and sizes as variables on `:root` (`--jk-text`, `--jk-panel`, `--jk-button-width`…), so a restyle starts there. Class names are block, element and modifier:

| Class | Meaning |
| --- | --- |
| `jk-view` | A screen's root |
| `jk-view--hidden`, `jk-view--shown` | Hidden screen and the screen on top. `MenuView` switches them |
| `jk-instance--hidden` | On a screen's instance in `JamKitMenu.uxml`: keeps it out of the preview until the menu shows it |
| `jk-panel`, `jk-panel--wide` | The box a pause or settings screen sits in |
| `jk-title`, `jk-title--small` | Headings |
| `jk-buttons`, `jk-button` | A column of buttons and one button |
| `jk-button--quit` | Hidden on WebGL |
| `jk-section`, `jk-section__title` | A group of settings |
| `jk-row`, `jk-row__label`, `jk-row__control` | One setting |
| `jk-row--desktop` | A setting hidden on WebGL (resolution, vsync) |
| `jk-score` | The Game Over score |

On WebGL the menu adds `jk-webgl` to its panel root, and the stylesheet hides `jk-button--quit` and `jk-row--desktop` under it.

## Adding a screen

1. Write `MyScreen.uxml` with a named root element of class `jk-view`, and instance it from `JamKitMenu.uxml` with the class `jk-instance jk-instance--hidden`.
2. Add a child GameObject under the menu with a `MenuView`.
3. Pick the root element from the `RootElementName` dropdown, which lists the names in the UXML.
4. Add its buttons: pick each from the dropdown and wire its `OnClicked` signal.
5. Drag the view into `MenuController.Views`.

## MenuView

| Field | Meaning |
| --- | --- |
| `RootElementName` | The screen's root element in the UXML |
| `CanOpen` | Constant or Ripple bool; the screen refuses to open while false |
| `PausesTime` | Time stays paused while this screen is anywhere in the stack |
| `SwitchesToUIInput` | Switch the input service to the UI map while this screen is on top |
| `OnShown`, `OnHidden` | Signals |
| `Buttons` | Element name + `OnClicked` signal |

## MenuController

Needs a `PanelRenderer` on the same GameObject. Holds the stack of open screens. Methods to wire from buttons: `Show(view)`, `ShowOnly(view)`, `Back()`, `ShowSettings()`, `ShowPause()`, `TogglePause()`, `HideAll()`, `QuitGame()`.

`Pause` (a gameplay action) and `Cancel` (a UI action) both call `HandleBack()`: close the top submenu, otherwise toggle `PauseView`.

When you edit the menu document while playing, the renderer reloads it; the controller binds the new elements and keeps the open screens.

Scene changes are not the menu's job. Buttons raise Ripple events and the `GameFlow` asset decides which scene each event loads.

## Settings

`MenuSettingsBinder` binds the three volume sliders to Ripple variables and stores quality, resolution, fullscreen and vsync in PlayerPrefs.

## HUD values

`ValueLabel` and `ValueBar` are controls in the UI Builder library under Project > Custom Controls > Metz.JamKit. Bind their `value` to a Ripple variable's `CurrentValue`, and they follow the variable with no script:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:jk="Metz.JamKit">
    <jk:ValueLabel format="Score: {0:0}" data-source="project://database/Assets/_Project/Variables/Score.asset">
        <Bindings>
            <ui:DataBinding property="value" data-source-path="CurrentValue" binding-mode="ToTarget" />
        </Bindings>
    </jk:ValueLabel>
    <jk:ValueBar min="0" max="100">
        <Bindings>
            <ui:DataBinding property="value" data-source-path="CurrentValue" binding-mode="ToTarget" />
        </Bindings>
    </jk:ValueBar>
</ui:UXML>
```

In UI Builder, set the data source by dragging the variable asset onto the control's Data Source field. `ValueLabel` formats its value with `format`. `ValueBar` fills the share of `min`..`max` the value covers, from the left, or from the bottom with `vertical`. The bar's look (`jk-bar`, `jk-bar__fill`, `--jk-bar-fill`…) comes from `JamKitControls.uss`, which the JamKit theme imports.

## Overlays

`Toast`, `DebugPanel` and the screen fade draw their own documents from `Runtime/UI/Overlays/`, each on its own panel settings so their order against the menu is fixed: the toast under the menu, the debug panel over it, the fade over everything. Their looks live in `JamKitOverlays.uss`; copy a document and its stylesheet into your project to change one, and point the overlay's `PanelRenderer` (or `ScreenFade`'s `Document`) at the copy.
