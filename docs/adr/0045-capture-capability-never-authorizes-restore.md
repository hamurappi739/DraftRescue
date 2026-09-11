# ADR 0045 — Capture Capability Never Authorizes Restore

**Status:** Accepted

## Decision

`AllowedFieldHandle` is limited to `ReadSnapshot`. Future Restore uses a separately revalidated `ValidatedRestoreTarget`. A capture capability cannot be upgraded/reused for writes.
