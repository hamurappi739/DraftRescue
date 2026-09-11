# ADR 0017 — Periodic checkpoints, not shutdown-time saving, provide durability

**Status:** Accepted

## Decision

Draft durability is based on bounded periodic/current-state checkpoints during normal operation. Windows session-end handling is best-effort only and must respond promptly without blocking shutdown for draft preservation.

## Consequences

- last few uncheckpointed characters may be lost on abrupt termination;
- product copy must not promise perfect keystroke-level recovery;
- checkpoint interval/max-dirty-age becomes an important Phase 3/4 experiment;
- shutdown code stays small and deterministic.
