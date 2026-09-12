# Phase 4 checkpoint scheduler review — 2026-09-12

## Decision

The current-state checkpoint scheduler is implemented as an application-layer planner over `PersistenceCheckpointCoordinator`. It never writes or protects text itself; callers take due candidates and pass them through the existing protect-before-repository coordinator.

## Guarantees

- trailing debounce delays rapid edits instead of checkpointing every event;
- a maximum dirty-age ceiling forces progress during sustained editing;
- one newest candidate is retained per `DraftId`, so older pending plaintext snapshots are replaced rather than queued;
- a configured pending-draft capacity prevents unbounded memory growth;
- stale snapshot sequences cannot replace a newer pending/in-flight candidate;
- protection/repository failures receive one delayed retry and cannot form a hot retry loop;
- failed, stale, empty, or disposed work does not create plaintext fallback or history rows.

## Timing decision

The scheduler requires an explicit `CheckpointSchedulePolicy`. Trailing debounce, maximum dirty age, retry delay, and pending capacity are not silently promoted to product defaults; they remain experiment-backed inputs as required by Open Decision 6 and `SNAPSHOT_TIMING_EXPERIMENT_PLAN.md`.

## Evidence

- Full solution build: 0 warnings, 0 errors.
- Full test suite: **174/174** passed.
- Tests cover coalescing, max dirty age, capacity rejection, delayed retry, newer-pending preservation, and stale-sequence rejection.

## Privacy traceability

The implementation preserves P-008, P-018, P-030, P-043, P-046 and C-050: uncertainty remains fail-closed, retention stays bounded, diagnostics remain content-free, protection precedes persistence, operational outcomes are typed, and checkpoint work cannot create an unbounded writer/queue path. Preview, Copy, Restore, and browser capture remain out of scope.
