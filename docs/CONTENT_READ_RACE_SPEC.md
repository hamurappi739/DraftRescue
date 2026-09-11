# Content Read Race Specification

## Race classes

1. focus/context changes after capability issuance;
2. target is destroyed/recreated during provider read;
3. provider returns after capability deadline;
4. two independent authorized reads complete out of order;
5. empty candidate is followed by newer non-empty state.

## Rules

- capability claim is atomic;
- late completion does not revive capability;
- result carries original `ContextGeneration` and `SnapshotSequence`;
- Application compares generation again before tracker mutation;
- for the same logical draft, lower/equal sequence cannot replace a higher sequence except exact idempotent replay explicitly recognized by tracker;
- provider exception/timeout produces no snapshot;
- an old empty result cannot clear a newer non-empty snapshot.

## Sequence source

`SnapshotSequence` is assigned before provider read from a monotonic per-context/application sequence source, not from completion time.
