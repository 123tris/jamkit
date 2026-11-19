# 03 Feel Bridge

**Requires the Feel asset.** Importing this sample without Feel gives compile errors; delete the folder to fix them.

JamKit ships its own basic juice (`PunchScale`, `ColorFlash`, `CameraShake`, `HitStop`, `PopupText`, `SpawnBurst`). Feel is for stacks of authored feedbacks. These two scripts connect the two; there is no scene, because every JamKit signal already wires straight to an `MMF_Player`:

```
Health.OnDamaged (Signal<float>)
  → FeelPlayer.Play(float)                 // the damage amount becomes the intensity
    → MMF_Player.PlayFeedbacks(position, intensity)
```

- **`FeelPlayer`**: the intensity bridge, so big hits feel bigger. Without scaling, wire `MMF_Player.PlayFeedbacks()` straight from the signal and skip it.
- **`MMF_JamKitHitStop`**: a Feel feedback that freezes frames through JamKit's `HitStop` and `TimeServiceSO`. **Use it instead of Feel's time feedbacks**, which set `Time.timeScale` directly and fight the pause menu (PILLARS.md: the time service is the one owner of the timescale).

## Using them

1. Import the sample, then add an `MMF_Player` to a starter, for example the 3D player.
2. Add `FeelPlayer` beside it, point its `Player` at the `MMF_Player`, and wire `Health.OnDamaged → FeelPlayer.Play`.
3. In the `MMF_Player`, add **Time > JamKit Hit Stop** and drag the starter's `HitStop` into it.
4. Remove the starter's own juice that the Feel stack replaces (`PunchScale`, the `OnDamaged → HitStop.Play` call).

See `Documentation~/feel-integration.md` for the whole map.
