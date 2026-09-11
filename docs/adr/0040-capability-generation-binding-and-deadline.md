# ADR 0040 — Capability Bound to Generation, Binding and Monotonic Deadline

**Status:** Accepted

## Decision

A capture capability is valid only for its original `ContextGeneration`, ephemeral target binding and profile revision, and before a monotonic deadline. Initial default max age is 1000 ms with an absolute implementation cap of 2000 ms.

## Consequences

Profile/user settings cannot extend the hard cap. Stale/expired handles fail closed.
