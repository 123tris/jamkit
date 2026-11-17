# Hour Zero Checklist

The first ten minutes of a jam, in order. Everything after step 4 is your game.

1. **New Unity 6 project** (URP template) → install Odin, Ripple, then JamKit (see README → Install). Feel and FMOD are optional, and the scaffold is the same with or without them: Unity audio, no Feel wiring.
2. **`JamKit > New Jam Project`.** Say yes to Fast Play Mode when offered. You now have Bootstrap / Game / GameOver scenes, all services, menus, pause, the starter prefab library, and `JamKitCore` / `JamKitMenu` in `Assets/_Project/Prefabs/` (edit a prefab once — every scene updates).
3. **Press Play.** Start → Settings (sliders work) → Game (Esc pauses) → verify the loop end-to-end *before* you write any code. Thirty seconds now saves an hour at submission. Backquote (`) toggles the DebugPanel.
4. **`JamKit > Doctor`.** Green? Go. Not green? Every issue has a Fix button. (Red `[Required]` fields anywhere? Odin Validator lists them all.)
5. **Blocking out:** `GameObject > JamKit > …` places starter instances (player archetypes, chaser enemies, pickup, spawner, kill zones, cameras). **Customize as prefab VARIANTS of the starters** — scenes stay lists of prefabs.
6. **Goals/score:** the Pickup starter or a TriggerZone with a ScoreValue writes straight into the `Score` variable; `HighScoreTracker` on JamKitCore keeps the record (persistent). HUDs bind via LabelBinding — no code.
7. **Feel check (hour 2, not hour 20):** add an `MMF_Player` to a starter, connect its `Health.OnDamaged` signal to `PlayFeedbacks()`, then author feedbacks (scale punch, flicker, impulse shake). Freeze-frames: `HitStop` (already wired on players), never Feel's time feedbacks. See `feel-integration.md`.
8. **Sounds/music:** FMOD path — author events in Studio, fire them with `FmodSfxOnEvent` (wire `Health.OnDamaged → Play`) and `FmodAudioService.PlayMusic`; sliders already drive the buses. No FMOD? Same story with `SfxOnEvent` + `AudioServiceSO`.
9. **Round clock / waves:** drop a `GameTimer` (wire `Completed` → whatever ends your run); grab `WaveSpawner` from sample 02 if you need sequenced waves.
10. **Ship:** `JamKit > Build > WebGL (itch.io)`. Zip the folder it reveals, upload to itch, set "This file will be played in the browser".

## When something's weird

- Sliders silent → Doctor → Repair Mixer (Unity-audio path) / check FMOD banks are built (FMOD path).
- UI unstyled → Doctor → Assign Theme.
- Gamepad can't navigate menus → scene needs an EventSystem (Doctor creates it).
- A mover ignores input → its `InputService` reference is red (`[Required]`) — Doctor → Auto-Assign, or the Gameplay map isn't active (a menu with `InitialView = None` activates it).
- Nothing shakes → the camera needs an impulse listener (wizard cameras and `FollowCamera` have one; custom cameras: add `CinemachineExternalImpulseListener`).
- Time frozen after a weird stop → select the TimeService asset: Debug foldout shows the stack; ResetState clears it (and it self-clears each play session).
