# Feel integration

Feel is optional. JamKit ships a small juice set of its own — `PunchScale`, `ColorFlash`,
`CameraShake`, `SpawnBurst` and `HitStop`, plus `Tween` and `Ease` for code — and the starters
use it. Feel is for when you want more: stacks of feedbacks authored in its inspector. JamKit's
job is the *trigger surface*; Feel's job is the feel.

## The trigger chain

```
Health.OnDamaged (Signal<float>)       ← per-instance: only THIS object reacts
   → MMF_Player.PlayFeedbacks()        ← wire in the inspector; author feedbacks in Feel
```

Every gameplay moment exposes the same shape: `TriggerZone.OnEntered`, `Respawner.OnRespawned`,
`Interactable.OnInteracted`, `GameTimer.OnCompleted`, `ProjectileShooter.OnFired` — all Ripple
`Signal`s, all wireable straight to an `MMF_Player`. Global reactions (any-enemy-died shake) go
through a Ripple event instead: drop the event asset on the signal's Raise row, and have the Feel
player listen to it (Ripple's `EventListenerVoid` → `PlayFeedbacks`).

Starters carry no `MMF_Player`: the project template is the same with or without Feel. Add one
to a starter and connect its `OnDamaged` signal to `PlayFeedbacks()`. That signal already plays
the starter's own juice (`PunchScale` on players and enemies, `HitStop` on players, `SpawnBurst`
on enemies), so remove whatever your Feel stack replaces.

For **damage-scaled intensity** (big hits feel bigger), route through the ten-line `FeelPlayer`
shim from the Feel Bridge sample (03): `OnDamaged(float) → FeelPlayer.Play(float)` →
`PlayFeedbacks(position, intensity)`.

## Coverage map (JamKit's juice, and where Feel goes further)

| JamKit | Feel |
| --- | --- |
| `PunchScale` | MMF_Scale / MMF_SquashAndStretch |
| `ColorFlash` (one `SpriteRenderer`) | MMF_Flicker / MMF_MaterialSetProperty / sprite feedbacks |
| `CameraShake` (a Cinemachine impulse) | Cinemachine Impulse feedbacks |
| `SpawnBurst` | MMF_Particles* |
| — | MMF_FloatingText + MMFloatingTextSpawner |

Either shake needs an impulse listener on the camera. The FollowCam starters' `CinemachineCamera`
has one; a camera without Cinemachine takes a `CinemachineExternalImpulseListener`.

Still JamKit's (because Feel can't):

- **`HitStop`** — freeze-frames MUST go through `TimeServiceSO`. `HitStop` pushes a time modifier
  and releases it, and the lowest active modifier wins, so a freeze and the pause menu never undo
  each other. Feel's time feedbacks set `Time.timeScale` directly and will fight the pause menu.
  Use `HitStop`, or drop the Feel Bridge sample's `MMF_JamKitHitStop` feedback into your stacks — it plays a
  scene `HitStop` and slots into Feel like any other feedback. **This is a hard rule**: PILLARS.md
  makes `TimeServiceSO` the one owner of `Time.timeScale`.
- **Sounds** — `Sound` and `Music` assets: `ClipSound` and `ClipMusic` for Unity audio, `FmodSound`
  and `FmodMusic` once FMOD is installed. Play them from a signal (`Health.OnDamaged → Play`).
  Feel has no FMOD support, so skip Feel's MMF_Sound under FMOD.

## The asmdef wall (why the package never references Feel)

Feel's MMFeedbacks ships without an asmdef — it compiles into `Assembly-CSharp`, which package
code can never reference. Integration is therefore inspector wiring and sample-shipped scripts
(`MMF_JamKitHitStop`, `FeelPlayer` — they live in your project, where Feel is visible). This is a
feature: your Feel version can never break the package.
