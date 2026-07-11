# Hour Zero Checklist

The first ten minutes of a jam, in order. Everything after step 4 is your game.

1. **New Unity 6 project** (URP template) → install Ripple, UltEvents, then JamKit (see README → Install).
2. **`JamKit > New Jam Project`.** Say yes to Fast Play Mode when offered. You now have Bootstrap / Game / GameOver scenes, all services, the mixer, menus, pause, and prefabs in `Assets/_Project/Prefabs/` (edit `JamKitCore` or `JamKitMenu` once — every scene updates).
3. **Press Play.** Start → Settings (sliders work) → Game (Esc pauses) → verify the loop end-to-end *before* you write any code. Thirty seconds now saves an hour at submission.
4. **`JamKit > Validate Setup`.** Green? Go. Not green? Every issue has a Fix button.
5. **Blocking out:** `GameObject > JamKit > …` presets for the player archetype closest to your idea (platformer / top-down / grid / ship / 3D). It lands with movement, health, and juice pre-wired.
6. **Enemies/hazards:** Enemy (Chaser), Hazard (Patrol), Kill Zone presets. Spawner or WaveSpawner to feed them in.
7. **Goals/score:** Pickup preset or a TriggerZone with ScoreValue. The HUD binds to `Score`/`Timer` variables via LabelBinding — no code.
8. **Juice check (hour 2, not hour 20):** every Health should have a flash + punch; the player should have CameraShake + HitStop (presets already did this). Drop `SfxOnEvent` components as sounds arrive; use the Sample 05 synth-blip trick before they do.
9. **Sounds/music:** drop clips on `AudioServiceSO.PlayMusic` (from any UltEvent or a 3-line script), hover/click clips on the MenuController, `DuckMusic` for stingers.
10. **Ship:** `JamKit > Build > WebGL (itch.io)`. Zip the folder it reveals, upload to itch, set "This file will be played in the browser".

## When something's weird

- Sliders silent → Validate → Repair Mixer.
- UI unstyled → Validate → Assign Theme.
- Gamepad can't navigate menus → scene needs an EventSystem (Validate creates it).
- A mover ignores input → its `InputService` reference is empty (Validate → Auto-Assign), or the Gameplay map isn't active (a menu with `InitialView = None` activates it).
- Nothing shakes → the camera needs an impulse listener (wizard cameras have one; custom cameras: add `CinemachineExternalImpulseListener`, or `CinemachineImpulseListener` on a CinemachineCamera).
