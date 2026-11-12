# JamKit menus

The menu is UI Toolkit (UXML + USS) driven by one `MenuController` and one `MenuView` per screen.

## The markup is yours

`JamKit > New Jam Project` copies the menu documents and `JamKitMenu.uss` into `Assets/_Project/UI/Resources/`. Edit them in UI Builder; the wizard never overwrites them.

`JamKitMenu.uxml` instances one document per screen (`JamKitStartMenu`, `JamKitPauseMenu`, `JamKitSettingsMenu`, `JamKitGameOverMenu`). The references are relative, so keep the files in one folder.

## Adding a screen

1. Write `MyScreen.uxml` with a named root element and instance it from `JamKitMenu.uxml`.
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

A shown screen also carries the USS class `jk-view--shown`.

## MenuController

Holds the stack of open screens. Methods to wire from buttons: `Show(view)`, `ShowOnly(view)`, `Back()`, `ShowSettings()`, `ShowPause()`, `TogglePause()`, `HideAll()`, `QuitGame()`.

`Pause` (a gameplay action) and `Cancel` (a UI action) both call `HandleBack()`: close the top submenu, otherwise toggle `PauseView`.

Scene changes are not the menu's job. Buttons raise Ripple events and the `GameFlow` asset decides which scene each event loads.

## Settings

`MenuSettingsBinder` binds the three volume sliders to Ripple variables and stores quality, resolution, fullscreen and vsync in PlayerPrefs. Resolution and vsync rows are hidden on WebGL. Elements with the class `jk-quit` are hidden on WebGL.
