# 02 Survivor

The full game loop in 3D: `WaveSpawner` waves of chasers, a `Mover3D` + `Aimer` +
`ProjectileShooter` player, pooled projectiles and death debris, a `GameTimer` round clock,
and a small glue script (`SurvivorLoop`) that ends the run — the "graduate to code" example.

This sample lives **inside your `Game.unity`** (setup drops the `SurvivorArena` prefab there):
the GameOver screen's Retry button loads the scene *named* "Game", so the whole
Bootstrap → Game → GameOver → Retry loop cycles.

## Setup
`JamKit > Samples > Set Up 02 Survivor`.

## Optional wiring (the setup log reminds you)
- Drag `Assets/_Project/Variables/Score` into `SurvivorLoop.Score` on the arena so kills feed
  the real score and the GameOver screen.

## Things to try
- Select the arena's `GameTimer` in play mode — progress bar + Pause/Resume buttons.
- Relocated components (`WaveSpawner`, `Aimer`, `Knockback`) live in `Scripts/` — they're
  sample-local now, copy or edit them freely.
