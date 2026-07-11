# 05 Juice Toggle

The before/after that sells Juice Lite. A turret pelts a target block twice a second; press **J** (or click the button) to flip every juice receiver on and off at once.

With juice ON: camera kick, freeze-frame, white flash, squash pop, floating damage numbers, debris burst on death, synthesized hit blips (no audio assets needed — the clips are generated in code).
With juice OFF: exactly the same mechanics, completely dead. That difference is what jam ratings reward.

## Setup

1. Run `JamKit > New Jam Project` (or make sure PoolService / AudioService / TimeService assets exist).
2. Open a scene with a `JamKitCore` (the wizard's Game scene), add an empty GameObject with `JuiceToggleDemo`.
3. Assign `PoolService`, `AudioService`, `TimeService` from `Assets/_Project/Services/` (auto-assign usually does this for you on add).
4. Press Play. Watch. Press **J**. Feel the difference.

## What to steal

- Every juice component subscribes to the target's sibling `Health` — the demo adds them with **zero wiring**.
- The toggle is one loop over `FindObjectsByType<JuiceBehaviour>` setting `enabled` — juice stays a clean seam you can strip or replace (with Feel) without touching gameplay.
- `SynthBlip()` shows how to fake SFX before you have audio assets; swap in real clips later without changing anything else.
