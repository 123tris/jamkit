# 03 UI Card Flip Mini

UI Toolkit demo with a `SaveServiceSO`-backed persistent high score. Click any of nine cards to flip it. Each flip increments a counter; the best run persists across plays.

## Setup

1. `JamKit > New Jam Project` first.
2. Empty scene, add a GameObject with `CardFlipDemo`. A `UIDocument` is added automatically.
3. Inspector:
   - `SaveService` ← `Assets/_Project/Services/SaveService`
4. Press Play. Click a card. Quit and re-enter — your high score sticks.

## What it demonstrates

- Building a UI Toolkit visual tree at runtime (no UXML required)
- `SaveServiceSO.Read` / `SaveServiceSO.Write` for persistent high-score storage
- Plain coroutines for the flip animation (no Feel needed for a UI tween of this scale)
