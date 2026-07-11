# Frogger in 5 Minutes

Grid movement, conveyor traffic, kill water, a goal, respawning. Assumes `JamKit > New Jam Project` has run. (Sample 06 "Arcade Playground" is this exact recipe, pre-built in code — import it to compare.)

## Player (30s)

1. Game scene, camera orthographic, size ~6.
2. `GameObject > JamKit > 2D > Player (Grid — Frogger)`. That's a `GridMover` (4-way, cell-snapped, hold-to-repeat), 1 HP `Health`, `Respawner`, flash/pop juice, and CameraShake + HitStop for when a car connects. Place it at the bottom, on a whole-number position.

## Traffic (90s)

3. `GameObject > JamKit > 2D > Hazard (Patrol — cars, saws)`. The preset is a kinematic trigger with `PatrolMover` in **TeleportToStart** mode (= conveyor: drive across, teleport back, drive again) and a lethal `Damager2D`.
4. Set `PathOffsets[0]` to `(18, 0, 0)` — the drive distance. Place at the left edge of a road row.
5. Duplicate along the row (stagger x positions for gaps); duplicate the row and negate the offset (`(-18, 0, 0)`) for opposing traffic. Speed per lane = difficulty dial.

## Water + bridge (90s)

6. Empty GameObject + `BoxCollider2D` (trigger) stretched across the river band + `TriggerZone`: set `RequiredTag = Player`, tick **Kill**. Death → the player's `Respawner` handles the rest.
7. Leave a gap in the water colliders for a bridge — or block columns instead: colliders on a layer of your choice, and set the player `GridMover.BlockedBy` to that layer so steps into rails are refused.
   (Log-riding is deliberately out of scope for 5 minutes — a kinematic log with `PatrolMover` + a script parenting the player on trigger is the 20-minute upgrade.)

## Goal (60s)

8. Trigger collider on the far bank + `TriggerZone`: `RequiredTag = Player`, `ScoreValue = 100` (+ ScoreService, auto-assigned), tick **OneShot** for one-and-done or leave off for laps.
9. Wire `OnEntered` to the player's `Respawner.Respawn()` (UltEvents on the Ripple event, or one line of code) to reset for the next crossing. Add a `Toast` (`GameObject > JamKit > Toast`) and map the same event to "GOAL!".

## Checklist before you decorate

- Cars kill on contact (drive one into the player).
- Water kills, bridge doesn't.
- Goal scores once and resets the run.
- Esc pauses; sliders work. `JamKit > Validate Setup` is green.
