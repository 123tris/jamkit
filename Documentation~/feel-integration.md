# Feel integration

Feel is JamKit's feedback engine — the kit ships **no** tween/flash/shake components of its
own (0.8's "Juice Lite" layer is gone). JamKit's job is the *trigger surface*; Feel's job is
the feel.

## The trigger chain

```
Health.OnDamaged (UltEvent<float>)     ← per-instance: only THIS object reacts
   → MMF_Player.PlayFeedbacks()        ← wire in the inspector; author feedbacks in Feel
```

Every gameplay moment exposes the same shape: `Pickup.OnCollected`, `TriggerZone.OnEntered`,
`Respawner.OnRespawned`, `Interactable.OnInteracted`, `GameTimer.Completed` — all serialized
UltEvents, all wireable straight to an `MMF_Player`. Global reactions (any-enemy-died shake)
subscribe a Feel player to the `Broadcast*` Ripple event assets instead (Ripple's
`EventListenerVoid` → `PlayFeedbacks`).

Starters from the wizard come pre-wired when Feel is installed: the `MMF_Player` is on the
prefab, `OnDamaged → PlayFeedbacks()` is connected, the stack is empty — author the feel, the
plumbing is done.

For **damage-scaled intensity** (big hits feel bigger), route through the ten-line `FeelPlayer`
shim from sample 03: `OnDamaged(float) → FeelPlayer.Play(float)` →
`PlayFeedbacks(position, intensity)`.

## Coverage map (what replaced Juice Lite)

| JamKit 0.8 (removed) | Feel replacement |
| --- | --- |
| `PunchScale` | MMF_Scale / MMF_SquashAndStretch |
| `SpriteFlash` / `MaterialFlash` | MMF_Flicker / MMF_MaterialSetProperty / sprite feedbacks |
| `CameraShake` | Cinemachine Impulse feedbacks (`FollowCamera` already carries the listener) |
| `ParticleBurst` | MMF_Particles* |
| `FloatingText` + `FloatingTextLayer` | MMF_FloatingText + MMFloatingTextSpawner |
| sibling-`Health` auto-trigger magic | the visible `OnDamaged/OnDied` UltEvent wiring above |

Still JamKit's (because Feel can't):

- **`HitStop`** — freeze-frames MUST go through `TimeServiceSO`'s push/pop stack. Feel's time
  feedbacks set `Time.timeScale` directly and will fight the pause menu. Use `HitStop`, or drop
  sample 03's `MMF_JamKitHitStop` custom feedback into your stacks — it routes through the
  stack and slots into Feel like any other feedback. **This is a hard rule** (PILLARS.md).
- **`SfxOnEvent` / `FmodSfxOnEvent`** — audio glue. Feel has no FMOD support; sounds go through
  JamKit's audio service. (Skip Feel's MMF_Sound under the FMOD backend.)

## The asmdef wall (why the package never references Feel)

Feel's MMFeedbacks ships without an asmdef — it compiles into `Assembly-CSharp`, which package
code can never reference. Integration is therefore: inspector wiring (UltEvents), one editor
reflection hook (the starter pre-wiring), and sample-shipped scripts (`MMF_JamKitHitStop`,
`FeelPlayer` — they live in your project, where Feel is visible). This is a feature: your Feel
version can never break the package.
