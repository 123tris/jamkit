# 01 Platformer

The 2D genre pack: `Mover2D` player, patrol hazard (`PatrolMover` + `Damager`), a kill-pit
`TriggerZone`, a one-shot goal zone, and a `FollowCamera`. WASD/arrows + Space; the hazard
hurts, the pit kills, the goal toasts.

**The wiring lesson lives on the Player prefab** — the old hidden "sibling Health" magic is now
three visible UltEvent calls you can inspect, reorder, or delete:

- `Health.OnDamaged → HitStop.Play(float)` (damage-scaled freeze-frame)
- `Health.OnDied → Respawner.RespawnAfterDelay()`
- `Respawner.OnRespawned → Health.ResetFull()`

Add feel the same way: drop an `MMF_Player` on the Player (Feel) and wire
`Health.OnDamaged → PlayFeedbacks()` next to the others.

## Setup
`JamKit > Samples > Set Up 01 Platformer`.

## Things to try
- Select the Player in play mode → Health → Debug → **Damage 1**: the button fires the real
  chain (hit-stop and all).
- Make a prefab VARIANT of `PatrolHazard` with a longer path and drop it in — scenes stay
  lists of prefabs.
