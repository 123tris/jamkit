# Pong in 60 Seconds

Two players, one keyboard. Assumes `JamKit > New Jam Project` has run (services exist).

## The court (15s)

1. Open the Game scene. Set the Main Camera to **orthographic**, size ~5.
2. `GameObject > JamKit > 2D > Paddle` — set `Mover2D.AxisScale` to **(0, 1)** (vertical), move to x = −7. This is P1.
3. Duplicate it, move to x = +7. This is P2.
4. Two walls: `GameObject > 2D Object > Sprites > Square` (or any sprite), stretch across the top (y = +5) and bottom (y = −5), add a `BoxCollider2D` to each.

## The ball (10s)

5. `GameObject > JamKit > 2D > Ball (Pong-Breakout)`. Done — it launches on Play, reflects at constant speed, and the paddles' `Paddle` marker bends its angle by hit offset (english). Optional: set `SpeedGainPerBounce` to 0.2 for escalating rallies.

## Goals (15s)

6. `GameObject > JamKit > 2D > Kill Zone` at x = −9, rotate/size the collider to cover the left edge. Uncheck `Kill`; check `RemoveEnterer`? No — for pong we want score + serve:
   - `ScoreService` + `ScoreValue 1` (auto-assigned) — this zone is P2's point.
   - Wire the zone's `OnEntered` Ripple event (or add a 2-line script on `Entered`) to the ball's `Respawner.Respawn()` — the serve.
7. Duplicate for the right edge (P1's point).

## Two players (20s)

8. P1 paddle already reads the default `InputService` (**W/S**, since WASD and arrows both feed the shared Gameplay map — but that would move P2 too, so:)
9. Create **two** input service assets: `Create > JamKit > Services > Input Service` →
   - `InputServiceP1`: set `GameplayMapName` to **Gameplay1** (WASD only), tick **AutoEnableGameplay**.
   - `InputServiceP2`: set `GameplayMapName` to **Gameplay2** (arrows only), tick **AutoEnableGameplay**.
   Both use the same `JamKitInput` actions asset — the maps ship with it. (AutoEnableGameplay matters:
   menus only activate the *default* service's map; secondary services activate their own.)
10. Assign `InputServiceP1` to the left paddle's Mover2D, `InputServiceP2` to the right.

Play. W/S vs ↑/↓, english on the paddle edges, score on the walls behind you.

**Juice next:** `OnBounce` on the ball → wire to an `SfxOnEvent.Play()` and a `CameraShake.Play()`; add `PunchScale` to the paddles with `PlayOn` = the bounce event. Sample 05 shows the full stack.
