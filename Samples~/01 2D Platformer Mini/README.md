# 01 2D Platformer Mini

Self-building 2D platformer test scene. Demonstrates `Mover2D` driven by `InputServiceSO` and a `Health` component that broadcasts via Ripple events.

## Setup

One click: **`JamKit > Samples > Set Up 01 2D Platformer Mini`** (also offered right after import) — scaffolds `Assets/_Project` if needed, builds the demo scene, adds the component, and assigns the services. Press Play.

Or manually:

1. Run `JamKit > New Jam Project` first.
2. New empty scene, add a GameObject with `PlatformerDemo`.
3. Inspector:
   - `InputService` ← `Assets/_Project/Services/InputService`
   - `OnPlayerDamaged` ← optional `FloatEvent` (create one to drive Feel)
   - `OnPlayerDied` ← optional `VoidEventSO`
4. Press Play. Arrow keys / WASD + Space to move and jump. Run into the red enemy to take damage.

## Feel integration

Recommended: wire `OnPlayerDamaged` to an `MMF_Player` with:

- `MMCameraShakeFeedback` (amplitude scaled by damage)
- `MMSpriteRendererFeedback` (flicker)
- `MMFreezeFrameFeedback` (60ms freeze)

Open the FloatEvent asset, add a persistent UltEvent call to `MyMMFPlayer.PlayFeedbacks()`. Done.

Wire `OnPlayerDied` to a separate `MMF_Player` for death effects (red vignette, slow-mo, etc.).
