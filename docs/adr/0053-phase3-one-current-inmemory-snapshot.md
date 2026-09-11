# ADR 0053 — Phase3 One Current Inmemory Snapshot

**Status:** Accepted

## Decision

Phase 3 tracker stores one current in-memory snapshot per logical draft and no revision history.

## Rationale

The product is recovery current-state, not typing history.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
