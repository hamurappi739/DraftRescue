# ADR 0042 — No Content Reader in Phase 2 DI Graph

**Status:** Accepted

## Decision

Phase 2 production composition contains no concrete `IEligibleFieldTextReader`, and security handlers may not depend on content readers, tracker, repository, protector, clipboard or restore services.

## Consequence

`Denied -> content read` is blocked structurally, not merely by convention.
