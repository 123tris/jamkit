# JamKit Roadmap

Three goals, in tension with each other:

1. **Efficiency** — minutes from "create project" to "playable thing on screen", and minutes from "I need X" to "X is wired".
2. **Juice** — JamKit games should *feel* good by default, because juice is what jam ratings reward.
3. **Flexibility** — nothing in the kit may assume a genre. Platformer, survivor, puzzle, card game: all first-class.

## Design principles (the contract every feature must pass)

- **Explicit wiring, zero-friction defaults.** Components keep serialized SO references (no runtime lookup, no singletons) — but the *editor* may auto-fill them when the answer is unambiguous. Convenience at edit time, transparency at runtime.
- **Juice = event receivers.** Gameplay broadcasts Ripple events (it already does); juice components *subscribe*. JamKit ships the lightweight 80% with **zero new dependencies**; Feel remains the deluxe path on the exact same events. Every juice component also exposes a plain public `Play()` so non-Ripple users can call it from any event system.
- **Small orthogonal components, not frameworks.** Flexibility comes from composition. Hard cap on juice components: ~100 lines each, no sequencers, no curve editors — if you need that, that's Feel's job.
- **Nothing ships unverified.** Every milestone ends with the Validate window green and the samples compiling.

---

## M1 — Friction Zero (efficiency) · ~1–2 sessions

The compounding milestone: everything built later benefits from it. Kills the remaining per-object drag work without touching the architecture.

- **Editor-time auto-assign.** `Reset()` hook (editor-only helper) on JamKit components: when added, search the project for the needed service SO — exactly one match → assign it; zero or many → leave null and log. (Same philosophy as the AutoAssign package already in this project.)
- **`GameObject > JamKit > …` creation menu.** Pre-wired presets: Player 2D, Player 3D, Enemy, Pickup, Projectile, Spawner, Camera 2D/3D, HUD, Debug Panel. Each lands fully assigned via the auto-assigner.
- **`JamKit > Validate Setup` window.** Checks: mixer params exposed, PanelSettings themed, scenes in Build Settings, EventSystem per scene, services assigned, input asset present — each issue with a Fix button. Jam-day insurance; would have caught both 0.4.1 bugs.
- **Prefab-first wizard output.** JamKitCore + Menu become prefabs instanced into each scene, so a fix or addition propagates to all scenes at once.
- **Wizard offers fast enter-play-mode** (domain-reload off — the reset guards already make this safe).

## M2 — Juice Lite (juice) · ~2–3 sessions

Fills the gap left when the Feel module was removed: today a jammer without Feel has *no* juice path. These use only existing dependencies (Cinemachine impulse, TimeService, AudioService, UI Toolkit).

- **`CameraShake`** — Cinemachine impulse source with a `Shake(strength)` method + Ripple event slot. The listeners are already on every JamKit camera; nothing currently fires them.
- **`HitStop`** — event → `TimeService.FreezeForSeconds(0.05)`. The service exists; nothing in the package uses it yet.
- **`SpriteFlash` / `MaterialFlash`** — white-flash on damage via MaterialPropertyBlock.
- **`PunchScale`** — squash-and-stretch pop on event (struct tween, no allocations, not a tween library).
- **`ParticleBurst`** — play an assigned ParticleSystem on event.
- **`FloatingTextLayer`** — pooled damage/score numbers on a UI Toolkit overlay, projecting world→panel space. No TMP, no world-space canvas.
- **Menu sounds** — optional hover/click AudioClips on MenuController through AudioService.
- **Stretch: music ducking** — mixer-snapshot duck for stingers ("wave complete!") via AudioService.
- **Sample 05 "Juice Toggle"** — same scene, juice on/off button. The before/after sells the kit.
- **Docs: "Graduating to Feel"** — same events, swap receivers; delete nothing.

## M3 — Any-Genre Kit (flexibility) · as-needed, per jam

Fill genre gaps with small movers/interactions, prioritized by what the next jam actually needs (top jam genres: platformer ✅, survivor ✅, arcade, puzzle, card).

- **`AimAtCursor2D/3D`** — mouse/stick aiming (pairs with ProjectileShooter for twin-stick).
- **`ThrustMover`** — rotate + thrust (Asteroids-likes).
- **`GridMover`** — tile-stepped movement (puzzle/sokoban).
- **`Interactable` + `Interactor`** — "press E" prompts, the most-requested missing primitive.
- **`Toast`** — UI Toolkit banner ("Wave 2!", "New High Score!") bound to Ripple events. Half flexibility, half juice.
- **Drag-and-drop helper** (UI Toolkit) — card/inventory jams.
- **One "playground" sample** exercising all of these in one scene, instead of a mini-sample each.

## M4 — Ship It (meta) · before sharing with teammates

- **Push to GitHub + CI** (game-ci): compile + edit-mode tests on every push. The Editor-asmdef and mixer bugs were exactly the class CI catches.
- **Resolve the Ripple path dependency** — `file:C:/Repos/Ripple` blocks anyone else from installing JamKit. Publish Ripple to a git URL and document both installs.
- **Sample compile harness** — `Samples~` is invisible to Unity, so sample code never compiles during development (SurvivorDemo has never been compiled). A dev-only import or CI step closes that hole.
- **One-click itch.io WebGL build** — `JamKit > Build > WebGL (itch.io)` with correct compression/template; builds burn jam hours.
- **Tests for ScoreService / TimerService**, `Documentation~` with a 10-step "hour zero" checklist.

## Later / undecided

- World-space UI Toolkit HUD (once 6.2-class support lands), input-rebinding screen, save-slot UI, JamKit Hub dockable window.

## Non-goals (the identity fence)

No tween library, no node graphs, no netcode, no ECS, no Feel clone. JamKit is the wiring and the 80% defaults — depth lives in dedicated assets.

## Sequencing rationale

M1 before M2: auto-assign + presets make every juice component cheaper to ship and test. M2 before M3: juice is visible in *every* genre, new movers only in some. M4 gates sharing — do it before the first team jam, not after.

## Open questions

- Auto-assign ambiguity rule (two InputServices for local co-op?): assign only on exact-single-match, surface the rest in Validate — acceptable?
- Juice Lite naming: `Runtime/Juice/` folder, components prefixed plainly (`CameraShake`, not `JamKitCameraShake`)?
- Should the wizard adopt Juice Lite by default (CameraShake + HitStop pre-wired to Health.OnDamaged in presets), or stay neutral?
