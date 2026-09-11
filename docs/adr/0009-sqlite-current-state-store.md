# ADR 0009 — SQLite Current-State Store

**Status:** Accepted for MVP baseline

## Decision

Use SQLite behind `IDraftRepository` with one current row per logical draft. Repository stores only protected payload plus minimized metadata. MVP does not enable WAL; `DELETE` journal mode and `secure_delete=ON` are the starting policy.

## Why

We need atomic updates, retention queries, crash-safe current-state persistence, and no custom file-index format. Avoiding WAL reduces persistence of older database page versions under the product's minimization goal.

## Consequences

- no revision/history table;
- write cadence must be debounced/bounded;
- if WAL is later desired, privacy implications require a new ADR;
- this is application-level minimization, not a forensic secure-erasure guarantee.
