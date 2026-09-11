# ADR 0027 — Restore is single-flight per DraftId

**Status:** Accepted

## Decision

At most one target mutation may run concurrently for a given DraftId. Duplicate clicks/commands coalesce or receive AlreadyInFlight; they never cause a second text injection.
