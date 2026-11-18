# Hour Zero Checklist

The first ten minutes of a jam, in order. Everything after step 4 is your game.

1. **New Unity 6 project** (URP template) → install Ripple, then JamKit (see README → Install). Feel and FMOD are optional, and the scaffold is the same with or without them: Unity audio, no Feel wiring.
2. **`JamKit > New Jam Project`.** Say yes to Fast Play Mode when offered. You now have Bootstrap / Game / GameOver scenes, all services, menus, pause, the starter prefabs, and `JamKitCore` / `JamKitMenu` in `Assets/_Project/Prefabs/` (edit a prefab once — every scene updates).
3. **Press Play.** Start → Settings (sliders work) → Game (Esc pauses) → verify the loop end-to-end *before* you write any code. Thirty seconds now saves an hour at submission. Backquote (`) toggles the DebugPanel.
4. **`JamKit > Doctor`.** Green? Go. Not green? Most issues have a Fix button. (Red `[Required]` fields anywhere? The Doctor lists them all.)
5. **Blocking out:** `GameObject > JamKit > …` places starter instances (player archetypes, chaser enemies, pickup, spawner, kill zones, cameras). **Customize as prefab VARIANTS of the starters** — scenes stay lists of prefabs.
6. **Goals/score:** the Pickup starter's `TriggerZone.OnEntered` adds to the `Score` variable; wire `Score.Add` from any other signal the same way. `HighScoreTracker` on JamKitCore keeps the record (persistent). HUDs bind a `ValueLabel` or `ValueBar` to a variable's `CurrentValue` in UXML — no code; the Game Over score is one.
7. **Feel check (hour 2, not hour 20):** add an `MMF_Player` to a starter, connect its `Health.OnDamaged` signal to `PlayFeedbacks()`, then author feedbacks (scale punch, flicker, impulse shake). Freeze-frames: `HitStop` (already wired on players), never Feel's time feedbacks. See `feel-integration.md`.
8. **Sounds/music:** make a Sound asset (`Create > JamKit > Audio > Sound`: a clip, or an Audio Random Container for variation), set its Output to JamKitMixer's SFX group, and play it from a signal (`Health.OnDamaged → Play`). Music assets (`Create > JamKit > Audio > Music`, Output on the Music group) play from a signal too and keep going across scene loads. The settings sliders already drive the mixer through the `MixerVolume` components on JamKitCore. FMOD path: author events in Studio and use FMOD Sound / FMOD Music assets the same way; for the sliders, add an `FmodBusVolume` per volume variable to JamKitCore.
9. **Round clock / waves:** drop a `GameTimer` (wire `OnCompleted` → whatever ends your run); grab `WaveSpawner` from sample 02 if you need sequenced waves.
10. **Ship:** `JamKit > Build > WebGL (itch.io)`. Zip the folder it reveals, upload to itch, set "This file will be played in the browser".

## When something's weird

- Sliders don't change a sound → its Output is empty, so it skips the mixer: set it to a JamKitMixer group (SFX or Music). FMOD path: check the banks are built and an `FmodBusVolume` drives each bus.
- UI unstyled → Doctor → Assign Theme.
- Gamepad can't navigate menus → scene needs an EventSystem (Doctor creates it).
- A mover ignores input → its `Move` or `Jump` action is red (`[Required]`): `JamKit > Auto-Assign References In Open Scenes` fills them by action name. Or the gameplay maps aren't active: the `MenuController` turns them on only while no screen that takes UI input is showing, so a Game scene's menu leaves `InitialView` empty.
- Nothing shakes → `CameraShake` sends a Cinemachine impulse, and the camera needs a listener to feel it: the FollowCam starters have one; a camera without Cinemachine takes `CinemachineExternalImpulseListener`.
- Time frozen after a weird stop → select the TimeService asset: its Debug foldout shows the effective scale, the active modifiers and whether it's paused, and Resume lifts a pause. Every modifier is cleared when Play starts.
