# JamKit Pillars

Four rules. A change that serves none of them does not ship.

## 1. Modular

- Systems do not reference each other. They meet through Ripple assets (variables, events, runtime sets) or through a `Signal` wired in the inspector.
- A prefab dragged into an empty scene works, or fails loudly through `[Required]`.
- A component does one thing. Combinations are built in prefabs, not in toggles.

## 2. Editable

- Data lives in ScriptableObjects. Changing the game means changing data.
- Every trigger, tuning value and reaction is an inspector field.
- Prefer a `FloatReference` over a bare float where sharing a value is plausible.
- Behaviour a designer picks from a fixed set is an enum. Use a `[SerializeReference]` strategy only when game code needs to add new behaviours.

## 3. Debuggable

- Stateful things show live state in their inspector, and actions have a `[Button]` that runs the real path.
- Misconfiguration is caught at edit time: `[Required]`, dropdowns filled from the real data, the Doctor. Runtime code does not carry guards or warn-once bookkeeping for it.
- The `DebugPanel` ships in builds, because a WebGL build has no inspector.

## 4. Lean

- Use what Unity and the stack already provide before writing anything: `UnityEvent`, `InputActionReference`, `AudioResource`, Cinemachine impulses, UI Toolkit bindings.
- A helper with no caller inside the kit is deleted.
- No comments. Names carry the meaning, and the reason for a decision goes in the CHANGELOG.
- A gameplay component earns its place by serving three or more genres. Otherwise it lives in a sample.

## Ownership

Process-wide state has one owner, and everything else asks that owner.

- `Time.timeScale`: `TimeServiceSO`
- The active input map and the cursor: `InputServiceSO`
- Persistence: Ripple persistent variables
- Scene loading: `SceneServiceSO`

If the owner cannot express what a caller needs, add a method to the owner. A second writer is a bug.

Services host themselves on one persistent `[JamKit]` object created on demand. It is the only `DontDestroyOnLoad` object in the kit, and no component looks it up.
