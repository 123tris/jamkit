# Forking JamKit during a jam, and getting the good parts back

Editing the kit mid-jam is **not** a failure mode. When a component is 90% right at 2am, forking it
is the correct call. What went wrong in GMTK 2026 was not the fork — it was that the fork was
*invisible* and had *no way home*.

This document is the way home.

## What happened, so it doesn't again

Day 3 of a two-week jam, the team vendored the package into the game repo so they could edit it:

```
13142ac  Prepare to vendor jamkit: drop git URL, un-ignore folder + lock
1ac445f  Squashed 'Packages/jamkit/' content from commit c27fb41
```

Seven commits then edited package internals. Some were genuinely good — a mouse-sensitivity binding,
`SceneService.FadeThrough`, `FirstPersonLook.LookTowards`, a `MenuNavigator`/`PauseCoordinator`
extraction. All of it was stranded, because:

- **Both trees claimed version `0.9.1`.** Two different codebases, one version number.
- **The base commit stopped existing.** `c27fb41` was squashed into the game repo, then rewritten out
  of the package repo — so `git merge-base` had nothing to work with, in either direction.
- **Nothing ever said "you have forked."** No warning, no marker, no checklist.

Every one of those is cheap to prevent.

## Rule 1 — mark the fork the moment you make one

The instant you edit an embedded copy, bump `package.json`:

```json
"version": "0.9.1-fork.1"
```

Increment `fork.N` on each substantive change. This costs one line and buys the single most important
property: **a version number identifies exactly one tree.** `JamKit > Doctor` reminds you — it detects
an embedded package and says so.

## Rule 2 — vendor with `subtree`, not by copying

Copying the folder in destroys the history the merge later needs. Use a subtree, which keeps it:

```bash
git subtree add --prefix Packages/jamkit https://github.com/123tris/jamkit.git main --squash
```

**Record the upstream commit you forked from** — put it in your game repo's README or a
`Packages/jamkit/FORKED_FROM.txt`. `--squash` writes it into the merge commit message, but that
message is easy to lose in a rebase, and it is the one fact the merge later depends on:

```bash
git -C Packages/jamkit log -1 --format=%H > FORKED_FROM.txt   # before squashing
```

Pull upstream changes mid-jam with:

```bash
git subtree pull --prefix Packages/jamkit https://github.com/123tris/jamkit.git main --squash
```

## Rule 3 — keep game code out of the package

Two specific mistakes to avoid, both of which happened:

- **Never add a game-specific dependency to a kit type.** GMTK added a `[Required] FloatVariableSO
  playerHealth` to `PauseController` to express "can't pause while dead". That makes the component
  unusable in any project without that variable. If the kit can't express what you need, add the
  *seam* (a `BoolReference CanOpen`), not the specific field.
- **Never put a new game class inside the package folder.** `Projectile.cs` and `HitHandler.cs` were
  created inside `Packages/jamkit/Runtime/`, in the global namespace. They are game code; they belong
  in `Assets/`. Anything under `Packages/jamkit/` must be in the `Metz.JamKit` namespace and
  genre-agnostic.

A useful test before editing a kit file: **would this change make sense in someone else's game?** If
no, the change belongs in game code — usually behind a seam you add to the kit instead.

## Rule 4 — upstream while the reasons are fresh

After the jam ships, walk the diff and sort every hunk into three piles:

```bash
git diff <forked-from-sha>..HEAD -- Packages/jamkit/
```

| Pile | What to do |
|---|---|
| **Genuine kit improvement** — a bug fix, a missing seam, a primitive that suits 3+ genres | Cherry-pick to upstream, with the jam as the justification in the commit message |
| **Game-specific** — needs your variables, your genre, your controller | Move it into `Assets/`; if the kit blocked you, upstream the *seam* that would have unblocked you |
| **A deletion because the feature was wrong** | Upstream the deletion, or file it — a feature the jam ripped out is the strongest signal the kit has |

That third pile is the one people skip, and it is the most valuable. Pooling was removed from JamKit
1.0 precisely because a jam deleted it on day 3.

## If there is no merge base (the GMTK case)

When the base commit is gone, no automated merge exists. Salvage by hand:

1. Diff the two trees file-by-file (`diff -ru`, or open both in a merge tool).
2. For each differing file decide: upstream-wins, fork-wins, or hand-merge.
3. Apply the fork's improvements to upstream as **fresh commits**, each with a message explaining
   what the jam hit. The reasoning is the valuable part; the diff is usually small.
4. Re-point the game at the package (`file:` or git URL) and delete the vendored copy.

Slow and manual — which is the whole argument for Rules 1 and 2.
