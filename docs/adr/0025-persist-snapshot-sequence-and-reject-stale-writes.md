# ADR 0025 — Persist SnapshotSequence and reject stale writes

**Status:** Accepted

## Decision

Every protected current-state record stores a monotonic `SnapshotSequence`. Repository transactions reject a lower sequence and treat equal retries idempotently.

## Rationale

Async protection/storage completion can reorder. The persistence boundary must independently prevent an old snapshot from replacing the newest draft.
