# 03 Feel Showcase

**Requires the Feel asset** (importing without Feel = compile errors; delete this folder to fix).

Feel is JamKit's feedback engine — this sample shows the whole trigger chain on the
`FeelTarget` prefab:

```
Health.OnDamaged (UltEvent<float>)
  → FeelPlayer.Play(float)                 // damage amount becomes intensity
    → MMF_Player.PlayFeedbacks(pos, i)     // scale punch + flicker + JamKit hit-stop
```

Two sample-local scripts ship here:

- `FeelPlayer` — ten-line intensity bridge (skip it and wire `PlayFeedbacks()` directly when
  you don't need scaling).
- `MMF_JamKitHitStop` — a custom Feel feedback that routes freeze-frames through JamKit's
  `HitStop`/`TimeServiceSO` stack. **Use this instead of Feel's time feedbacks**, which stomp
  `Time.timeScale` and fight the pause menu (see PILLARS.md).

## Setup
`JamKit > Samples > Set Up 03 Feel Showcase`.

## Things to try
- Select a target in play mode → Health → Debug → **Damage 1** — the full Feel stack fires.
- Open the target's `MMF_Player` and author more feedbacks — the trigger is already wired.
- Pause with Esc mid-hit-stop: the timescale stack composes; nothing fights.
