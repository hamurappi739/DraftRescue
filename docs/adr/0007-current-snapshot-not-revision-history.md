# ADR 0007 — Persist current draft snapshot, not revision history

**Status:** Accepted.

## Decision

For a logical active draft session, DraftRescue maintains the latest recoverable state. Persistence updates/upserts that state and does not append a durable revision on every edit.

## Consequences

- product remains a recovery layer rather than typing history;
- lower storage/write volume;
- older user-deleted/replaced versions are intentionally not recoverable by default;
- crash consistency must use atomic replacement/checkpoint semantics.
