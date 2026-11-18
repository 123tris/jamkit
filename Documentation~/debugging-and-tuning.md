# Debugging & runtime tuning

The debuggable pillar (PILLARS.md): **a feature is not done until you can watch it work.**
Everything below already exists — this page is the map.

## Inspectors are the debugger

- **Every stateful thing has a `Debug` foldout.** Select a service *asset* during play
  (Project window → `Assets/_Project/Services/…`): TimeService shows the live timescale,
  the active modifiers and whether it's paused, with Pause/Resume buttons.
- **Every action is a `[Button]` that fires the real wiring.** `Health` → Damage 1 / Heal 1 /
  Kill / Reset runs the entire chain — juice, feedback players, raised events, death, despawn. If the
  button looks right, the game is right. Same for `Spawner.SpawnOne`, `SpawnBurst.Burst`,
  `Respawner.Respawn`, `GameTimer` Start/Pause, `MenuController` view toggles, `Toast` test, and
  Play on a `Sound` or `Music` asset.
- **Misconfiguration is red.** Load-bearing references are `[Required]` — an empty one shows an
  error in the inspector, and `JamKit > Doctor` lists every one in open scenes, prefabs and assets.
  Auto-assign turns them green as you add objects, and `JamKit > Auto-Assign References In Open
  Scenes` reruns it. The Doctor also covers the project-shape checks (Ripple version, service
  assets, input actions, themes, build list, EventSystem).
- **The live values and buttons work in Play mode.** In edit mode the Debug foldout shows a dash
  and its buttons are greyed out, because they would act on objects that are not running.

## Ripple's built-in tooling

- Every event asset has a **Raise button** in play mode — raise `GameOver` from the inspector
  and watch everything that listens react, without playing until it happens.
- Every variable shows a live, editable **Value** in play mode.
- **Tools > Ripple > Debugger** — what was raised or changed, when, by whom, with the value.

## Builds: the DebugPanel

WebGL jam builds have no inspector — the `DebugPanel` on `JamKitCore` is the debug surface
that ships. **Backquote (`)** toggles it: FPS, a time-scale slider, reload, quick-jump buttons
for every scene in Build Settings, quit. Strip it for release by deleting the child on the
`JamKitCore` prefab (one edit, every scene updates).

## Runtime tuning that sticks

- **ScriptableObject edits persist play mode** (Unity native). Tune the *asset* — a service's
  config, a shared `FloatReference` variable — not the scene object, and your balance survives
  stopping. Scene-component tweaks still revert; use Unity's Copy/Paste Component Values for
  those.
- **Ripple variables reset `current → initial` each play session** (by design, so runs are
  repeatable). To keep a tuned value: set the *initial* value, or tick **Persist** on the
  variable — persisted variables (volumes, HighScore) save on change and load next session,
  in the editor and in builds. `Clear Saved Value` on the asset resets it.

## The feature-done checklist

Before calling any new feature finished:

1. Live state visible in a Debug foldout.
2. Its trigger pressable from the inspector (and it fires the *real* path).
3. Misconfiguration caught by `[Required]` / the Doctor.
4. One sentence in the README or sample README.
