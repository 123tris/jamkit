# JamKit Roadmap

Three goals, in tension with each other:

1. **Efficiency**: minutes from "create project" to "playable thing on screen", and minutes from "I need X" to "X is wired".
2. **Juice**: JamKit games should *feel* good by default, because juice is what jam ratings reward.
3. **Flexibility**: nothing in the kit may assume a genre. Platformer, survivor, puzzle, card game: all first-class.

## Design principles

**The contract lives in [PILLARS.md](PILLARS.md)**: modular, editable, debuggable, lean. The roadmap's corollaries:

- **Free to open.** A teammate who installs nothing from the Asset Store can open the project: JamKit's assemblies need only Unity packages and Ripple. Feel and FMOD stay optional, behind their own assemblies or in samples.
- **Explicit wiring, zero-friction defaults.** Components keep serialized references (no runtime lookup, no singletons), and the editor fills them when the answer is unambiguous: exactly one candidate, never a guess.
- **Two event granularities, picked deliberately.** A component's own reaction is wired on its `Signal` (`Health.OnDamaged`: only *this* instance reacts). A global reaction is a Ripple event asset on the Signal's raise list, heard with an `EventListener`. Wiring per-instance feedback through a shared event is a bug: every enemy flashes when one is hit.
- **The kit ships the basic juice; Feel goes further.** `PunchScale`, `ColorFlash`, `CameraShake`, `HitStop` and `Tween` cover hour one. Feel is for stacks of authored feedbacks. Audio is `Sound` and `Music` assets on a Unity or FMOD backend; state is Ripple.
- **Small orthogonal components, not frameworks.** Flexibility comes from composition. A new primitive is measured by how many archetype columns it serves (below), not by how complete it is. One or two columns: it lives in a sample as a copy to hack.
- **Nothing ships unverified.** Every change ends with `Tools~/compile-check.sh` green, the Doctor green on a fresh scaffold, and the samples compiling.

## The archetype matrix

When considering a new primitive, check it against the classics. A primitive in three or more columns is kit material; one column is game code.

| Primitive | Pong | Breakout | Frogger | Invaders | Asteroids | Flappy | Platformer | Survivor | FPS¹ | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Input mover | ✓ | ✓ | | ✓ | | ✓ | ✓ | ✓ | ✓ | ✅ Mover2D/3D |
| First-person look | | | | | | | | | ✓ | ✅ FirstPersonLook |
| Dash/dodge | | | | | | | ✓ | ✓ | ✓ | ⛔ blocked: movers overwrite velocity (backlog, game feel) |
| Knockback | | | | | | | ✓ | ✓ | ✓ | 📦 sample 02, blocked the same way |
| Ball bounce | ✓ | ✓ | | | | | | | | 📦 sample 04 (Bouncer2D) |
| Grid step | | | ✓ | | | | | | | 📦 sample 04 (GridMover) |
| Rotate+thrust | | | | | ✓ | | | | | 📦 sample 04 (ThrustMover2D) |
| Chase target | | | | | | | ✓ | ✓ | ✓ | ✅ ChaseMover |
| Patrol/conveyor | | | ✓ | ~ | | ✓ | ✓ | | ✓ | ✅ PatrolMover |
| Screen wrap | | | | | ✓ | | | | | 📦 sample 04 (ScreenWrap2D) |
| Zones (kill/goal/score) | ✓ | ✓ | ✓ | ✓ | | ✓ | ✓ | | ✓ | ✅ TriggerZone |
| Aim cursor/stick | | | | | | | | ✓ | | 📦 sample 02 (Aimer) |
| Spawn on death | | ✓ | | ✓ | ✓ | | | ✓ | ✓ | ✅ SpawnBurst |
| Respawn/checkpoint | ✓ | ✓ | ✓ | | ✓ | | ✓ | | ✓ | ✅ Respawner |
| Press-E interact | | | | | | | ✓ | ✓ | ✓ | ✅ Interactor/-able |
| Shoot (visible projectile) | | | | ✓ | ✓ | | ✓ | ✓ | ✓ | ✅ ProjectileShooter |
| Shoot (hitscan) | | | | ~ | | | | ✓ | ✓ | ⛔ cut 1.0, see CHANGELOG |
| Health-as-timer drain | | | | | | | ~ | ✓ | ✓ | ✅ HealthDrain |
| Sequenced waves | | | | ✓ | | | | ✓ | ✓ | 📦 sample 02 (WaveSpawner); meets the rule, promote with enemies & combat |
| HP/damage/pickups/score/menus | — | — | — | — | — | — | — | — | — | ✅ core |
| Feedback (pop/flash/shake/freeze) | all | all | all | all | all | all | all | all | all | ✅ juice; Feel for more |
| Placeholder sounds, popup text | all | all | all | all | all | all | all | all | all | Next: phase 4 |

📦 = ships as a sample-local script (one or two columns); ✅ = kit component.

¹ The FPS column stands in for every first-person genre (boomer shooter, horror, walking sim, immersive sim). They share the same rig, so `FirstPersonLook` earns its slot despite the single column.

Genres still thin: **card/board** (drag and drop, hand layout), **puzzle-match** (grid queries beyond movement), **rhythm** (beat clock). Add rows when a jam demands them.

## Next

Items leave this list when they land; the CHANGELOG records each one with its reasons.

### Phase 1: 1.0 installs and works for anyone

- **Release Ripple 0.3.0.** README, `ProjectTemplate.RippleProblem` and ci.yml point at `#v0.3.0`, a tag that does not exist yet, so the documented install fails. Merge `feature/0.3.0` into Ripple's `main` and tag it.
- **Confirm the 6.5 floor** by playing the template once in a fresh 6000.5 project; it is what the kit is compiled and authored in.
- **CI runs** once Ripple is tagged and the repository has a `UNITY_LICENSE` secret; then delete its `if: ${{ false }}` line.

### Phase 4: zero-asset juice

- **`PopupText`**: damage numbers and pickup text that rise and fade on a UI Toolkit overlay.

### Samples

Hour Zero, Platformer, Survivor and Arcade are re-authored on the project template's fixed GUIDs, which makes sample setup "scaffold and open the scene". The Feel showcase becomes the two bridge scripts and a README, since Feel is a paid asset.

## Backlog

Recorded, not scheduled. Pull an item when a jam needs it.

- **Game feel.** `Mover2D`, `Mover3D` and `ChaseMover` set velocity outright every physics step, so no impulse survives: the Survivor sample's knockback is gone a step later, and a dash cannot compose with a mover. First, movers accelerate toward their input velocity (infinite acceleration keeps today's behaviour). Then coyote time, jump buffering, variable jump height, invulnerability frames, and `Knockback` and `Dash` as kit components.
- **Enemies and combat.** `ChaseMover` paths around walls when its object has a `NavMeshAgent`; area damage; `WaveSpawner` promoted into the kit; shooting at a target.
- **Thin genres**: card/board drag and drop, grid queries for match puzzles, a beat clock. An input rebinding screen.
- **Small fixes**: `GameTimer.StartTimer(float)` replaces the `Duration` reference and loses its variable binding; `Mover3D` throws when the scene has no main camera.

## Non-goals (the identity fence)

No netcode, no ECS, no Feel clone. JamKit ships a one-file tween and a single graph (Game Flow); depth lives in dedicated assets.
