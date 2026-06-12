# 02 3D Walker Mini

Self-building 3D walker scene with a player capsule and a ring of pickups. Demonstrates `Mover3D` + `AudioServiceSO` + a Ripple `AudioClipEvent` for pickup notifications.

## Setup

1. `JamKit > New Jam Project` first.
2. Empty scene, add a GameObject with `WalkerDemo`.
3. Inspector:
   - `InputService` ← `InputService`
   - `AudioService` ← `AudioService`
   - `PickupClip` ← any short pickup chime
   - `OnPickup` ← optional `AudioClipEvent`
4. Press Play. WASD to walk.

## Feel integration

For floating "+10" text on pickup, drop an `MMF_Player` with `MMFloatingTextFeedback`. Wire `OnPickup` (AudioClipEvent) to call `MyMMFPlayer.PlayFeedbacks()` and an extra `MMSoundManagerSoundPlayFeedback` if you want Feel's sound system to handle it instead of `AudioServiceSO`.
