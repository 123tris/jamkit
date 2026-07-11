# 06 Arcade Playground

One 2D scene exercising the whole any-genre kit at once — the "how do these compose?" reference.

**Left — frogger crossing.** `GridMover` player (WASD/arrows), two lanes of `PatrolMover` cars on a conveyor (`TeleportToStart` mode), a river of `TriggerZone` kill-water with a one-cell bridge, and a one-shot goal zone worth 100 points. Death → `Respawner` puts you back at the start.

**Middle — a lever.** Walk up to it and the prompt appears (`Interactable.PromptVisual` — zero UI plumbing); press **E** (`Interactor`) to toggle traffic speed. A `Toast` narrates everything.

**Right — self-playing breakout.** A `Bouncer2D` ball trapped in a pit with an angled `Paddle` wall (english keeps the trajectory lively) grinds down brick rows — each brick is `Health(1)` + `SpriteFlash` + `SpawnBurst` debris + `FloatingText` "+10". Cleared rows rebuild after a beat.

Everything is tinted white-square sprites generated at runtime — no art, no assets.

## Setup

1. Run `JamKit > New Jam Project` (for the service SOs; the Game scene's JamKitCore is handy but not required).
2. Empty GameObject → add `ArcadePlaygroundDemo`.
3. Assign `InputService` (required), `ScoreService` and `PoolService` (optional) from `Assets/_Project/Services/`.
4. Play. Cross the road, dodge the river, pull the lever, watch the bricks.

## What to steal

- `TriggerZone` is doing four different jobs here (kill water, goal, score, event hook) — same component, different toggles.
- The cars are one `PatrolMover` preset in conveyor mode; an entire frogger lane is ~10 lines.
- The bricks show per-instance juice: only the brick that's hit flashes, because juice hangs off each brick's own `Health`.
