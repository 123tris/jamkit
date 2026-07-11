# Graduating to Feel

Juice Lite is deliberately the 80%: small receivers, no sequencing, no curves. When you own MoreMountains **Feel**, it becomes the deluxe layer — on the exact same triggers, with nothing rewired.

## The mental model

Every Juice Lite receiver fires from one of three trigger paths:

1. **Sibling `Health` C# events** (per-instance: the hit thing reacts),
2. **Ripple event assets** (global: anything reacts to anything),
3. **`Play()` called directly** (UltEvents, UnityEvents, code).

Feel's `MMF_Player.PlayFeedbacks()` is just another `Play()`-shaped method. Anywhere JamKit can trigger a Juice Lite receiver, it can trigger Feel instead.

## The swap, path by path

- **Ripple-triggered juice** (CameraShake on a shared damage event, Toast on wave start): open the Ripple event asset, add a persistent UltEvent call to your `MMF_Player.PlayFeedbacks()`. Remove the Juice Lite component — or keep both during tuning.
- **Sibling-triggered juice** (SpriteFlash/PunchScale on prefabs): Health's per-instance events are C#-only, so give the prefab an `MMF_Player` and point `Health.OnDamaged` (the Ripple slot) at it — or keep the Lite components; they coexist fine with Feel.
- **Code-triggered juice**: replace `myFlash.Play()` with `myFeelPlayer.PlayFeedbacks()`.

## Recommended mapping

| Juice Lite | Feel feedback |
| --- | --- |
| CameraShake | Camera Shake / Cinemachine Impulse |
| HitStop | Time > Freeze Frame |
| SpriteFlash / MaterialFlash | Renderer > Flicker / Material |
| PunchScale | Transform > Scale (punch) |
| ParticleBurst | Particles > Play |
| SfxOnEvent | Audio > Sound (with variations) |
| FloatingText(-Layer) | UI > Floating Text |
| Toast | keep Toast — it's UI, not feedback |

Both camera shakes speak Cinemachine impulse, so JamKit's impulse listeners on cameras serve Feel unchanged.

## Why not just always Feel?

Team members without the asset still get a game that feels good, WebGL builds stay lean, and the identity fence holds: JamKit ships the wiring and defaults, Feel ships the depth.
