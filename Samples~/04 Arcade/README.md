# 04 Arcade

Home of the arcade components — sample-local scripts now (`Scripts/`): `Bouncer2D`, `Paddle`,
`GridMover`, `ThrustMover2D`, `ScreenWrap2D`. Copy or edit them freely; they're yours.

The scene is breakout-pong: a `Bouncer2D` ball serves itself, gains english off the `Paddle`
(vertical `Mover2D`), and shatters `Health(1)` bricks (each `SpawnBurst`s debris). The goal
behind the paddle resets the ball — scene-level wiring:
`TriggerZone.OnEntered → Respawner.Respawn()` → `Respawner.OnRespawned → Bouncer2D.Launch()`.

`GridMover` (frogger steps), `ThrustMover2D` (asteroids ship), and `ScreenWrap2D` aren't in the
scene — grab them from `Scripts/` when your jam calls for them.

## Setup
`JamKit > Samples > Set Up 04 Arcade`.

## Things to try
- Duplicate the Brick prefab as a variant with `Health.Max` set to 3 for armored bricks.
- Put `ScreenWrap2D` on the ball and delete the walls. Chaos mode.
