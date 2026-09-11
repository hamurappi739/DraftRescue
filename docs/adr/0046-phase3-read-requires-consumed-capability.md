# ADR 0046 — Phase3 Read Requires Consumed Capability

**Status:** Accepted

## Decision

Phase 3 target-content read requires an atomically consumed one-read capability.

## Rationale

This preserves the classify-before-read boundary and prevents permission reuse.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
