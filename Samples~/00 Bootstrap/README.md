# 00 Bootstrap

Shows the canonical pattern: a single MonoBehaviour that takes service SOs in the inspector and fires a Ripple event for designers to wire to Feel.

## Setup

1. Run `JamKit > New Jam Project` so the service SOs exist in `Assets/_Project/Services/`.
2. Open `Assets/_Project/Scenes/Game.unity` (or any scene with the JamKitCore in it).
3. Add an empty GameObject with the `BootstrapDemo` component.
4. Inspector:
   - `InputService` ← `Assets/_Project/Services/InputService`
   - `PoolService` ← `Assets/_Project/Services/PoolService`
   - `AudioService` ← `Assets/_Project/Services/AudioService`
   - `ClickClip` ← any short SFX
   - `OnClicked` ← optional `VoidEventSO` (create one if you want to wire Feel to it)
5. Press Play. Click attack button (left mouse / gamepad X) to spawn a pooled cube and fire the click event.

## Feel integration

To add screen shake + hit-freeze + sound polish on click:

1. Drop an `MMF_Player` in the scene with `MMCameraShakeFeedback` + `MMFreezeFrameFeedback` + `MMSoundFeedback`.
2. Open the `OnClicked` `VoidEventSO` asset, expand the UltEvent response.
3. Add a persistent call: `MyMMFPlayer.PlayFeedbacks()`.
4. Press Play — every click now shakes the camera, freezes time briefly, and plays the sound.

No code change to JamKit. Designer drives the feel entirely via Feel + Ripple events.
