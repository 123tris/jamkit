# 00 Hour Zero

The kit tour with **zero scripts and zero input**: crates rain from a `Spawner` (through the
`PoolServiceSO`), a `TriggerZone` catch-zone swallows them, bumps a sample-local Ripple score
variable, and broadcasts a sample event that a `Toast` narrates. Read the hierarchy — every
gameplay object is a prefab instance, and every connection is visible in the inspector.

## Setup
`JamKit > Samples > Set Up 00 Hour Zero` (offered automatically on import). Setup scaffolds
`Assets/_Project` if needed, wires the sample's prefabs to your service assets, and drops the
project's `JamKitCore` into the scene.

## Things to try
- Select `Data/DemoScore` while playing — the live value ticks in the inspector.
- Select the `PoolService` asset while playing — watch the pool counts in its Debug foldout.
- Click `Spawn One` on the CrateSpawner's `Spawner` Debug foldout.
- Raise `Data/CrateCaught` from its own Invoke button — the Toast fires without a crate.
